namespace NewEdenKit.Sde;

/// <summary>
/// Describes one SDE dataset (one <c>.jsonl</c> file in the archive) and the type its records are read as.
/// Use the predefined instances on <see cref="SdeDatasets"/>, or <see cref="SdeDatasets.Raw"/> for any other file.
/// </summary>
public abstract class SdeDataset : IEquatable<SdeDataset>
{
    private protected SdeDataset(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
    }

    /// <summary>The dataset name, which is the archive file name without extension (for example <c>types</c>).</summary>
    public string Name { get; }

    /// <summary>The file name inside the JSON Lines archive.</summary>
    public string FileName => Name + ".jsonl";

    /// <summary>The CLR type each record is read as.</summary>
    public abstract Type RecordType { get; }

    internal abstract Task<long> ImportAsync(ISdeReader reader, ISdeImportSession session, CancellationToken cancellationToken);

    /// <inheritdoc />
    public bool Equals(SdeDataset? other) =>
        other is not null && Name == other.Name && RecordType == other.RecordType;

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as SdeDataset);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Name, RecordType);

    /// <inheritdoc />
    public override string ToString() => Name;
}

/// <summary>An SDE dataset whose records are read as <typeparamref name="T"/>.</summary>
/// <typeparam name="T">The record type.</typeparam>
public sealed class SdeDataset<T> : SdeDataset
{
    /// <summary>Describes the dataset <paramref name="name"/> with records read as <typeparamref name="T"/>.</summary>
    public SdeDataset(string name) : base(name)
    {
    }

    /// <inheritdoc />
    public override Type RecordType => typeof(T);

    internal override async Task<long> ImportAsync(ISdeReader reader, ISdeImportSession session, CancellationToken cancellationToken)
    {
        var counter = new CountingAsyncEnumerable<T>(reader.ReadAsync(this, cancellationToken));
        await session.ImportAsync(this, counter, cancellationToken).ConfigureAwait(false);
        return counter.Count;
    }

    private sealed class CountingAsyncEnumerable<TItem>(IAsyncEnumerable<TItem> source) : IAsyncEnumerable<TItem>
    {
        public long Count { get; private set; }

        public async IAsyncEnumerator<TItem> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            await foreach (var item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                Count++;
                yield return item;
            }
        }
    }
}
