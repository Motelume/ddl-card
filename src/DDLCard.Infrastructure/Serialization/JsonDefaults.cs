using System.Text.Json;
using System.Text.Json.Serialization;

namespace DDLCard.Infrastructure.Serialization;

public static class JsonDefaults
{
    public static JsonSerializerOptions Create(bool indented = false) => new(JsonSerializerDefaults.Web)
    {
        WriteIndented = indented,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };
}

