using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ElectronicService.Core.Catalog.Recognition.Evaluation;

public static class EvaluationJson
{
    public static JsonSerializerOptions Options { get; } = new(JsonSerializerDefaults.Web);
    public static string Serialize<T>(T value) => JsonSerializer.Serialize(value, Options);
    public static string Fingerprint<T>(T value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Canonicalize(Serialize(value)))));

    public static string Canonicalize(string json)
    {
        using var document = JsonDocument.Parse(json);
        using var output = new MemoryStream();
        using (var writer = new Utf8JsonWriter(output)) Write(writer, document.RootElement);
        return Encoding.UTF8.GetString(output.ToArray());
    }

    private static void Write(Utf8JsonWriter writer, JsonElement value)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var property in value.EnumerateObject().OrderBy(x => x.Name, StringComparer.Ordinal))
                {
                    writer.WritePropertyName(property.Name);
                    Write(writer, property.Value);
                }
                writer.WriteEndObject();
                break;
            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in value.EnumerateArray()) Write(writer, item);
                writer.WriteEndArray();
                break;
            case JsonValueKind.Number:
                writer.WriteRawValue(value.TryGetDecimal(out var number) ? number.ToString("G29", CultureInfo.InvariantCulture) : value.GetDouble().ToString("R", CultureInfo.InvariantCulture));
                break;
            default:
                value.WriteTo(writer);
                break;
        }
    }
}
