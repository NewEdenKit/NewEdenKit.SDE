using System.Text.Json;
using System.Text.RegularExpressions;
using NewEdenKit.Sde.Serialization;

namespace NewEdenKit.Sde;

/// <summary>
/// A ready-made <see cref="ISdeStore"/> that keeps each dataset in its own JSON Lines file, written in the
/// shape of the model it was imported as. It is also an <see cref="ISdeReader"/>, so cached datasets can be
/// loaded back into memory with <see cref="SdeReaderExtensions.ReadDictionaryAsync{T}"/> and friends.
/// </summary>
/// <remarks>
/// <para>
/// Layout: each committed import is a folder (<c>&lt;build&gt;-&lt;id&gt;</c>) holding <c>_sde.jsonl</c> plus one
/// <c>&lt;dataset&gt;.jsonl</c> per dataset, and the file <c>current</c> names the active folder. An import writes
/// a new folder and switches <c>current</c> only on commit, so readers never see a half-written import, and
/// an import that fails leaves the previous data in place.
/// </para>
/// <para>Run one import at a time per directory. Reading while an import runs is safe.</para>
/// </remarks>
public sealed partial class SdeFileStore : ISdeStore, ISdeReader
{
    private const string PointerFileName = "current";
    private const string BuildInfoFileName = "_sde.jsonl";
    private const string StagingPrefix = ".import-";
    private static readonly TimeSpan AbandonedStagingAge = TimeSpan.FromDays(1);

    private readonly JsonSerializerOptions _readOptions;
    private readonly JsonSerializerOptions _writeOptions;

    /// <summary>Creates a store in <paramref name="directory"/>. The directory is created when needed.</summary>
    /// <param name="directory">Where the files are kept.</param>
    /// <param name="readOptions">Options for reading records back; defaults to <see cref="SdeJson.Options"/>.</param>
    /// <param name="writeOptions">Options for writing records; defaults to <see cref="SdeJson.WriteOptions"/>.</param>
    public SdeFileStore(string directory, JsonSerializerOptions? readOptions = null, JsonSerializerOptions? writeOptions = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        Directory = Path.GetFullPath(directory);
        _readOptions = readOptions ?? SdeJson.Options;
        _writeOptions = writeOptions ?? SdeJson.WriteOptions;
    }

    /// <summary>The full path of the store directory.</summary>
    public string Directory { get; }

    /// <summary>The folder holding the current data, or <see langword="null"/> when the store is empty.</summary>
    public string? CurrentDirectory
    {
        get
        {
            var pointer = Path.Combine(Directory, PointerFileName);
            if (!File.Exists(pointer))
                return null;

            var folder = Path.Combine(Directory, File.ReadAllText(pointer).Trim());
            return File.Exists(Path.Combine(folder, BuildInfoFileName)) ? folder : null;
        }
    }

    /// <summary>Whether the store holds any data.</summary>
    public bool IsEmpty => CurrentDirectory is null;

    private SdeDirectoryReader? CurrentReader() =>
        CurrentDirectory is { } folder ? new SdeDirectoryReader(folder, _readOptions) : null;

    private SdeDirectoryReader RequireReader() =>
        CurrentReader() ?? throw new SdeException($"The SDE file store at '{Directory}' is empty. Load it with SdeLoader.LoadAsync first.");

    // ---- ISdeStore ----

    /// <inheritdoc />
    public async Task<long?> GetBuildNumberAsync(CancellationToken cancellationToken = default) =>
        CurrentReader() is { } reader
            ? (await reader.GetBuildInfoAsync(cancellationToken).ConfigureAwait(false)).BuildNumber
            : null;

    /// <inheritdoc />
    public Task<IReadOnlyCollection<string>?> GetDatasetNamesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<string>?>(CurrentReader() is null ? null : GetDatasetNames());

    /// <inheritdoc />
    public Task<ISdeImportSession> BeginImportAsync(SdeImportContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        var staging = Path.Combine(Directory, $"{StagingPrefix}{context.Build.BuildNumber}-{Guid.NewGuid():N}");
        System.IO.Directory.CreateDirectory(staging);
        return Task.FromResult<ISdeImportSession>(new ImportSession(this, context, staging));
    }

    // ---- ISdeReader ----

    /// <inheritdoc />
    public Task<SdeBuildInfo> GetBuildInfoAsync(CancellationToken cancellationToken = default) =>
        RequireReader().GetBuildInfoAsync(cancellationToken);

