using System.Text.Json;
using Lianxin.Fleet.Core.Serialization;

namespace Lianxin.Fleet.Core.Persistence;

// 工作站本机设置，保存在数据目录的 settings.json。API 密钥只保存加密后的内容，由界面层负责加解密。
public sealed record FleetSettings
{
    public string? PythonPath
    {
        get; init;
    }
    public string? RunnerPackage
    {
        get; init;
    }
    public int RefreshSeconds { get; init; } = 3;
    public string[] HiddenColumns { get; init; } = [];
    public string AgentModel { get; init; } = "claude-opus-5-5";
    public int AgentMaxSteps { get; init; } = 25;
    public string? AgentKeyProtected
    {
        get; init;
    }

    public static FleetSettings Load(string path)
    {
        try
        {
            var settings = JsonSerializer.Deserialize<FleetSettings>(File.ReadAllText(path), FleetJson.Options) ?? new();
            return settings with
            {
                RefreshSeconds = Math.Clamp(settings.RefreshSeconds, 1, 60),
                AgentMaxSteps = Math.Clamp(settings.AgentMaxSteps, 1, 200),
                HiddenColumns = settings.HiddenColumns ?? []
            };
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException)
        {
            return new();
        }
    }

    // 先写临时文件再替换，避免写到一半时断电留下损坏的设置。
    public void Save(string path)
    {
        var pending = path + ".tmp";
        File.WriteAllText(pending, JsonSerializer.Serialize(this, FleetJson.Options));
        File.Move(pending, path, overwrite: true);
    }
}
