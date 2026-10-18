using System.Text.Json;
using System.Text.Json.Serialization;

namespace NewEdenKit.Sde.Serialization;

/// <summary>
/// Reads <c>{"en": "...", "de": "..."}</c> objects as <see cref="LocalizedString"/>.
/// A plain JSON string is accepted too and treated as English.
/// </summary>
public sealed class LocalizedStringConverter : JsonConverter<LocalizedString>
{
    /// <inheritdoc />
    public override LocalizedString? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return null;
            case JsonTokenType.String:
                return new LocalizedString(new Dictionary<string, string>(1) { [LocalizedString.DefaultLanguage] = reader.GetString()! });
            case JsonTokenType.StartObject:
                var values = new Dictionary<string, string>(8, StringComparer.Ordinal);
                while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                {
                    var language = reader.GetString()!;
                    reader.Read();
                    if (reader.TokenType == JsonTokenType.String)
                        values[language] = reader.GetString()!;
                    else
                        reader.Skip();
                }

                return new LocalizedString(values);
            default:
                throw new JsonException($"Expected an object or string for a localized string, got {reader.TokenType}.");
        }
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, LocalizedString value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        foreach (var (language, text) in value)
            writer.WriteString(language, text);
        writer.WriteEndObject();
    }
}
