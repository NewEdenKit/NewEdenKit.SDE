using System.Text.Json;
using System.Text.Json.Serialization;
using NewEdenKit.Sde.Serialization;

namespace NewEdenKit.Sde;

/// <summary>
/// The changes CCP published for one SDE build, relative to the build before it
/// (<see cref="LastBuildNumber"/>).
/// </summary>
public sealed record SdeChangeSet
{
    /// <summary>The build these changes produce.</summary>
    public required long BuildNumber { get; init; }

    /// <summary>The build these changes apply on top of.</summary>
    public required long LastBuildNumber { get; init; }

    /// <summary>When <see cref="BuildNumber"/> was released.</summary>
    public required DateTimeOffset ReleaseDate { get; init; }

    /// <summary>Changes per dataset, keyed by dataset name (for example <c>types</c>).</summary>
    public required IReadOnlyDictionary<string, SdeDatasetChanges> Datasets { get; init; }

    internal static SdeChangeSet Parse(IEnumerable<string> lines)
    {
        long? buildNumber = null, lastBuildNumber = null;
        DateTimeOffset? releaseDate = null;
        var datasets = new Dictionary<string, SdeDatasetChanges>(StringComparer.Ordinal);

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            using var document = JsonDocument.Parse(line);
            var root = document.RootElement;
            var key = root.GetProperty("_key").GetString()!;
            if (key == "_meta")
            {
                buildNumber = root.GetProperty("buildNumber").GetInt64();
                lastBuildNumber = root.GetProperty("lastBuildNumber").GetInt64();
                releaseDate = root.GetProperty("releaseDate").GetDateTimeOffset();
                continue;
            }

            datasets[key] = root.Deserialize<SdeDatasetChanges>(SdeJson.Options)!;
        }

        if (buildNumber is null || lastBuildNumber is null || releaseDate is null)
            throw new SdeException("Change set is missing its '_meta' record.");

        return new SdeChangeSet
        {
            BuildNumber = buildNumber.Value,
            LastBuildNumber = lastBuildNumber.Value,
            ReleaseDate = releaseDate.Value,
            Datasets = datasets,
        };
    }
}

/// <summary>The keys of records that changed in one dataset. Keys are formatted as invariant strings.</summary>
public sealed record SdeDatasetChanges : SdeRecord
{
    /// <summary>The dataset name (for example <c>types</c>).</summary>
    [JsonPropertyName("_key")]
    public string Dataset { get; init; } = string.Empty;

    /// <summary>Keys of records added in this build.</summary>
    [JsonConverter(typeof(SdeKeyListConverter))]
    public IReadOnlyList<string> Added { get; init; } = [];

    /// <summary>Keys of records whose data changed in this build.</summary>
    [JsonConverter(typeof(SdeKeyListConverter))]
    public IReadOnlyList<string> Changed { get; init; } = [];

    /// <summary>Keys of records removed in this build.</summary>
    [JsonConverter(typeof(SdeKeyListConverter))]
    public IReadOnlyList<string> Removed { get; init; } = [];

    /// <summary>Keys of records where only translations changed in this build.</summary>
    [JsonConverter(typeof(SdeKeyListConverter))]
    public IReadOnlyList<string> ChangedLocalization { get; init; } = [];
}
