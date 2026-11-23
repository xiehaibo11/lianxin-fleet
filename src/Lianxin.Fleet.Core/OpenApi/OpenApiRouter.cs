namespace Lianxin.Fleet.Core.OpenApi;

public delegate Task<OpenApiReply> OpenApiHandler(OpenApiCall call);

public enum OpenApiRouteKind
{
    Implemented, Unsupported, Pending
}

public sealed record OpenApiRoute(string Method, string Path, OpenApiRouteKind Kind, string Note, OpenApiHandler Handler);

// 路径（区分大小写）到处理函数的映射。新增接口：在对应的 *Routes.cs 里写一行 router.Post("/openapi/xxx", handler)。
// 已登记为 Pending/Unsupported 的路径可以直接用 Post/Get 覆盖，登记顺序无关；同一路径重复实现会报错。
public sealed class OpenApiRouter
{
    private readonly Dictionary<string, OpenApiRoute> routes = new(StringComparer.Ordinal);

    public IReadOnlyCollection<OpenApiRoute> Routes => routes.Values;

    public OpenApiRouter Post(string path, OpenApiHandler handler, string note = "") =>
        Add(new("POST", path, OpenApiRouteKind.Implemented, note, handler));

    // 只用于文档里本来就是 GET 的接口。
    public OpenApiRouter Get(string path, OpenApiHandler handler, string note = "") =>
        Add(new("GET", path, OpenApiRouteKind.Implemented, note, handler));

    // 联信没有对应能力（需要 EasyClick 自己的代理 IPA、蓝牙板或 USB HID 等）的接口。
    public OpenApiRouter Unsupported(string path, string reason, string method = "POST") =>
        Add(new(method, path, OpenApiRouteKind.Unsupported, reason,
            _ => Task.FromResult(OpenApiReply.Fail(OpenApiCodes.Unsupported, "联信 Fleet 暂不支持" + reason))));

    // 已规划、等待对应模块接入的接口，接入时用 Post 覆盖即可。
    public OpenApiRouter Pending(string path, string feature) =>
        Add(new("POST", path, OpenApiRouteKind.Pending, feature,
            _ => Task.FromResult(OpenApiReply.Fail(OpenApiCodes.Pending, $"联信 Fleet 暂不支持{feature}（功能开发中）。"))));

    public OpenApiRoute? Find(string path) => routes.GetValueOrDefault(path);

    private OpenApiRouter Add(OpenApiRoute route)
    {
        if (!route.Path.StartsWith('/') || route.Path.Any(char.IsWhiteSpace))
            throw new ArgumentException("接口路径无效：" + route.Path);
        if (routes.TryGetValue(route.Path, out var existing))
        {
            if (existing.Kind == OpenApiRouteKind.Implemented && route.Kind == OpenApiRouteKind.Implemented)
                throw new InvalidOperationException("接口重复登记：" + route.Path);
            if (existing.Kind == OpenApiRouteKind.Implemented)
                return this;
        }
        routes[route.Path] = route;
        return this;
    }

    // 文档中全部接口的默认登记。
    public static OpenApiRouter CreateDefault()
    {
        var router = new OpenApiRouter();
        UnsupportedRoutes.Register(router);
        CenterRoutes.Register(router);
        AutomationRoutes.Register(router);
        ActionRoutes.Register(router);
        ScreenshotRoutes.Register(router);
        FileRoutes.Register(router);
        return router;
    }
}
