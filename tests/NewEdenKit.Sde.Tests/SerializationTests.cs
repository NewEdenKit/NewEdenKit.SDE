using System.Text.Json;
using System.Text.Json.Serialization;
using NewEdenKit.Sde.Serialization;

namespace NewEdenKit.Sde.Tests;

public sealed class SerializationTests
{
    [Fact]
    public void Localized_string_reads_all_languages()
    {
        var value = JsonSerializer.Deserialize<LocalizedString>("""{"en": "Rifter", "de": "Rifter DE", "zh": "裂谷级"}""", SdeJson.Options)!;

        Assert.Equal("Rifter", value.En);
        Assert.Equal("Rifter DE", value.De);
        Assert.Equal("裂谷级", value["zh"]);
        Assert.Null(value.Fr);
        Assert.Equal("Rifter", value.GetOrDefault("fr"));
        Assert.Equal("Rifter", value.ToString());
    }

    [Fact]
    public void Localized_string_accepts_plain_string_as_english()
    {
        var value = JsonSerializer.Deserialize<LocalizedString>("\"Jita\"", SdeJson.Options)!;

        Assert.Equal("Jita", value.En);
    }

    [Fact]
    public void Localized_string_round_trips()
    {
        var value = new LocalizedString(new Dictionary<string, string> { ["en"] = "a", ["fr"] = "b" });

        var json = JsonSerializer.Serialize(value, SdeJson.Options);

        Assert.Equal(value, JsonSerializer.Deserialize<LocalizedString>(json, SdeJson.Options));
    }

    private sealed record WithMap
    {
        [JsonConverter(typeof(KeyedValueListConverter<double>))]
        public IReadOnlyDictionary<int, double> Map { get; init; } = new Dictionary<int, double>();
    }

    [Theory]
    [InlineData("""{"map": [{"_key": 1, "_value": 0.5}, {"_key": 7, "_value": 2}]}""")]
    [InlineData("""{"map": {"1": 0.5, "7": 2}}""")]
    public void Keyed_value_list_reads_both_encodings(string json)
    {
        var value = JsonSerializer.Deserialize<WithMap>(json, SdeJson.Options)!;

        Assert.Equal(0.5, value.Map[1]);
        Assert.Equal(2.0, value.Map[7]);
    }

    [Fact]
    public void Keyed_value_list_round_trips()
    {
        var value = new WithMap { Map = new Dictionary<int, double> { [3] = 1.5 } };

        var json = JsonSerializer.Serialize(value, SdeJson.Options);

        Assert.Equal(1.5, JsonSerializer.Deserialize<WithMap>(json, SdeJson.Options)!.Map[3]);
    }

    [Fact]
    public void Unknown_fields_are_kept()
    {
        var type = JsonSerializer.Deserialize<Models.ItemType>("""{"_key": 1, "groupID": 2, "brandNewField": 42}""", SdeJson.Options)!;

        Assert.Equal(2, type.GroupId);
        Assert.Equal(42, type.AdditionalProperties!["brandNewField"].GetInt32());
    }
}
