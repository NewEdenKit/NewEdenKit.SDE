using System.Text.Json;
using System.Text.Json.Serialization;

namespace NewEdenKit.Sde.Serialization;

/// <summary>
/// Reads the SDE's JSON Lines encoding of integer-keyed maps, <c>[{"_key": 1, "_value": x}, ...]</c>,
/// as a dictionary. A regular JSON object with numeric property names is accepted too.
/// </summary>
/// <typeparam name="TValue">The value type of the map.</typeparam>
public sealed class KeyedValueListConverter<TValue> : JsonConverter<IReadOnlyDictionary<int, TValue>>
{
    /// <inheritdoc />
    public override IReadOnlyDictionary<int, TValue>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var result = new Dictionary<int, TValue>();
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return null;
            case JsonTokenType.StartArray:
                while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                {
                    if (reader.TokenType != JsonTokenType.StartObject)
                        throw new JsonException($"Expected an object in keyed value list, got {reader.TokenType}.");

                    int? key = null;
                    TValue? value = default;
                    while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                    {
                        var property = reader.GetString();
                        reader.Read();
                        switch (property)
                        {
                            case "_key":
                                key = reader.GetInt32();
                                break;
                            case "_value":
                                value = JsonSerializer.Deserialize<TValue>(ref reader, options);
                                break;
                            default:
                                reader.Skip();
                                break;
                        }
                    }

                    if (key is null)
                        throw new JsonException("Keyed value list entry is missing '_key'.");
                    result[key.Value] = value!;
                }

                return result;
            case JsonTokenType.StartObject:
                while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                {
                    var key = int.Parse(reader.GetString()!, System.Globalization.CultureInfo.InvariantCulture);
                    reader.Read();
                    result[key] = JsonSerializer.Deserialize<TValue>(ref reader, options)!;
                }

                return result;
            default:
                throw new JsonException($"Expected an array or object for a keyed value list, got {reader.TokenType}.");
        }
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, IReadOnlyDictionary<int, TValue> value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        foreach (var (key, item) in value)
        {
            writer.WriteStartObject();
            writer.WriteNumber("_key", key);
            writer.WritePropertyName("_value");
            JsonSerializer.Serialize(writer, item, options);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }
}
