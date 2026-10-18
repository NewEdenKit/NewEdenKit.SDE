namespace NewEdenKit.Sde;

/// <summary>Downloads and caches SDE builds, and loads them into an <see cref="ISdeStore"/>.</summary>
public interface ISdeLoader
{
    /// <summary>Returns the latest published build.</summary>
    Task<SdeBuildInfo> GetLatestBuildAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a reader for a build, downloading the archive into the cache first if needed.
    /// </summary>
    /// <param name="buildNumber">The build to open, or <see langword="null"/> for the latest.</param>
    /// <param name="downloadProgress">Receives download progress when the archive is not cached yet.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    Task<SdeArchiveReader> OpenAsync(long? buildNumber = null, IProgress<SdeDownloadProgress>? downloadProgress = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Brings <paramref name="store"/> up to date: does nothing if it already holds the target build,
    /// otherwise imports the datasets in <see cref="SdeLoadOptions.Datasets"/> (only the changed ones
    /// when <see cref="SdeLoadOptions.Incremental"/> is set and the change history allows it).
    /// </summary>
    Task<SdeLoadResult> LoadAsync(ISdeStore store, SdeLoadOptions? options = null, CancellationToken cancellationToken = default);
}

/// <summary>Settings for <see cref="SdeLoader"/>.</summary>
public sealed class SdeLoaderOptions
{
    /// <summary>Where downloaded archives are kept. Defaults to <see cref="SdeArchiveCache.DefaultDirectory"/>.</summary>
    public string CacheDirectory { get; set; } = SdeArchiveCache.DefaultDirectory;

    /// <summary>How many archives to keep in the cache after a load. Each is roughly 100 MB. Defaults to 1.</summary>
    public int ArchivesToKeep { get; set; } = 1;
}

/// <summary>Settings for one <see cref="ISdeLoader.LoadAsync"/> call.</summary>
public sealed class SdeLoadOptions
{
    /// <summary>The datasets to import. Defaults to <see cref="SdeDatasets.All"/>.</summary>
    public IReadOnlyList<SdeDataset> Datasets { get; set; } = SdeDatasets.All;

    /// <summary>The build to load, or <see langword="null"/> for the latest.</summary>
    public long? BuildNumber { get; set; }

    /// <summary>Import even when the store already holds the target build.</summary>
    public bool Force { get; set; }

    /// <summary>
    /// When the store holds an older build, import only the datasets CCP's change sets list as changed
    /// since then. Falls back to a full import when the change history is incomplete. Defaults to <see langword="true"/>.
    /// </summary>
    public bool Incremental { get; set; } = true;

    /// <summary>The most change sets to walk back through before falling back to a full import. Defaults to 50.</summary>
    public int MaxChangeSets { get; set; } = 50;

    /// <summary>Receives download progress.</summary>
    public IProgress<SdeDownloadProgress>? DownloadProgress { get; set; }

    /// <summary>Receives progress as each dataset is imported.</summary>
    public IProgress<SdeLoadProgress>? Progress { get; set; }
}

/// <summary>Progress of an import.</summary>
/// <param name="Dataset">The dataset just started or finished.</param>
/// <param name="DatasetIndex">Zero-based position of the dataset in the import.</param>
/// <param name="DatasetCount">Number of datasets in the import.</param>
/// <param name="RecordCount">Records imported, once the dataset has finished; <see langword="null"/> when it is starting.</param>
public readonly record struct SdeLoadProgress(SdeDataset Dataset, int DatasetIndex, int DatasetCount, long? RecordCount);

/// <summary>The outcome of <see cref="ISdeLoader.LoadAsync"/>.</summary>
public sealed record SdeLoadResult
{
    /// <summary>The build the store now holds.</summary>
    public required SdeBuildInfo Build { get; init; }

    /// <summary>The build the store held before, or <see langword="null"/> when it was empty.</summary>
    public long? PreviousBuildNumber { get; init; }

    /// <summary><see langword="true"/> when the store was already up to date and nothing was imported.</summary>
    public bool UpToDate { get; init; }

    /// <summary><see langword="true"/> when only changed datasets were imported.</summary>
    public bool IsIncremental { get; init; }

    /// <summary>Records imported per dataset name.</summary>
    public IReadOnlyDictionary<string, long> ImportedDatasets { get; init; } = new Dictionary<string, long>();

    /// <summary>Requested datasets that the archive did not contain, and were skipped.</summary>
    public IReadOnlyList<string> MissingDatasets { get; init; } = [];
}

/// <summary>Default <see cref="ISdeLoader"/>: downloads through an <see cref="ISdeClient"/> into an <see cref="SdeArchiveCache"/>.</summary>
public sealed class SdeLoader : ISdeLoader
{
    private readonly ISdeClient _client;
    private readonly SdeLoaderOptions _options;

    /// <summary>Creates a loader.</summary>
    public SdeLoader(ISdeClient client, SdeLoaderOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(client);
        _client = client;
        _options = options ?? new SdeLoaderOptions();
        Cache = new SdeArchiveCache(_options.CacheDirectory);
    }

    /// <summary>The archive cache.</summary>
    public SdeArchiveCache Cache { get; }

