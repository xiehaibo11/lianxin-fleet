using System.Text.Json.Serialization;
using System.Text.Json;

namespace Lianxin.Fleet.Core.Serialization;

public static class FleetJson
{
    public static JsonSerializerOptions Options
    {
        get;
    } = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };
}
