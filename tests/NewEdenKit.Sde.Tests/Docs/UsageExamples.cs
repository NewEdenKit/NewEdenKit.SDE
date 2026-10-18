using System.Text.Json;
using NewEdenKit.Sde.Models;

namespace NewEdenKit.Sde.Tests.Docs;

/// <summary>
/// The examples in <c>docs/usage.md</c>, <c>README.md</c> and <c>llms.txt</c>. Each <c>#region snippet: name</c> is
/// the exact text of the matching snippet in the docs (<see cref="UsageGuideTests"/> checks), so every documented
/// example compiles.
/// </summary>
internal static class UsageExamples
{
    // Compiled but not run: it downloads the real SDE.
    public static async Task QuickStart()
    {
        #region snippet: quickstart
        using var client = new SdeClient(new SdeClientOptions { UserAgent = "MyApp/1.0 (admin@example.com)" });
        var loader = new SdeLoader(client);
        var store = new SdeFileStore("sde-cache");

        // Downloads the SDE only when there is a new build, and caches just these datasets.
        await loader.LoadAsync(store, new SdeLoadOptions
        {
            Datasets = [SdeDatasets.Types, SdeDatasets.Groups],
        });

        Dictionary<int, ItemType> types = await store.ReadDictionaryAsync(SdeDatasets.Types);
        Console.WriteLine(types[34].Name.En); // Tritanium
        #endregion
    }

    public static SdeClient CreateClient(HttpClient httpClient)
    {
        #region snippet: client
        // Identify your application to CCP. With IHttpClientFactory, pass the HttpClient it gives you.
        var client = new SdeClient(httpClient, new SdeClientOptions
        {
            UserAgent = "MyApp/1.0 (admin@example.com)",
        });

        // Archives (about 100 MB each) are cached in %LOCALAPPDATA%/NewEdenKit/Sde unless you choose a folder.
        var loader = new SdeLoader(client, new SdeLoaderOptions { CacheDirectory = "data/sde-archives" });
        #endregion
        _ = loader;
        return client;
    }

    public static async Task<List<string>> ReadArchive(ISdeLoader loader, CancellationToken ct)
    {
        var names = new List<string>();
        #region snippet: read-archive
        // Opens the latest build, downloading it into the archive cache first if needed.
        SdeArchiveReader sde = await loader.OpenAsync(cancellationToken: ct);
        SdeBuildInfo build = await sde.GetBuildInfoAsync(ct);

        // Records are streamed one at a time; nothing is loaded into memory unless you keep it.
        await foreach (ItemType type in sde.ReadAsync(SdeDatasets.Types, ct))
        {
            if (type.Published && type.MarketGroupId is not null)
                names.Add(type.Name.En!);
        }
        #endregion
        _ = build;
        return names;
    }

    public static async Task<SdeLoadResult> CacheModels(ISdeLoader loader, string directory, CancellationToken ct)
    {
        #region snippet: file-store
        var store = new SdeFileStore(directory);

        SdeLoadResult result = await loader.LoadAsync(store, new SdeLoadOptions
        {
            Datasets = [SdeDatasets.Types, SdeDatasets.Groups, SdeDatasets.Categories, SdeDatasets.SolarSystems],
        }, ct);

        if (result.UpToDate)
            Console.WriteLine($"Already on build {result.Build.BuildNumber}");
        else
            Console.WriteLine($"Imported {string.Join(", ", result.ImportedDatasets.Keys)} from build {result.Build.BuildNumber}");
        #endregion
        return result;
    }

    #region snippet: in-memory
    /// <summary>The SDE data an application keeps in memory, loaded once from the file store.</summary>
    public sealed record SdeData(
        IReadOnlyDictionary<int, ItemType> Types,
        IReadOnlyDictionary<int, ItemGroup> Groups,
        IReadOnlyDictionary<int, ItemCategory> Categories)
    {
        public static async Task<SdeData> LoadAsync(SdeFileStore store, CancellationToken ct) => new(
            await store.ReadDictionaryAsync(SdeDatasets.Types, ct),
            await store.ReadDictionaryAsync(SdeDatasets.Groups, ct),
            await store.ReadDictionaryAsync(SdeDatasets.Categories, ct));

        public string CategoryOf(int typeId) =>
            Categories[Groups[Types[typeId].GroupId].CategoryId].Name.En!;
    }
    #endregion

