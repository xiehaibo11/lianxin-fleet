using System.Security.Cryptography;
using System.Text;
using Lianxin.Sms.Api.Validation;
using Org.BouncyCastle.Crypto.Generators;

namespace Lianxin.Sms.Api.Security;

internal static class Credentials
{
    public static string Token() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    public static string Hash(string value) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    public static string Password(string? password)
    {
        InputValidation.Require(password is { Length: >= 12 and <= 128 }, 400, "密码长度须为 12 至 128 个字符。");
        var salt = Token();
        return salt + ":" + Convert.ToHexStringLower(SCrypt.Generate(Encoding.UTF8.GetBytes(password!), Encoding.UTF8.GetBytes(salt), 16384, 8, 1, 64));
    }
    public static bool VerifyPassword(string password, string record)
    {
        var parts = record.Split(':');
        if (parts.Length != 2)
            return false;
        try
        {
            var expected = Convert.FromHexString(parts[1]);
            var actual = SCrypt.Generate(Encoding.UTF8.GetBytes(password), Encoding.UTF8.GetBytes(parts[0]), 16384, 8, 1, 64);
            return expected.Length == 64 && CryptographicOperations.FixedTimeEquals(expected, actual);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
