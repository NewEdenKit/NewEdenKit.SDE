using NewEdenKit.Sde.Models;

namespace NewEdenKit.Sde.Tests;

public sealed class SdeLoaderTests : IDisposable
{
    private readonly TempDirectory _temp = new();
    private readonly FakeClient _client = new();
    private readonly SdeLoader _loader;

    public SdeLoaderTests()
    {
        _loader = new SdeLoader(_client, new SdeLoaderOptions { CacheDirectory = Path.Combine(_temp.Path, "cache") });
    }

    public void Dispose() => _temp.Dispose();

    [Fact]
    public async Task Imports_all_datasets_into_empty_store()
    {
        var store = new InMemoryStore();

        var result = await _loader.LoadAsync(store);

        Assert.False(result.UpToDate);
        Assert.False(result.IsIncremental);
        Assert.Null(result.PreviousBuildNumber);
        Assert.Equal(TestSde.BuildNumber, result.Build.BuildNumber);
        Assert.Equal(TestSde.BuildNumber, store.BuildNumber);
        Assert.Equal(SdeDatasets.All.Count, result.ImportedDatasets.Count);
        Assert.Equal(store.Records["types"].Count, result.ImportedDatasets["types"]);
        Assert.Contains(store.Records["types"].Cast<ItemType>(), t => t.Name.En == "Tritanium");
        Assert.Equal(1, _client.Downloads);
    }

    [Fact]
    public async Task Does_nothing_when_store_is_up_to_date()
    {
        var store = new InMemoryStore { BuildNumber = TestSde.BuildNumber };

        var result = await _loader.LoadAsync(store);

        Assert.True(result.UpToDate);
        Assert.Empty(store.Records);
        Assert.Equal(0, _client.Downloads);
    }

    [Fact]
    public async Task Force_reimports_current_build()
    {
        var store = new InMemoryStore { BuildNumber = TestSde.BuildNumber };

        var result = await _loader.LoadAsync(store, new SdeLoadOptions { Force = true });

        Assert.False(result.UpToDate);
        Assert.NotEmpty(store.Records);
    }

    [Fact]
    public async Task Reuses_cached_archive()
    {
        await _loader.LoadAsync(new InMemoryStore());
        await _loader.LoadAsync(new InMemoryStore());

        Assert.Equal(1, _client.Downloads);
    }

    [Fact]
    public async Task Imports_only_changed_datasets_across_several_builds()
    {
        _client.Changes[TestSde.BuildNumber] = ChangeSet(TestSde.BuildNumber, 200, "types", "icons", "skins");
        _client.Changes[200] = ChangeSet(200, 100, "mapSolarSystems");
        var store = new InMemoryStore { BuildNumber = 100 };

        var result = await _loader.LoadAsync(store);

        Assert.True(result.IsIncremental);
        Assert.True(store.LastContext!.IsIncremental);
        Assert.Equal(["icons", "mapSolarSystems", "types"], result.ImportedDatasets.Keys.Order());
        Assert.Equal(TestSde.BuildNumber, store.BuildNumber);
    }

    [Fact]
    public async Task Falls_back_to_full_import_when_change_history_is_incomplete()
    {
        _client.Changes[TestSde.BuildNumber] = ChangeSet(TestSde.BuildNumber, 200, "types");
        var store = new InMemoryStore { BuildNumber = 100 };

        var result = await _loader.LoadAsync(store);

        Assert.False(result.IsIncremental);
        Assert.Equal(SdeDatasets.All.Count, result.ImportedDatasets.Count);
    }

    [Fact]
    public async Task Imports_only_requested_datasets_and_reports_progress()
    {
        var store = new InMemoryStore();
        var progress = new List<SdeLoadProgress>();

        var result = await _loader.LoadAsync(store, new SdeLoadOptions
        {
            Datasets = [SdeDatasets.Types, SdeDatasets.Raw("skins"), SdeDatasets.Raw("notInArchive")],
            Progress = new SynchronousProgress<SdeLoadProgress>(progress.Add),
        });

        Assert.Equal(["skins", "types"], result.ImportedDatasets.Keys.Order());
        Assert.Equal(["notInArchive"], result.MissingDatasets);
        Assert.Equal(4, progress.Count);
        Assert.Equal(result.ImportedDatasets["types"], progress[1].RecordCount);
    }