    // Compiled but not run: it loops until cancelled.
    public static async Task KeepCurrent(ISdeLoader loader, SdeFileStore store, SdeLoadOptions options, Action<SdeData> publish, CancellationToken ct)
    {
        #region snippet: keep-current
        // CCP publishes a new SDE with most game updates. Checking is cheap: one small request
        // when nothing changed, and only the changed datasets are imported when something did.
        using var timer = new PeriodicTimer(TimeSpan.FromHours(6));
        do
        {
            SdeLoadResult result = await loader.LoadAsync(store, options, ct);
            if (!result.UpToDate)
                publish(await SdeData.LoadAsync(store, ct)); // swap in the new data
        }
        while (await timer.WaitForNextTickAsync(ct));
        #endregion
    }

    public static async Task<SdeLoadResult> ReportProgress(ISdeLoader loader, SdeFileStore store, CancellationToken ct)
    {
        #region snippet: progress
        var options = new SdeLoadOptions
        {
            Datasets = SdeDatasets.Items,
            DownloadProgress = new Progress<SdeDownloadProgress>(p => Console.Write($"\rDownloading {p.Fraction:P0}")),
            Progress = new Progress<SdeLoadProgress>(p =>
            {
                if (p.RecordCount is { } count)
                    Console.WriteLine($"[{p.DatasetIndex + 1}/{p.DatasetCount}] {p.Dataset}: {count} records");
            }),
        };

        SdeLoadResult result = await loader.LoadAsync(store, options, ct);
        #endregion
        return result;
    }

    #region snippet: custom-store
    /// <summary>Keeps English type names in memory. A database store follows the same pattern.</summary>
    public sealed class TypeNameStore : ISdeStore
    {
        private long? _buildNumber;

        public IReadOnlyDictionary<int, string> Names { get; private set; } = new Dictionary<int, string>();

        public Task<long?> GetBuildNumberAsync(CancellationToken ct = default) => Task.FromResult(_buildNumber);

        // Optional: lets the loader add datasets you start requesting later.
        public Task<IReadOnlyCollection<string>?> GetDatasetNamesAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyCollection<string>?>(_buildNumber is null ? null : ["types"]);

        public Task<ISdeImportSession> BeginImportAsync(SdeImportContext context, CancellationToken ct = default) =>
            Task.FromResult<ISdeImportSession>(new Session(this, context));

        private sealed class Session(TypeNameStore store, SdeImportContext context) : ISdeImportSession
        {
            private Dictionary<int, string>? _names;

            public async Task ImportAsync<T>(SdeDataset<T> dataset, IAsyncEnumerable<T> records, CancellationToken ct = default)
            {
                // Each call carries one complete dataset. Stage it; publish nothing until CommitAsync.
                if (records is IAsyncEnumerable<ItemType> types)
                {
                    _names = [];
                    await foreach (var type in types.WithCancellation(ct))
                        _names[type.Id] = type.Name.En ?? "";
                }
            }

            public Task CommitAsync(CancellationToken ct = default)
            {
                if (_names is not null)
                    store.Names = _names;
                store._buildNumber = context.Build.BuildNumber;
                return Task.CompletedTask;
            }

            // Called after CommitAsync, or instead of it when the import failed: discard staged data here.
            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
    }
    #endregion

    public static async Task<IReadOnlyDictionary<int, string>> UseCustomStore(ISdeLoader loader, CancellationToken ct)
    {
        #region snippet: custom-store-use
        var store = new TypeNameStore();
        await loader.LoadAsync(store, new SdeLoadOptions { Datasets = [SdeDatasets.Types] }, ct);
        #endregion
        return store.Names;
    }

    public static string Localized(ItemType type)
    {
        #region snippet: localized
        string? english = type.Name.En;                 // null when that language is missing
        string? german = type.Name.Get("de");
        string label = type.Name.GetOrDefault("ja");    // falls back to English, then any language, then ""
        string text = type.Name.ToString();             // English
        string? description = type.Description?.En;     // optional text fields are nullable
        #endregion
        _ = (english, german, text, description);
        return label;
    }

