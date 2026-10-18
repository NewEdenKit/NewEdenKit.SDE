using System.Text.Json;

namespace NewEdenKit.Sde;

/// <summary>
/// Reads records from one SDE build in JSON Lines format. Records are streamed one at a time,
/// so even the largest datasets do not need to fit in memory.
/// </summary>
/// <remarks>Implementations are safe to read several datasets concurrently.</remarks>
public interface ISdeReader
{
    /// <summary>Reads the build number and release date from the <c>_sde</c> record.</summary>
    Task<SdeBuildInfo> GetBuildInfoAsync(CancellationToken cancellationToken = default);

    /// <summary>The names of all datasets available (for example <c>types</c>, <c>mapMoons</c>).</summary>
    IReadOnlyCollection<string> GetDatasetNames();

    /// <summary>Whether the dataset <paramref name="name"/> is available.</summary>
    bool HasDataset(string name);

    /// <summary>Streams every record of <paramref name="dataset"/>.</summary>
    /// <exception cref="SdeException">The dataset does not exist or a record cannot be parsed.</exception>
    IAsyncEnumerable<T> ReadAsync<T>(SdeDataset<T> dataset, CancellationToken cancellationToken = default);
}

/// <summary>Convenience methods for <see cref="ISdeReader"/>.</summary>
public static class SdeReaderExtensions
{
    /// <summary>Streams every record of the dataset <paramref name="name"/> as raw JSON.</summary>
    public static IAsyncEnumerable<JsonElement> ReadRawAsync(this ISdeReader reader, string name, CancellationToken cancellationToken = default) =>
        reader.ReadAsync(SdeDatasets.Raw(name), cancellationToken);

    /// <summary>Reads every record of a dataset into a dictionary keyed by <see cref="SdeEntity.Id"/>.</summary>
    public static async Task<Dictionary<int, T>> ReadDictionaryAsync<T>(this ISdeReader reader, SdeDataset<T> dataset, CancellationToken cancellationToken = default)
        where T : SdeEntity
    {
        var result = new Dictionary<int, T>();
        await foreach (var record in reader.ReadAsync(dataset, cancellationToken).ConfigureAwait(false))
            result[record.Id] = record;
        return result;
    }

    /// <summary>Reads every record of a dataset into a list.</summary>
    public static async Task<List<T>> ReadListAsync<T>(this ISdeReader reader, SdeDataset<T> dataset, CancellationToken cancellationToken = default)
    {
        var result = new List<T>();
        await foreach (var record in reader.ReadAsync(dataset, cancellationToken).ConfigureAwait(false))
            result.Add(record);
        return result;
    }
}