    [Fact]
    public async Task Does_not_commit_when_import_fails()
    {
        var store = new InMemoryStore { FailOn = "groups" };

        await Assert.ThrowsAsync<InvalidOperationException>(() => _loader.LoadAsync(store));

        Assert.Null(store.BuildNumber);
        Assert.Empty(store.Records);
        Assert.True(store.SessionDisposed);
    }

    [Fact]
    public async Task Prunes_old_archives()
    {
        TestSde.CreateArchive(_loader.Cache.Directory, buildNumber: 1);
        TestSde.CreateArchive(_loader.Cache.Directory, buildNumber: 2);

        await _loader.LoadAsync(new InMemoryStore());

        Assert.Equal([TestSde.BuildNumber], _loader.Cache.GetCachedBuilds());
    }

    private static SdeChangeSet ChangeSet(long build, long last, params string[] datasets) => new()
    {
        BuildNumber = build,
        LastBuildNumber = last,
        ReleaseDate = DateTimeOffset.UnixEpoch,
        Datasets = datasets.ToDictionary(d => d, d => new SdeDatasetChanges { Dataset = d, Changed = ["1"] }),
    };

    internal sealed class FakeClient : ISdeClient
    {
        public Dictionary<long, SdeChangeSet> Changes { get; } = [];

        public int Downloads { get; private set; }

        public Task<SdeBuildInfo> GetLatestBuildAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new SdeBuildInfo(TestSde.BuildNumber, DateTimeOffset.UnixEpoch));

        public Task<SdeChangeSet?> GetChangesAsync(long buildNumber, CancellationToken cancellationToken = default) =>
            Task.FromResult(Changes.GetValueOrDefault(buildNumber));

        public Uri GetArchiveUri(long buildNumber, SdeVariant variant = SdeVariant.JsonLines) => new("https://example.invalid/");

        public Task DownloadArchiveAsync(long buildNumber, string destinationPath, SdeVariant variant = SdeVariant.JsonLines,
            IProgress<SdeDownloadProgress>? progress = null, CancellationToken cancellationToken = default)
        {
            Downloads++;
            File.Move(TestSde.CreateArchive(Path.Combine(Path.GetDirectoryName(destinationPath)!, "staging"), buildNumber), destinationPath);
            return Task.CompletedTask;
        }
    }

    /// <summary>A minimal store: records are staged per session and only become visible on commit.</summary>
    private sealed class InMemoryStore : ISdeStore
    {
        public long? BuildNumber { get; set; }

        public Dictionary<string, List<object>> Records { get; } = [];

        public SdeImportContext? LastContext { get; private set; }

        public string? FailOn { get; init; }

        public bool SessionDisposed { get; private set; }

        public Task<long?> GetBuildNumberAsync(CancellationToken cancellationToken = default) => Task.FromResult(BuildNumber);

        public Task<ISdeImportSession> BeginImportAsync(SdeImportContext context, CancellationToken cancellationToken = default)
        {
            LastContext = context;
            return Task.FromResult<ISdeImportSession>(new Session(this, context));
        }

        private sealed class Session(InMemoryStore store, SdeImportContext context) : ISdeImportSession
        {
            private readonly Dictionary<string, List<object>> _staged = [];

            public async Task ImportAsync<T>(SdeDataset<T> dataset, IAsyncEnumerable<T> records, CancellationToken cancellationToken = default)
            {
                if (dataset.Name == store.FailOn)
                    throw new InvalidOperationException("boom");

                var list = new List<object>();
                await foreach (var record in records.WithCancellation(cancellationToken))
                    list.Add(record!);
                _staged[dataset.Name] = list;
            }

            public Task CommitAsync(CancellationToken cancellationToken = default)
            {
                foreach (var (name, list) in _staged)
                    store.Records[name] = list;
                store.BuildNumber = context.Build.BuildNumber;
                return Task.CompletedTask;
            }

            public ValueTask DisposeAsync()
            {
                store.SessionDisposed = true;
                return ValueTask.CompletedTask;
            }
        }
    }
}
