using System.Text.RegularExpressions;
using System.Xml.Linq;
using Lianxin.Fleet.Cli.Commands;
using Lianxin.Fleet.Core.Infrastructure;

namespace Lianxin.Fleet.Cli.Signing;

internal static partial class IosSigning
{
    public static async Task<int> RunAsync(string[] args)
    {
        if (args.Contains("--help"))
        {
            Console.WriteLine("sign-ios --team TEAMID --profile 路径 [--bundle-id com.lianxin.device] [--method debugging|release-testing|enterprise] [--udids ID1,ID2] [--output 新目录] [--check-only] [--project Xcode工程路径]");
            return 0;
        }
        if (!OperatingSystem.IsMacOS())
            throw new PlatformNotSupportedException("iOS 归档与签名需在安装 Xcode 的 Mac 上执行。");
        var options = Options.Parse(args);
        var runner = new ProcessRunner();
        var identityText = await Run(runner, "/usr/bin/security", ["find-identity", "-v", "-p", "codesigning"]);
        var identities = IdentityPattern().Matches(identityText).Select(match => (Hash: match.Groups[1].Value, Name: match.Groups[2].Value)).ToArray();
        if (options.Get("--team") is null || options.Get("--profile") is null)
        {
            Console.WriteLine($"可用签名身份：{identities.Length} 个。请显式指定 --team 与 --profile；不会自动选择证书。");
            return 1;
        }
        var team = options.Required("--team");
        if (!TeamPattern().IsMatch(team))
            throw new ArgumentException("Team ID 必须为 10 位大写字母或数字。");
        var method = options.Get("--method") ?? "release-testing";
        method = method switch
        {
            "development" => "debugging",
            "ad-hoc" => "release-testing",
            _ => method
        };
        if (method is not ("debugging" or "release-testing" or "enterprise"))
            throw new ArgumentException("只支持内部安装签名方式。");
        var bundleId = options.Get("--bundle-id") ?? "com.lianxin.device";
        if (!BundlePattern().IsMatch(bundleId))
            throw new ArgumentException("Bundle ID 无效。");
        var profilePath = Path.GetFullPath(options.Required("--profile"));
        var profile = IosProfile.Parse(await Run(runner, "/usr/bin/security", ["cms", "-D", "-i", profilePath]));
        profile.Validate(team, bundleId, method, (options.Get("--udids") ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries), DateTimeOffset.UtcNow);
        var prefix = method == "debugging" ? "Apple Development:" : "Apple Distribution:";
        var identity = identities.FirstOrDefault(item => profile.Certificates.Contains(item.Hash) &&
            (item.Name.StartsWith(prefix, StringComparison.Ordinal) || item.Name.StartsWith(method == "debugging" ? "iPhone Developer:" : "iPhone Distribution:", StringComparison.Ordinal)));
        if (identity.Hash is null)
            throw new ArgumentException("钥匙串没有与描述文件匹配的证书和私钥。");
        var output = Path.GetFullPath(options.Get("--output") ?? Path.Combine("data", "exports", DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss")));
        if (Directory.Exists(output) || File.Exists(output))
            throw new ArgumentException("输出目录已存在，请选择新目录。");
        Console.WriteLine($"预检通过：{method}，描述文件包含 {profile.Devices.Length} 台设备，到期 {profile.ExpiresAt:u}。");
        if (options.Has("--check-only"))
            return 0;
        var project = Path.GetFullPath(options.Get("--project") ?? "ios/LianxinDevice.xcodeproj");
        if (!Directory.Exists(project))
            throw new ArgumentException("找不到 Xcode 工程。");
        var profileDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library/Developer/Xcode/UserData/Provisioning Profiles");
        Directory.CreateDirectory(profileDirectory);
        var installed = Path.Combine(profileDirectory, profile.Uuid + ".mobileprovision");
        if (File.Exists(installed) && !File.ReadAllBytes(installed).SequenceEqual(File.ReadAllBytes(profilePath)))
            throw new ArgumentException("本机同 UUID 的描述文件内容不同，请先在 Xcode 核对。");
        if (!File.Exists(installed))
            File.Copy(profilePath, installed);
        Directory.CreateDirectory(output);
        var archive = Path.Combine(output, "LianxinDevice.xcarchive");
        var export = Path.Combine(output, "export");
        var exportOptions = Path.Combine(output, "ExportOptions.plist");
        await File.WriteAllTextAsync(exportOptions, ExportOptions(method, team, bundleId, profile.Uuid, identity.Hash));
        await Run(runner, "/usr/bin/xcodebuild", ["-quiet", "-project", project, "-scheme", "LianxinDevice", "-configuration", "Release", "-destination", "generic/platform=iOS", "-archivePath", archive, "archive", $"DEVELOPMENT_TEAM={team}", $"PRODUCT_BUNDLE_IDENTIFIER={bundleId}", "CODE_SIGN_STYLE=Manual", $"CODE_SIGN_IDENTITY={identity.Hash}", $"PROVISIONING_PROFILE_SPECIFIER={profile.Uuid}"], TimeSpan.FromMinutes(30));
        await Run(runner, "/usr/bin/xcodebuild", ["-quiet", "-exportArchive", "-archivePath", archive, "-exportPath", export, "-exportOptionsPlist", exportOptions], TimeSpan.FromMinutes(15));
        Console.WriteLine("已导出安装包：" + export);
        return 0;
    }

    private static async Task<string> Run(IProcessRunner runner, string file, string[] args, TimeSpan? timeout = null)
    {
        var result = await runner.RunAsync(file, args, timeout ?? TimeSpan.FromSeconds(30));
        if (result.ExitCode != 0)
            throw new InvalidOperationException($"{Path.GetFileName(file)} 执行失败：{result.StandardError[..Math.Min(1500, result.StandardError.Length)]}");
        return result.StandardOutput;
    }
    private static string ExportOptions(string method, string team, string bundle, string uuid, string certificate)
    {
        XElement Value(string value) => new("string", value);
        var dict = new XElement("dict", new XElement("key", "method"), Value(method), new XElement("key", "destination"), Value("export"),
            new XElement("key", "teamID"), Value(team), new XElement("key", "signingStyle"), Value("manual"),
            new XElement("key", "signingCertificate"), Value(certificate), new XElement("key", "provisioningProfiles"),
            new XElement("dict", new XElement("key", bundle), Value(uuid)), new XElement("key", "manageAppVersionAndBuildNumber"), new XElement("false"));
        return new XDocument(new XDeclaration("1.0", "UTF-8", null), new XElement("plist", new XAttribute("version", "1.0"), dict)).ToString();
    }
    [GeneratedRegex("\\b([A-F0-9]{40})\\s+\"([^\"]+)\"")]
    private static partial Regex IdentityPattern();
    [GeneratedRegex("^[A-Z0-9]{10}$")]
    private static partial Regex TeamPattern();
    [GeneratedRegex("^[A-Za-z0-9-]+(?:\\.[A-Za-z0-9-]+)+$")]
    private static partial Regex BundlePattern();
}
