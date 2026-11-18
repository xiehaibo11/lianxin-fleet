using System.Globalization;
using System.Security.Cryptography;
using System.Xml.Linq;

namespace Lianxin.Fleet.Cli.Signing;

internal sealed record IosProfile(string Uuid, string Team, string AppId, string Prefix,
    DateTimeOffset ExpiresAt, bool Development, bool AllDevices, string[] Devices, string[] Certificates)
{
    public static IosProfile Parse(string xml)
    {
        var root = XDocument.Parse(xml).Root?.Element("dict") ?? throw new ArgumentException("描述文件格式无效。");
        var fields = ReadDict(root);
        var entitlements = ReadDict(fields["Entitlements"]);
        string Field(string key) => fields.TryGetValue(key, out var value) ? value.Value : "";
        string Entitlement(string key) => entitlements.TryGetValue(key, out var value) ? value.Value : "";
        string[] Array(string key) => fields.TryGetValue(key, out var value) ? value.Elements().Select(item => item.Value).ToArray() : [];
        return new(Field("UUID"), Array("TeamIdentifier").FirstOrDefault() ?? "", Entitlement("application-identifier"),
            Array("ApplicationIdentifierPrefix").FirstOrDefault() ?? "",
            DateTimeOffset.Parse(Field("ExpirationDate"), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal),
            entitlements.TryGetValue("get-task-allow", out var allow) && allow.Name.LocalName == "true",
            fields.TryGetValue("ProvisionsAllDevices", out var all) && all.Name.LocalName == "true",
            Array("ProvisionedDevices"), Array("DeveloperCertificates").Select(value => Convert.ToHexString(SHA1.HashData(Convert.FromBase64String(value)))).ToArray());
    }

    public void Validate(string team, string bundleId, string method, string[] devices, DateTimeOffset now)
    {
        if (!Guid.TryParse(Uuid, out _) || Team != team || ExpiresAt <= now)
            throw new ArgumentException("描述文件 UUID、Team 或有效期不匹配。");
        if (!AppId.StartsWith(Prefix + ".", StringComparison.Ordinal))
            throw new ArgumentException("App ID 前缀无效。");
        var pattern = AppId[(Prefix.Length + 1)..];
        if (pattern != bundleId && !(pattern.EndsWith('*') && !pattern[..^1].Contains('*') && bundleId.StartsWith(pattern[..^1], StringComparison.Ordinal)))
            throw new ArgumentException("描述文件不包含目标 Bundle ID。");
        if (method == "enterprise")
        {
            if (!AllDevices || Development || Devices.Length != 0)
                throw new ArgumentException("企业签名必须使用企业内部发布描述文件。");
        }
        else if (AllDevices || Devices.Length == 0 || Development != (method == "debugging") || devices.Any(device => !Devices.Contains(device)))
            throw new ArgumentException("开发/Ad Hoc 签名需要正确类型的描述文件，且包含全部目标设备。");
    }

    private static Dictionary<string, XElement> ReadDict(XElement dictionary)
    {
        var values = dictionary.Elements().ToArray();
        if (dictionary.Name.LocalName != "dict" || values.Length % 2 != 0)
            throw new ArgumentException("描述文件字典无效。");
        var result = new Dictionary<string, XElement>(StringComparer.Ordinal);
        for (var i = 0; i < values.Length; i += 2)
        {
            if (values[i].Name.LocalName != "key")
                throw new ArgumentException("描述文件字典键无效。");
            result.Add(values[i].Value, values[i + 1]);
        }
        return result;
    }
}