    public static async Task<List<(string Name, double Value)>> Attributes(ISdeReader sde, int typeId, CancellationToken ct)
    {
        #region snippet: dogma
        Dictionary<int, DogmaAttribute> attributes = await sde.ReadDictionaryAsync(SdeDatasets.DogmaAttributes, ct);
        Dictionary<int, TypeDogma> dogma = await sde.ReadDictionaryAsync(SdeDatasets.TypeDogma, ct);

        var values = new List<(string Name, double Value)>();
        if (dogma.TryGetValue(typeId, out var typeDogma))
        {
            foreach (TypeDogmaAttribute value in typeDogma.DogmaAttributes)
            {
                // DisplayName is the UI label; Name is the internal name (for example "maxVelocity").
                string name = attributes.TryGetValue(value.AttributeId, out var definition)
                    ? definition.DisplayName?.En ?? definition.Name
                    : $"attribute {value.AttributeId}";
                values.Add((name, value.Value));
            }
        }
        #endregion
        return values;
    }

    public static async Task<List<BlueprintMaterial>> Materials(ISdeReader sde, int productTypeId, CancellationToken ct)
    {
        #region snippet: blueprints
        // Blueprints are keyed by blueprint type ID; find the one whose manufacturing produces the item.
        Blueprint? blueprint = null;
        await foreach (Blueprint candidate in sde.ReadAsync(SdeDatasets.Blueprints, ct))
        {
            if (candidate.Activities.Manufacturing?.Products.Any(p => p.TypeId == productTypeId) == true)
            {
                blueprint = candidate;
                break;
            }
        }

        IReadOnlyList<BlueprintMaterial> materials = blueprint?.Activities.Manufacturing?.Materials ?? [];
        int seconds = blueprint?.Activities.Manufacturing?.Time ?? 0;
        #endregion
        _ = seconds;
        return [.. materials];
    }

    public static async Task<List<int>> Neighbours(ISdeReader sde, int solarSystemId, CancellationToken ct)
    {
        #region snippet: map
        Dictionary<int, SolarSystem> systems = await sde.ReadDictionaryAsync(SdeDatasets.SolarSystems, ct);
        Dictionary<int, Stargate> stargates = await sde.ReadDictionaryAsync(SdeDatasets.Stargates, ct);

        SolarSystem system = systems[solarSystemId];
        bool highSec = Math.Round(system.SecurityStatus, 1) >= 0.5;   // in-game display rounds to one decimal

        // A system's stargates lead to its neighbours.
        List<int> neighbours = system.StargateIds
            .Where(stargates.ContainsKey)
            .Select(id => stargates[id].Destination.SolarSystemId)
            .ToList();
        #endregion
        _ = highSec;
        return neighbours;
    }

    public static async Task<List<string>> Raw(ISdeReader sde, CancellationToken ct)
    {
        var keys = new List<string>();
        #region snippet: raw
        // Any dataset without a typed model, as JsonElement. sde.GetDatasetNames() lists them all.
        await foreach (JsonElement skin in sde.ReadRawAsync("skins", ct))
        {
            int id = skin.GetProperty("_key").GetInt32();
            keys.Add(id.ToString());
        }
        #endregion
        return keys;
    }

    #region snippet: custom-model
    // Only the fields you need. Property names match SDE fields case-insensitively; "_key" needs the attribute.
    // Deriving from SdeEntity gives you Id and ReadDictionaryAsync support.
    public sealed record TypeSummary : SdeEntity
    {
        public LocalizedString Name { get; init; } = LocalizedString.Empty;
        public int GroupId { get; init; }
        public double? Volume { get; init; }
    }

    public static readonly SdeDataset<TypeSummary> TypeSummaries = new("types");
    #endregion

    public static async Task<Dictionary<int, TypeSummary>> CustomModel(ISdeReader sde, CancellationToken ct)
    {
        #region snippet: custom-model-use
        Dictionary<int, TypeSummary> summaries = await sde.ReadDictionaryAsync(TypeSummaries, ct);
        #endregion
        return summaries;
    }

    public static async Task<string> Testing(string testDataDirectory)
    {
        #region snippet: testing
        // In your own tests, read a few hand-picked records from a folder of .jsonl files: no network, no zip.
        ISdeReader sde = SdeReader.FromDirectory(testDataDirectory);
        Dictionary<int, ItemType> types = await sde.ReadDictionaryAsync(SdeDatasets.Types);
        #endregion
        return types[34].Name.En!;
    }
}
