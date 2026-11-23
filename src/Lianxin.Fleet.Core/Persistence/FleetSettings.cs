using System.Text.Json;
using Lianxin.Fleet.Core.Serialization;

namespace Lianxin.Fleet.Core.Persistence;

// 工作站本机设置，保存在数据目录的 settings.json。API 密钥只保存加密后的内容，由界面层负责加解密。
public sealed record FleetSettings
{
    // EasyClick 开放接口文档的默认端口。
    public const int DefaultOpenApiPort = 8019;

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
    // 开放接口（兼容 EasyClick 企业版接口），默认关闭；开启后默认只监听 127.0.0.1。
    public bool OpenApiEnabled
    {
        get; init;
    }
    public int OpenApiPort { get; init; } = DefaultOpenApiPort;
    // 监听全部网卡，局域网内其他电脑可访问，此时必须带访问令牌。
    public bool OpenApiAllowLan
    {
        get; init;
    }
    // 访问令牌：Windows 保存 DPAPI 加密后的内容；macOS 不用此项，令牌在数据目录的 openapi-token 文件（仅当前用户可读）。
    public string? OpenApiTokenProtected
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
                HiddenColumns = settings.HiddenColumns ?? [],
                OpenApiPort = settings.OpenApiPort is >= 1024 and <= 65535 ? settings.OpenApiPort : DefaultOpenApiPort
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
