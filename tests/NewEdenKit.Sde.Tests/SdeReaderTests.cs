using System.Collections;
using System.Text.Json;
using NewEdenKit.Sde.Models;

namespace NewEdenKit.Sde.Tests;

public sealed class SdeReaderTests : IDisposable
{
    private readonly TempDirectory _temp = new();

    public void Dispose() => _temp.Dispose();

    public static TheoryData<string> Sources => ["archive", "directory"];

    private SdeReader Open(string source) => source == "archive"
        ? SdeReader.FromArchive(TestSde.CreateArchive(_temp.Path))
        : SdeReader.FromDirectory(TestSde.Directory);

    [Theory]
    [MemberData(nameof(Sources))]
    public async Task Reads_build_info(string source)
    {
        var build = await Open(source).GetBuildInfoAsync();

        Assert.Equal(TestSde.BuildNumber, build.BuildNumber);
        Assert.Equal(new DateTimeOffset(2026, 10, 12, 11, 8, 38, TimeSpan.Zero), build.ReleaseDate);
    }

    [Theory]
    [MemberData(nameof(Sources))]
    public void Lists_datasets(string source)
    {
        var reader = Open(source);

        Assert.Contains("types", reader.GetDatasetNames());
        Assert.True(reader.HasDataset("mapMoons"));
        Assert.False(reader.HasDataset("doesNotExist"));
    }

    [Theory]
    [MemberData(nameof(Sources))]
    public async Task Reads_typed_records(string source)
    {
        var types = await Open(source).ReadDictionaryAsync(SdeDatasets.Types);

        var tritanium = types[34];
        Assert.Equal("Tritanium", tritanium.Name.En);
        Assert.Equal(18, tritanium.GroupId);
        Assert.Equal(1857, tritanium.MarketGroupId);
        Assert.Equal(0.01, tritanium.Volume);
        Assert.True(tritanium.Published);
        Assert.Null(tritanium.MetaGroupId);

        var rifter = types[587];
        Assert.Equal("Rifter", rifter.Name.ToString());
        Assert.Equal(1_067_000.0, rifter.Mass);
        Assert.Equal(2, rifter.RaceId);
        Assert.True(rifter.IsRepackable);
    }

    [Fact]
    public async Task Reads_nested_and_keyed_value_fields()
    {
        var reader = Open("directory");

        var jita = (await reader.ReadDictionaryAsync(SdeDatasets.SolarSystems))[30000142];
        Assert.Equal("Jita", jita.Name.En);
        Assert.Equal(10000002, jita.RegionId);
        Assert.Equal(0.945913, jita.SecurityStatus);
        Assert.True(jita.Hub);
        Assert.False(jita.Corridor);
        Assert.Equal(-1.29064861735e17, jita.Position.X);
        Assert.Equal([22, 65], jita.DisallowedAnchorCategories);
        Assert.Contains(50001248, jita.StargateIds);

        var rifterBlueprint = (await reader.ReadDictionaryAsync(SdeDatasets.Blueprints))[691];
        var manufacturing = Assert.IsType<BlueprintActivity>(rifterBlueprint.Activities.Manufacturing);
        Assert.Equal(587, Assert.Single(manufacturing.Products).TypeId);
        Assert.NotEmpty(manufacturing.Materials);
        Assert.NotNull(rifterBlueprint.Activities.ResearchMaterial);

        var race = Assert.Single(await reader.ReadListAsync(SdeDatasets.Races), r => r.Skills.Count > 0);
        Assert.Equal(4, race.Skills[3300]);

        var corporation = (await reader.ReadListAsync(SdeDatasets.NpcCorporations)).First(c => c.CorporationTrades.Count > 0);
        Assert.NotEqual(0, corporation.CorporationTrades.First().Value);
    }

    [Fact]
    public async Task Reads_untyped_datasets_as_raw_json()
    {
        var skins = await Open("directory").ReadRawAsync("skins").ToListAsync();

        Assert.Equal(2, skins.Count);
        Assert.True(skins[0].TryGetProperty("_key", out _));
    }

    [Fact]
    public async Task Throws_for_missing_dataset()
    {
        var reader = Open("directory");

        var ex = await Assert.ThrowsAsync<SdeException>(() => reader.ReadRawAsync("doesNotExist").ToListAsync().AsTask());
        Assert.Contains("doesNotExist", ex.Message);
    }

    [Fact]
    public async Task Reports_dataset_and_line_for_bad_records()
    {
        File.WriteAllText(Path.Combine(_temp.Path, "types.jsonl"), "{\"_key\": 1, \"groupID\": 1}\n{\"_key\": \"oops\"}\n");

        var ex = await Assert.ThrowsAsync<SdeException>(() => SdeReader.FromDirectory(_temp.Path).ReadListAsync(SdeDatasets.Types));
        Assert.Contains("line 2", ex.Message);
        Assert.Contains("types", ex.Message);
    }

    [Fact]
    public async Task Archive_reader_supports_concurrent_reads()
    {
        var reader = Open("archive");

        var counts = await Task.WhenAll(SdeDatasets.All.Select(d => reader.ReadRawAsync(d.Name).CountAsync().AsTask()));

        Assert.All(counts, c => Assert.True(c > 0));
    }

    /// <summary>
    /// The sample holds every field seen in the full export, so any field a model does not map
    /// would show up in <see cref="SdeRecord.AdditionalProperties"/>.
    /// </summary>
    [Fact]
    public async Task Typed_models_map_every_field()
    {
        var reader = Open("directory");
        var unmapped = new List<string>();

        foreach (var dataset in SdeDatasets.All)
        {
            var count = 0;
            await foreach (var record in reader.ReadRawAsync(dataset.Name))
            {
                count++;
                var typed = record.Deserialize(dataset.RecordType, Serialization.SdeJson.Options);
                CollectUnmapped(typed, dataset.Name, unmapped);
            }

            Assert.True(count > 0, $"Sample has no records for {dataset.Name}.");
        }

        Assert.Empty(unmapped);
    }

    private static void CollectUnmapped(object? value, string path, List<string> unmapped)
    {
        switch (value)
        {
            case SdeRecord record:
                unmapped.AddRange(record.AdditionalProperties?.Keys.Select(k => $"{path}.{k}") ?? []);
                foreach (var property in record.GetType().GetProperties())
                {
                    if (property.Name != nameof(SdeRecord.AdditionalProperties))
                        CollectUnmapped(property.GetValue(record), $"{path}.{property.Name}", unmapped);
                }

                break;
            case IEnumerable items and not string and not LocalizedString and not IDictionary:
                foreach (var item in items)
                    CollectUnmapped(item, path + "[]", unmapped);
                break;
        }
    }
}
