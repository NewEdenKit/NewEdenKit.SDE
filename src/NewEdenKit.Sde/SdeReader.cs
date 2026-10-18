using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using NewEdenKit.Sde.Serialization;

namespace NewEdenKit.Sde;

/// <summary>
/// Base for readers of JSON Lines SDE data. Each <see cref="ReadAsync{T}"/> call opens its own stream,
/// so datasets can be read concurrently.
/// </summary>
public abstract class SdeReader : ISdeReader
{
    private const string BuildInfoDataset = "_sde";

    /// <summary>Creates a reader that deserializes with <paramref name="jsonOptions"/>, or <see cref="SdeJson.Options"/> when omitted.</summary>
    protected SdeReader(JsonSerializerOptions? jsonOptions = null)
    {
        JsonOptions = jsonOptions ?? SdeJson.Options;
    }

    /// <summary>Options used to deserialize records.</summary>
    protected JsonSerializerOptions JsonOptions { get; }

    /// <summary>Opens a reader over a JSON Lines SDE zip archive.</summary>
    public static SdeReader FromArchive(string zipPath, JsonSerializerOptions? jsonOptions = null) => new SdeArchiveReader(zipPath, jsonOptions);

    /// <summary>Opens a reader over a directory containing the extracted <c>.jsonl</c> files.</summary>
    public static SdeReader FromDirectory(string directory, JsonSerializerOptions? jsonOptions = null) => new SdeDirectoryReader(directory, jsonOptions);

    /// <inheritdoc />
    public abstract IReadOnlyCollection<string> GetDatasetNames();

    /// <inheritdoc />
    public bool HasDataset(string name) => GetDatasetNames().Contains(name, StringComparer.Ordinal);

    /// <summary>Opens the raw stream of <paramref name="name"/>, or returns <see langword="null"/> when it does not exist.</summary>
    protected abstract Stream? OpenDataset(string name);

    /// <inheritdoc />
    public async Task<SdeBuildInfo> GetBuildInfoAsync(CancellationToken cancellationToken = default)
    {
        await foreach (var record in ReadAsync(new SdeDataset<JsonElement>(BuildInfoDataset), cancellationToken).ConfigureAwait(false))
        {
            if (record.TryGetProperty("_key", out var key) && key.ValueEquals("sde"))
                return record.Deserialize<SdeBuildInfo>(JsonOptions)!;
        }

        throw new SdeException("The '_sde' dataset does not contain an 'sde' record.");
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<T> ReadAsync<T>(SdeDataset<T> dataset, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dataset);

        var stream = OpenDataset(dataset.Name) ?? throw new SdeException($"Dataset '{dataset.Name}' was not found.");
        await using var _ = stream.ConfigureAwait(false);
        using var lines = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 64 * 1024);

        var lineNumber = 0;
        while (await lines.ReadLineAsync(cancellationToken).ConfigureAwait(false) is { } line)
        {
            lineNumber++;
            if (line.Length == 0 || string.IsNullOrWhiteSpace(line))
                continue;

            T record;
            try
            {
                record = JsonSerializer.Deserialize<T>(line, JsonOptions)!;
            }
            catch (JsonException ex)
            {
                throw new SdeException($"Could not parse line {lineNumber} of dataset '{dataset.Name}' as {typeof(T).Name}: {ex.Message}", ex);
            }

            yield return record;
        }
    }
}

/// <summary>Reads a JSON Lines SDE zip archive without extracting it.</summary>
public sealed class SdeArchiveReader : SdeReader
{
    private readonly Dictionary<string, string> _entries;

    /// <summary>Opens <paramref name="zipPath"/> and indexes its datasets.</summary>
    public SdeArchiveReader(string zipPath, JsonSerializerOptions? jsonOptions = null) : base(jsonOptions)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(zipPath);
        ArchivePath = Path.GetFullPath(zipPath);

        using var archive = ZipFile.OpenRead(ArchivePath);
        _entries = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var entry in archive.Entries)
        {
            if (entry.Name.EndsWith(".jsonl", StringComparison.OrdinalIgnoreCase))
                _entries[Path.GetFileNameWithoutExtension(entry.Name)] = entry.FullName;
        }

        if (_entries.Count == 0)
            throw new SdeException($"'{ArchivePath}' contains no .jsonl files. Only the JSON Lines variant of the SDE can be read.");
    }

    /// <summary>The full path of the archive.</summary>
    public string ArchivePath { get; }

    /// <inheritdoc />
    public override IReadOnlyCollection<string> GetDatasetNames() => _entries.Keys;

    /// <inheritdoc />
    protected override Stream? OpenDataset(string name)
    {
        if (!_entries.TryGetValue(name, out var entryName))
            return null;

        var archive = ZipFile.OpenRead(ArchivePath);
        try
        {
            return new OwningStream(archive.GetEntry(entryName)!.Open(), archive);
        }
        catch
        {
            archive.Dispose();
            throw;
        }
    }

    /// <summary>A read-only stream that disposes its owner along with itself.</summary>
    private sealed class OwningStream(Stream inner, IDisposable owner) : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override int Read(byte[] buffer, int offset, int count) => inner.Read(buffer, offset, count);
        public override int Read(Span<byte> buffer) => inner.Read(buffer);
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => inner.ReadAsync(buffer, offset, count, cancellationToken);
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) => inner.ReadAsync(buffer, cancellationToken);
        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                inner.Dispose();
                owner.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}

/// <summary>Reads a directory of extracted JSON Lines SDE files.</summary>
public sealed class SdeDirectoryReader : SdeReader
{
    /// <summary>Reads the <c>.jsonl</c> files in <paramref name="directory"/>.</summary>
    public SdeDirectoryReader(string directory, JsonSerializerOptions? jsonOptions = null) : base(jsonOptions)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        Directory = Path.GetFullPath(directory);
        if (!System.IO.Directory.Exists(Directory))
            throw new DirectoryNotFoundException($"SDE directory '{Directory}' does not exist.");
    }

    /// <summary>The full path of the directory.</summary>
    public string Directory { get; }

    /// <inheritdoc />
    public override IReadOnlyCollection<string> GetDatasetNames() =>
        System.IO.Directory.EnumerateFiles(Directory, "*.jsonl").Select(Path.GetFileNameWithoutExtension).ToArray()!;

    /// <inheritdoc />
    protected override Stream? OpenDataset(string name)
    {
        var path = Path.Combine(Directory, name + ".jsonl");
        return File.Exists(path)
            ? new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete, 64 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan)
            : null;
    }
}