    /// <summary>The names of the cached datasets (empty when the store is empty).</summary>
    public IReadOnlyCollection<string> GetDatasetNames() =>
        CurrentReader()?.GetDatasetNames().Where(n => n != "_sde").ToArray() ?? [];

    /// <inheritdoc />
    public bool HasDataset(string name) => GetDatasetNames().Contains(name, StringComparer.Ordinal);

    /// <inheritdoc />
    public IAsyncEnumerable<T> ReadAsync<T>(SdeDataset<T> dataset, CancellationToken cancellationToken = default) =>
        RequireReader().ReadAsync(dataset, cancellationToken);

    // ---- Import ----

    private sealed class ImportSession(SdeFileStore store, SdeImportContext context, string staging) : ISdeImportSession
    {
        private bool _committed;

        public async Task ImportAsync<T>(SdeDataset<T> dataset, IAsyncEnumerable<T> records, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(dataset);
            ArgumentNullException.ThrowIfNull(records);

            var path = Path.Combine(staging, dataset.FileName);
            var file = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 1 << 16, FileOptions.Asynchronous);
            await using (file.ConfigureAwait(false))
            {
                var writer = new Utf8JsonWriter(file, new JsonWriterOptions { Encoder = store._writeOptions.Encoder, SkipValidation = true });
                await using (writer.ConfigureAwait(false))
                {
                    await foreach (var record in records.WithCancellation(cancellationToken).ConfigureAwait(false))
                    {
                        JsonSerializer.Serialize(writer, record, store._writeOptions);
                        await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
                        writer.Reset();
                        file.WriteByte((byte)'\n');
                    }
                }
            }
        }

        public async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            // An incremental import only carries the datasets that changed: keep the rest from the current folder.
            if (context.IsIncremental && store.CurrentDirectory is { } current)
            {
                foreach (var file in System.IO.Directory.EnumerateFiles(current, "*.jsonl"))
                {
                    var target = Path.Combine(staging, Path.GetFileName(file));
                    if (Path.GetFileName(file) != BuildInfoFileName && !File.Exists(target))
                        File.Copy(file, target);
                }
            }

            var buildInfo = JsonSerializer.Serialize(new
            {
                _key = "sde",
                buildNumber = context.Build.BuildNumber,
                releaseDate = context.Build.ReleaseDate,
            });
            await File.WriteAllTextAsync(Path.Combine(staging, BuildInfoFileName), buildInfo + "\n", cancellationToken).ConfigureAwait(false);

            // Publish: give the folder its final name, then switch the pointer atomically.
            var folderName = $"{context.Build.BuildNumber}-{Guid.NewGuid().ToString("N")[..12]}";
            System.IO.Directory.Move(staging, Path.Combine(store.Directory, folderName));

            var pointer = Path.Combine(store.Directory, PointerFileName);
            var pointerTemp = pointer + "." + Guid.NewGuid().ToString("N") + ".tmp";
            await File.WriteAllTextAsync(pointerTemp, folderName, cancellationToken).ConfigureAwait(false);
            File.Move(pointerTemp, pointer, overwrite: true);
            _committed = true;

            store.DeleteStaleFolders(keep: folderName);
        }

        public ValueTask DisposeAsync()
        {
            if (!_committed)
                TryDelete(staging);
            return ValueTask.CompletedTask;
        }
    }

    /// <summary>Removes folders from earlier imports. Folders still open by a reader are skipped and retried next time.</summary>
    private void DeleteStaleFolders(string keep)
    {
        foreach (var folder in System.IO.Directory.EnumerateDirectories(Directory))
        {
            var name = Path.GetFileName(folder);
            if (name == keep)
                continue;

            // Another import may still be writing to a recent staging folder.
            if (name.StartsWith(StagingPrefix, StringComparison.Ordinal) &&
                DateTime.UtcNow - System.IO.Directory.GetCreationTimeUtc(folder) < AbandonedStagingAge)
                continue;

            if (name.StartsWith(StagingPrefix, StringComparison.Ordinal) || ImportFolderName().IsMatch(name))
                TryDelete(folder);
        }
    }

    [GeneratedRegex(@"^\d+-[0-9a-f]{12}$")]
    private static partial Regex ImportFolderName();

    private static void TryDelete(string folder)
    {
        try
        {
            System.IO.Directory.Delete(folder, recursive: true);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
