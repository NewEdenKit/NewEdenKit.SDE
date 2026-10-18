using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NewEdenKit.Sde.Serialization;

/// <summary>
/// Reads a list of record keys that may be numbers or strings (most datasets use integer keys,
/// a few use string keys) as invariant-culture strings.
/// </summary>
public sealed class SdeKeyListConverter : JsonConverter<IReadOnlyList<string>>
{
    /// <inheritdoc />
    public override IReadOnlyList<string>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;
        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException($"Expected an array of keys, got {reader.TokenType}.");

        var keys = new List<string>();
        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            keys.Add(reader.TokenType switch
            {
                JsonTokenType.String => reader.GetString()!,
                JsonTokenType.Number => reader.TryGetInt64(out var l)
                    ? l.ToString(CultureInfo.InvariantCulture)
                    : reader.GetDouble().ToString(CultureInfo.InvariantCulture),
                _ => throw new JsonException($"Unexpected key token {reader.TokenType}."),
            });
        }

        return keys;
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, IReadOnlyList<string> value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        foreach (var key in value)
        {
            if (long.TryParse(key, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number))
                writer.WriteNumberValue(number);
            else
                writer.WriteStringValue(key);
        }

        writer.WriteEndArray();
    }
}
