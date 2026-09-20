using System.Text;
using System.Text.Json;
using DDLCard.Core.Contracts;

namespace DDLCard.Core.Services;

public static class PresetCodec
{
    private const string Prefix = "DDLCARD-PRESET-1:";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static string Encode(PresetDocument preset)
    {
        Validate(preset);
        var bytes = JsonSerializer.SerializeToUtf8Bytes(preset, JsonOptions);
        return Prefix + Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    public static PresetDocument Decode(string code)
    {
        var trimmed = code.Trim();
        if (!trimmed.StartsWith(Prefix, StringComparison.Ordinal))
        {
            throw new FormatException("不是有效的 DDLCard 预设码。");
        }

        var payload = trimmed[Prefix.Length..].Replace('-', '+').Replace('_', '/');
        payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');

        try
        {
            var preset = JsonSerializer.Deserialize<PresetDocument>(Convert.FromBase64String(payload), JsonOptions)
                ?? throw new FormatException("预设码内容为空。");
            Validate(preset);
            return preset;
        }
        catch (Exception ex) when (ex is FormatException or JsonException)
        {
            throw new FormatException("预设码损坏或版本不受支持。", ex);
        }
    }

    private static void Validate(PresetDocument preset)
    {
        if (preset.SchemaVersion != PresetDocument.CurrentSchemaVersion)
        {
            throw new FormatException($"不支持预设码版本 {preset.SchemaVersion}。");
        }

        if (preset.CardOpacity is < 0.35 or > 1 || preset.CornerRadius is < 0 or > 64 || preset.FontScale is < 0.75 or > 1.5)
        {
            throw new FormatException("预设码包含超出范围的显示设置。");
        }
    }
}

