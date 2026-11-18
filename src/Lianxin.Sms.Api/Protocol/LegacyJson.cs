using System.Text;

namespace Lianxin.Sms.Api.Protocol;

internal static class LegacyJson
{
    public static string Quote(string value)
    {
        var result = new StringBuilder("\"");
        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];
            switch (c)
            {
                case '"':
                    result.Append("\\\"");
                    break;
                case '\\':
                    result.Append("\\\\");
                    break;
                case '\b':
                    result.Append("\\b");
                    break;
                case '\f':
                    result.Append("\\f");
                    break;
                case '\n':
                    result.Append("\\n");
                    break;
                case '\r':
                    result.Append("\\r");
                    break;
                case '\t':
                    result.Append("\\t");
                    break;
                default:
                    if (char.IsHighSurrogate(c) && i + 1 < value.Length && char.IsLowSurrogate(value[i + 1]))
                    {
                        result.Append(c).Append(value[++i]);
                    }
                    else if (c < 32 || char.IsSurrogate(c))
                        result.Append("\\u").Append(((int)c).ToString("x4"));
                    else
                        result.Append(c);
                    break;
            }
        }
        return result.Append('"').ToString();
    }
}
