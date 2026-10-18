using System.Text.Json;
using NewEdenKit.Sde.Models;
using NewEdenKit.Sde.Serialization;

namespace NewEdenKit.Sde.Tests;

public sealed class SdeFileStoreTests : IDisposable
{
    private readonly TempDirectory _temp = new();
    private readonly SdeLoaderTests.FakeClient _client = new();
    private readonly SdeLoader _loader;
    private readonly SdeFileStore _store;

    public SdeFileStoreTests()
    {
        _loader = new SdeLoader(_client, new SdeLoaderOptions { CacheDirectory = Path.Combine(_temp.Path, "cache") });
        _store = new SdeFileStore(Path.Combine(_temp.Path, "store"));
    }

    public void Dispose() => _temp.Dispose();

    private Task<SdeLoadResult> Load(params SdeDataset[] datasets) =>
        _loader.LoadAsync(_store, new SdeLoadOptions { Datasets = datasets });

    [Fact]
    public async Task Empty_store()
    {
        Assert.True(_store.IsEmpty);
        Assert.Null(await _store.GetBuildNumberAsync());
        Assert.Empty(_store.GetDatasetNames());
        Assert.Throws<SdeException>(() => _store.ReadAsync(SdeDatasets.Types));
    }

    [Fact]
    public async Task Caches_each_dataset_in_its_own_file_and_reads_it_back()
    {
        await Load([.. SdeDatasets.All]);

        Assert.Equal(TestSde.BuildNumber, await _store.GetBuildNumberAsync());
        Assert.Equal(TestSde.BuildNumber, (await _store.GetBuildInfoAsync()).BuildNumber);
        Assert.Equal(SdeDatasets.All.Select(d => d.Name).Order(), _store.GetDatasetNames().Order());
        Assert.All(SdeDatasets.All, d => Assert.True(File.Exists(Path.Combine(_store.CurrentDirectory!, d.FileName))));

        var types = await _store.ReadDictionaryAsync(SdeDatasets.Types);
        Assert.Equal("Tritanium", types[34].Name.En);
        Assert.Equal(18, types[34].GroupId);
    }

    [Fact]
    public async Task Cached_records_match_the_source_exactly()
    {
        await Load([.. SdeDatasets.All]);
        var source = SdeReader.FromDirectory(TestSde.Directory);

        foreach (var dataset in SdeDatasets.All)
        {
            var expected = await source.ReadRawAsync(dataset.Name)
                .Select(r => Normalize(r.Deserialize(dataset.RecordType, SdeJson.Options)))
                .ToListAsync();
            var actual = await _store.ReadRawAsync(dataset.Name)
                .Select(r => Normalize(r.Deserialize(dataset.RecordType, SdeJson.Options)))
                .ToListAsync();

            Assert.Equal(expected, actual);
        }

        static string Normalize(object? record) => JsonSerializer.Serialize(record, SdeJson.WriteOptions);
    }

    [Fact]
    public async Task Raw_datasets_are_cached_as_is()
    {
        await Load(SdeDatasets.Raw("skins"));

        var expected = await SdeReader.FromDirectory(TestSde.Directory).ReadRawAsync("skins").Select(e => e.GetRawText()).ToListAsync();
        var actual = await _store.ReadRawAsync("skins").Select(e => e.GetRawText()).ToListAsync();

        Assert.Equal(expected.Select(Compact), actual.Select(Compact));

        static string Compact(string json) => JsonSerializer.Serialize(JsonDocument.Parse(json).RootElement, SdeJson.WriteOptions);
    }

    [Fact]
    public async Task Caches_only_the_requested_models()
    {
        await Load(SdeDatasets.Types, SdeDatasets.Groups);

        Assert.Equal(["groups", "types"], _store.GetDatasetNames().Order());
    }

    [Fact]
    public async Task Adding_a_model_later_imports_just_that_dataset()
    {
        await Load(SdeDatasets.Types);
        var typesFile = File.ReadAllText(Path.Combine(_store.CurrentDirectory!, "types.jsonl"));

        var result = await Load(SdeDatasets.Types, SdeDatasets.Groups);

        Assert.False(result.UpToDate);
        Assert.True(result.IsIncremental);
        Assert.Equal(["groups"], result.ImportedDatasets.Keys);
        Assert.Equal(["groups", "types"], _store.GetDatasetNames().Order());
        Assert.Equal(typesFile, File.ReadAllText(Path.Combine(_store.CurrentDirectory!, "types.jsonl")));
        Assert.Equal(1, _client.Downloads);
    }

