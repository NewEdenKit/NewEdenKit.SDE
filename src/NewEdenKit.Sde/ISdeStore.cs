namespace NewEdenKit.Sde;

/// <summary>
/// Your storage for SDE data: a database, a key/value cache, files, or anything else.
/// Implement this to let <see cref="SdeLoader"/> fill and update it.
/// </summary>
/// <remarks>
/// <para>
/// An import runs as a session: <see cref="BeginImportAsync"/> opens it, the loader calls
/// <see cref="ISdeImportSession.ImportAsync{T}"/> once per dataset, and then
/// <see cref="ISdeImportSession.CommitAsync"/>. If anything fails the session is disposed without
/// being committed, and the store should discard the partial import.
/// </para>
/// <para>
/// Each <see cref="ISdeImportSession.ImportAsync{T}"/> call carries the complete contents of that
/// dataset, so the store should replace whatever it holds for the dataset.
/// </para>
/// </remarks>
public interface ISdeStore
{
    /// <summary>
    /// Returns the build number of the data currently stored, or <see langword="null"/> when the store is empty.
    /// The loader uses this to skip imports when the store is already up to date.
    /// </summary>
    Task<long?> GetBuildNumberAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the names of the datasets the store holds, or <see langword="null"/> when the store does not track this.
    /// When implemented, the loader also imports requested datasets the store does not have yet, even if
    /// the build is unchanged.
    /// </summary>
    Task<IReadOnlyCollection<string>?> GetDatasetNamesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<string>?>(null);

    /// <summary>Starts importing a build.</summary>
    Task<ISdeImportSession> BeginImportAsync(SdeImportContext context, CancellationToken cancellationToken = default);
}

/// <summary>One import into an <see cref="ISdeStore"/>. Disposing it without committing discards the import.</summary>
public interface ISdeImportSession : IAsyncDisposable
{
    /// <summary>
    /// Stores every record of <paramref name="dataset"/>, replacing what the store held for it.
    /// <paramref name="records"/> is streamed and can be enumerated once.
    /// </summary>
    /// <remarks>
    /// Use <see cref="SdeDataset.Name"/> to choose a table or key prefix, or pattern-match the records
    /// (for example <c>if (records is IAsyncEnumerable&lt;ItemType&gt; types)</c>) to map typed records.
    /// </remarks>
    Task ImportAsync<T>(SdeDataset<T> dataset, IAsyncEnumerable<T> records, CancellationToken cancellationToken = default);

    /// <summary>Makes the import permanent and records <see cref="SdeImportContext.Build"/> as the stored build.</summary>
    Task CommitAsync(CancellationToken cancellationToken = default);
}

/// <summary>Describes an import that is about to run.</summary>
public sealed record SdeImportContext
{
    /// <summary>The build being imported.</summary>
    public required SdeBuildInfo Build { get; init; }

    /// <summary>The build the store held before, or <see langword="null"/> when it was empty.</summary>
    public long? PreviousBuildNumber { get; init; }

    /// <summary>
    /// <see langword="true"/> when only some datasets are imported (those that changed since
    /// <see cref="PreviousBuildNumber"/>, or that the store did not have yet). Datasets not in
    /// <see cref="Datasets"/> are still current and should be kept. When <see langword="false"/>,
    /// the import replaces the store's entire contents.
    /// </summary>
    public bool IsIncremental { get; init; }

    /// <summary>The datasets that will be imported in this session.</summary>
    public required IReadOnlyList<SdeDataset> Datasets { get; init; }
}