    /// <inheritdoc />
    public Task<SdeBuildInfo> GetLatestBuildAsync(CancellationToken cancellationToken = default) =>
        _client.GetLatestBuildAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<SdeArchiveReader> OpenAsync(long? buildNumber = null, IProgress<SdeDownloadProgress>? downloadProgress = null, CancellationToken cancellationToken = default)
    {
        var build = buildNumber ?? (await _client.GetLatestBuildAsync(cancellationToken).ConfigureAwait(false)).BuildNumber;
        var path = Cache.GetArchivePath(build);
        if (!File.Exists(path))
            await _client.DownloadArchiveAsync(build, path, SdeVariant.JsonLines, downloadProgress, cancellationToken).ConfigureAwait(false);

        return new SdeArchiveReader(path);
    }

    /// <inheritdoc />
    public async Task<SdeLoadResult> LoadAsync(ISdeStore store, SdeLoadOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(store);
        options ??= new SdeLoadOptions();

        var storedBuild = await store.GetBuildNumberAsync(cancellationToken).ConfigureAwait(false);
        var requested = options.Datasets.Distinct().ToList();

        // Datasets requested now but never imported (for example a model added since the last load).
        var storedDatasets = storedBuild is null ? null : await store.GetDatasetNamesAsync(cancellationToken).ConfigureAwait(false);
        var notInStore = storedDatasets is null
            ? []
            : requested.Where(d => !storedDatasets.Contains(d.Name)).ToList();

        var latest = options.BuildNumber is null ? await _client.GetLatestBuildAsync(cancellationToken).ConfigureAwait(false) : null;
        var targetBuild = options.BuildNumber ?? latest!.BuildNumber;

        // Skip the download entirely when the store already has the latest build.
        if (latest is not null && storedBuild == latest.BuildNumber && notInStore.Count == 0 && !options.Force)
            return new SdeLoadResult { Build = latest, PreviousBuildNumber = storedBuild, UpToDate = true };

        var reader = await OpenAsync(targetBuild, options.DownloadProgress, cancellationToken).ConfigureAwait(false);
        var build = await reader.GetBuildInfoAsync(cancellationToken).ConfigureAwait(false);

        var datasets = requested;
        var incremental = false;
        if (storedBuild == build.BuildNumber && !options.Force)
        {
            if (notInStore.Count == 0)
                return new SdeLoadResult { Build = build, PreviousBuildNumber = storedBuild, UpToDate = true };

            datasets = notInStore;
            incremental = true;
        }
        else if (options.Incremental && !options.Force && storedBuild is { } from && from < build.BuildNumber)
        {
            var changed = await GetChangedDatasetsAsync(from, build.BuildNumber, options.MaxChangeSets, cancellationToken).ConfigureAwait(false);
            if (changed is not null)
            {
                datasets = requested.Where(d => changed.Contains(d.Name) || notInStore.Contains(d)).ToList();
                incremental = true;
            }
        }

        var missing = datasets.Where(d => !reader.HasDataset(d.Name)).Select(d => d.Name).ToList();
        datasets = datasets.Where(d => reader.HasDataset(d.Name)).ToList();

        var context = new SdeImportContext
        {
            Build = build,
            PreviousBuildNumber = storedBuild,
            IsIncremental = incremental,
            Datasets = datasets,
        };

        var imported = new Dictionary<string, long>(StringComparer.Ordinal);
        var session = await store.BeginImportAsync(context, cancellationToken).ConfigureAwait(false);
        await using (session.ConfigureAwait(false))
        {
            for (var i = 0; i < datasets.Count; i++)
            {
                var dataset = datasets[i];
                options.Progress?.Report(new SdeLoadProgress(dataset, i, datasets.Count, null));
                var count = await dataset.ImportAsync(reader, session, cancellationToken).ConfigureAwait(false);
                imported[dataset.Name] = count;
                options.Progress?.Report(new SdeLoadProgress(dataset, i, datasets.Count, count));
            }

            await session.CommitAsync(cancellationToken).ConfigureAwait(false);
        }

        Cache.Prune(Math.Max(1, _options.ArchivesToKeep));

        return new SdeLoadResult
        {
            Build = build,
            PreviousBuildNumber = storedBuild,
            IsIncremental = incremental,
            ImportedDatasets = imported,
            MissingDatasets = missing,
        };
    }

    /// <summary>
    /// Walks the change sets back from <paramref name="toBuild"/> to <paramref name="fromBuild"/> and returns
    /// the names of every dataset that changed, or <see langword="null"/> when the chain cannot be followed.
    /// </summary>
    private async Task<HashSet<string>?> GetChangedDatasetsAsync(long fromBuild, long toBuild, int maxChangeSets, CancellationToken cancellationToken)
    {
        var changed = new HashSet<string>(StringComparer.Ordinal);
        var build = toBuild;
        for (var i = 0; i < maxChangeSets; i++)
        {
            var changes = await _client.GetChangesAsync(build, cancellationToken).ConfigureAwait(false);
            if (changes is null || changes.LastBuildNumber >= build)
                return null;

            changed.UnionWith(changes.Datasets.Keys);
            if (changes.LastBuildNumber == fromBuild)
                return changed;
            if (changes.LastBuildNumber < fromBuild)
                return null;

            build = changes.LastBuildNumber;
        }

        return null;
    }
}