    [Fact]
    public async Task Up_to_date_when_all_models_are_cached()
    {
        await Load(SdeDatasets.Types, SdeDatasets.Groups);

        var result = await Load(SdeDatasets.Types);

        Assert.True(result.UpToDate);
    }

    [Fact]
    public async Task Incremental_update_keeps_unchanged_datasets()
    {
        // Seed the store as if it held an older build.
        await _loader.LoadAsync(_store, new SdeLoadOptions { Datasets = [SdeDatasets.Types, SdeDatasets.Groups] });
        await RewriteBuildNumberAsync(100);
        _client.Changes[TestSde.BuildNumber] = new SdeChangeSet
        {
            BuildNumber = TestSde.BuildNumber,
            LastBuildNumber = 100,
            ReleaseDate = DateTimeOffset.UnixEpoch,
            Datasets = new Dictionary<string, SdeDatasetChanges> { ["types"] = new() { Changed = ["34"] } },
        };

        var result = await Load(SdeDatasets.Types, SdeDatasets.Groups);

        Assert.True(result.IsIncremental);
        Assert.Equal(["types"], result.ImportedDatasets.Keys);
        Assert.Equal(["groups", "types"], _store.GetDatasetNames().Order());
        Assert.NotEmpty(await _store.ReadListAsync(SdeDatasets.Groups));
        Assert.Equal(TestSde.BuildNumber, await _store.GetBuildNumberAsync());
    }

    [Fact]
    public async Task Full_import_replaces_everything()
    {
        await Load(SdeDatasets.Types, SdeDatasets.Groups);

        await _loader.LoadAsync(_store, new SdeLoadOptions { Datasets = [SdeDatasets.Icons], Force = true });

        Assert.Equal(["icons"], _store.GetDatasetNames());
    }

    [Fact]
    public async Task Failed_import_keeps_previous_data_and_cleans_up()
    {
        await Load(SdeDatasets.Types);
        var before = _store.CurrentDirectory;

        var context = new SdeImportContext { Build = new SdeBuildInfo(999, DateTimeOffset.UnixEpoch), Datasets = [SdeDatasets.Groups] };
        await using (var session = await _store.BeginImportAsync(context))
        {
            await session.ImportAsync(SdeDatasets.Groups, SdeReader.FromDirectory(TestSde.Directory).ReadAsync(SdeDatasets.Groups));
            // Not committed.
        }

        Assert.Equal(before, _store.CurrentDirectory);
        Assert.Equal(TestSde.BuildNumber, await _store.GetBuildNumberAsync());
        Assert.Single(Directory.GetDirectories(_store.Directory));
    }

    [Fact]
    public async Task Removes_folders_of_earlier_imports()
    {
        await Load(SdeDatasets.Types);
        await _loader.LoadAsync(_store, new SdeLoadOptions { Datasets = [SdeDatasets.Types], Force = true });

        Assert.Equal([_store.CurrentDirectory!], Directory.GetDirectories(_store.Directory));
    }

    [Fact]
    public async Task Reading_continues_from_a_snapshot_during_an_import()
    {
        await Load(SdeDatasets.Types);
        var enumerator = _store.ReadAsync(SdeDatasets.Types).GetAsyncEnumerator();
        Assert.True(await enumerator.MoveNextAsync());

        await _loader.LoadAsync(_store, new SdeLoadOptions { Datasets = [SdeDatasets.Types], Force = true });

        while (await enumerator.MoveNextAsync())
        {
        }

        await enumerator.DisposeAsync();
        Assert.NotEmpty(await _store.ReadListAsync(SdeDatasets.Types));
    }

    private async Task RewriteBuildNumberAsync(long buildNumber)
    {
        var path = Path.Combine(_store.CurrentDirectory!, "_sde.jsonl");
        var json = $$"""{"_key":"sde","buildNumber":{{buildNumber}},"releaseDate":"2026-01-01T00:00:00Z"}""";
        await File.WriteAllTextAsync(path, json + "\n");
    }
}
