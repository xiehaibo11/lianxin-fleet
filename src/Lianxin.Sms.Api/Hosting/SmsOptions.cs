namespace Lianxin.Sms.Api.Hosting;

public sealed record SmsOptions(string DatabasePath = ":memory:", bool Demo = false,
    string? AdminPassword = null, string AdminEmail = "admin@lianxin.local", string? PublicOrigin = null);
