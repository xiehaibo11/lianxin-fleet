using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

public sealed class SourceGovernance : Task
{
    [Required]
    public string RepositoryRoot { get; set; }

    public ITaskItem[] Sources { get; set; } = new ITaskItem[0];
    public bool SelfTest { get; set; }

    private static readonly Dictionary<string, string[]> Categories = new Dictionary<string, string[]>
    {
        ["Lianxin.Fleet.Core"] = new[] { "Domain", "Devices", "Persistence", "Services", "Validation", "Infrastructure", "Serialization", "Agents", "Imaging", "OpenApi" },
        ["Lianxin.Fleet.Cli"] = new[] { "Commands", "Signing", "Properties" },
        ["Lianxin.Fleet.Desktop"] = new[] { "Views", "ViewModels", "Interaction", "Resources" },
        ["Lianxin.Fleet.Mac"] = new[] { "Views", "ViewModels", "Interaction", "Resources" },
        ["Lianxin.Sms.Api"] = new[] { "Hosting", "Routing", "Persistence", "Security", "Validation", "Protocol" },
        ["Lianxin.Fleet.Checks"] = new[] { "Scenarios", "Infrastructure" },
        ["Lianxin.Sms.Checks"] = new[] { "Scenarios", "Infrastructure" }
    };

    public override bool Execute()
    {
        if (SelfTest)
        {
            VerifyRules();
            Log.LogMessage(MessageImportance.High, "治理规则自检通过：行数边界、类别、命名空间、入口及生成文件排除。");
            return true;
        }
        var root = Path.GetFullPath(RepositoryRoot).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var comparison = Path.DirectorySeparatorChar == '\\' ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        var files = Sources.Select(item => item.GetMetadata("FullPath")).Distinct().ToArray();
        var checkedFiles = 0;
        foreach (var file in files)
        {
            if (!file.StartsWith(root, comparison))
                continue;
            var relative = file.Substring(root.Length).Replace('\\', '/');
            var parts = relative.Split('/');
            if (parts.Any(part => part == "obj" || part == "bin" || part == "data" || part == "dist"))
                continue;
            if (!File.Exists(file))
            {
                Error("GOV004", file, "源文件引用不存在，请同步更新移动后的项目链接。");
                continue;
            }
            var lines = File.ReadAllLines(file);
            checkedFiles++;
            if (lines.Length > 300)
                Error("GOV001", file, "文件为 " + lines.Length + " 行，超过 300 行上限；请按职责拆分。");
            if (parts.Length < 3 || (parts[0] != "src" && parts[0] != "checks"))
                continue;
            string[] categories;
            if (!Categories.TryGetValue(parts[1], out categories))
            {
                Error("GOV002", file, "项目尚未登记职责类别，请先更新治理规则。");
                continue;
            }
            var name = parts[parts.Length - 1];
            if (parts.Length == 3)
            {
                var entry = name == "Program.cs"
                    || (parts[1] == "Lianxin.Fleet.Desktop" && (name == "App.xaml" || name == "App.xaml.cs"))
                    || (parts[1] == "Lianxin.Fleet.Mac" && (name == "App.axaml" || name == "App.axaml.cs"));
                if (!entry)
                    Error("GOV002", file, "业务代码需放入职责类别目录，项目根目录仅保留程序入口。");
            }
            else if (!categories.Contains(parts[2]))
            {
                Error("GOV002", file, "未识别的职责类别：" + parts[2]);
            }
            if (!name.EndsWith(".cs", StringComparison.Ordinal) || name == "AssemblyInfo.cs")
                continue;
            var declaration = lines.Select(line => line.Trim()).FirstOrDefault(line => line.StartsWith("namespace ", StringComparison.Ordinal));
            if (declaration == null)
            {
                if (name != "Program.cs")
                    Error("GOV003", file, "C# 源文件必须声明与目录对应的命名空间。");
                continue;
            }
            var actual = declaration.Substring("namespace ".Length).Trim().TrimEnd(';', '{').Trim();
            var expected = string.Join(".", parts.Skip(1).Take(parts.Length - 2));
            if (actual != expected)
                Error("GOV003", file, "命名空间应为 " + expected + "，当前为 " + actual);
        }
        Log.LogMessage(MessageImportance.Low, "治理检查：{0} 个源文件，300 行上限及类别/命名空间已检查。", checkedFiles);
        return !Log.HasLoggedErrors;
    }

    private void Error(string code, string file, string message)
    {
        Log.LogError(null, code, null, file, 1, 1, 1, 1, message);
    }

    private static void VerifyRules()
    {
        var root = Path.Combine(Path.GetTempPath(), "lianxin-policy-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var domain = Path.Combine(root, "src", "Lianxin.Fleet.Core", "Domain");
            Directory.CreateDirectory(domain);
            var file = Path.Combine(domain, "Boundary.cs");
            File.WriteAllLines(file, new[] { "namespace Lianxin.Fleet.Core.Domain;" }
                .Concat(Enumerable.Repeat("// boundary fixture", 299)));
            VerifyCase(root, file, null);
            File.AppendAllText(file, "// line 301" + Environment.NewLine);
            VerifyCase(root, file, "GOV001");
            File.WriteAllText(file, "namespace Wrong.Namespace;");
            VerifyCase(root, file, "GOV003");
            var loose = Path.Combine(root, "src", "Lianxin.Fleet.Core", "Loose.cs");
            File.WriteAllText(loose, "namespace Lianxin.Fleet.Core;");
            VerifyCase(root, loose, "GOV002");
            var entry = Path.Combine(root, "src", "Lianxin.Fleet.Cli", "Program.cs");
            Directory.CreateDirectory(Path.GetDirectoryName(entry));
            File.WriteAllText(entry, "Console.WriteLine(\"entry\");");
            VerifyCase(root, entry, null);
            var generated = Path.Combine(root, "src", "Lianxin.Fleet.Core", "obj", "Generated.cs");
            Directory.CreateDirectory(Path.GetDirectoryName(generated));
            File.WriteAllLines(generated, Enumerable.Repeat("// generated fixture", 301));
            VerifyCase(root, generated, null);
            VerifyCase(root, Path.Combine(domain, "Missing.cs"), "GOV004");
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    private static void VerifyCase(string root, string file, string expectedCode)
    {
        var engine = new RecordingBuildEngine();
        var task = new SourceGovernance
        {
            BuildEngine = engine,
            RepositoryRoot = root,
            Sources = new ITaskItem[] { new TaskItem(file) }
        };
        var success = task.Execute();
        if (success != (expectedCode == null) || (expectedCode != null && !engine.Codes.Contains(expectedCode)))
            throw new InvalidOperationException("治理规则自检失败：" + file + "，预期 " + (expectedCode ?? "通过"));
    }

    private sealed class RecordingBuildEngine : IBuildEngine
    {
        public readonly List<string> Codes = new List<string>();
        public bool ContinueOnError { get { return false; } }
        public int LineNumberOfTaskNode { get { return 1; } }
        public int ColumnNumberOfTaskNode { get { return 1; } }
        public string ProjectFileOfTaskNode { get { return "governance-self-test.proj"; } }
        public void LogErrorEvent(BuildErrorEventArgs error) { Codes.Add(error.Code); }
        public void LogWarningEvent(BuildWarningEventArgs warning) { }
        public void LogMessageEvent(BuildMessageEventArgs message) { }
        public void LogCustomEvent(CustomBuildEventArgs message) { }
        public bool BuildProjectFile(string project, string[] targets, IDictionary properties, IDictionary outputs) { return false; }
    }
}
