# Using NewEdenKit.Sde

How to get EVE Online's Static Data Export (SDE) into an application with NewEdenKit.Sde. Written for developers and
for AI coding assistants: every example below is compiled and checked by the tests
(`tests/NewEdenKit.Sde.Tests/Docs/UsageExamples.cs`), so it matches the current API.

## Contents
1. [What the SDE is](#what-the-sde-is)
2. [Install](#install)
3. [Choose an approach](#choose-an-approach)
4. [Quick start](#quick-start)
5. [The client and the archive cache](#the-client-and-the-archive-cache)
6. [Read an SDE build directly](#read-an-sde-build-directly)
7. [Cache models as local files](#cache-models-as-local-files)
8. [Keep it current](#keep-it-current)
9. [Load into your own storage](#load-into-your-own-storage)
10. [Models](#models)
11. [Common lookups](#common-lookups)
12. [Datasets without a typed model](#datasets-without-a-typed-model)
13. [Testing code that uses NewEdenKit.Sde](#testing-code-that-uses-newedenkitsde)
14. [Rules and common mistakes](#rules-and-common-mistakes)

## What the SDE is
The SDE is CCP's export of EVE's static game data: item types, groups, market groups, dogma attributes and effects,
blueprints, the map (regions, systems, stargates, planets, moons), NPC corporations, factions and more. It changes
only with game updates. CCP publishes it at <https://developers.eveonline.com/static-data> as a zip of about 100 MB
holding one JSON Lines file per dataset (`types.jsonl`, `mapSolarSystems.jsonl`, ...), about 100 datasets in all.
Each build has a build number; CCP also publishes a change list per build.

Live data (market prices, character assets, ...) is **not** in the SDE; use ESI for that
([NewEdenKit.ESI](https://github.com/NewEdenKit/NewEdenKit.ESI)).

## Install
```sh
dotnet add package NewEdenKit.Sde
```
Targets .NET 8 and .NET 10. No dependencies beyond the base library (it uses `System.Text.Json`). Namespaces:
`NewEdenKit.Sde` (client, loader, readers, stores, `SdeDatasets`, `LocalizedString`) and `NewEdenKit.Sde.Models`
(the record types).

## Choose an approach
| You want to | Use |
|---|---|
| Keep the datasets you use on disk and load them into memory at startup | `SdeFileStore` with `SdeLoader.LoadAsync` ([details](#cache-models-as-local-files)) |
| Put the SDE in your database, Redis, search index, ... | Implement `ISdeStore` ([details](#load-into-your-own-storage)) |
| Process a build once (export, analysis, a one-off tool) | `SdeLoader.OpenAsync` and stream records ([details](#read-an-sde-build-directly)) |
| Read an SDE zip or folder you already have | `SdeReader.FromArchive(path)` / `SdeReader.FromDirectory(path)` |

## Quick start
<!-- snippet: quickstart -->
```csharp
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
```

## The client and the archive cache
`SdeClient` talks to CCP's endpoints (latest build number, change lists, archive downloads). `SdeLoader` downloads
archives through it into a local cache, and reuses a cached archive instead of downloading it again.

<!-- snippet: client -->
```csharp
// Identify your application to CCP. With IHttpClientFactory, pass the HttpClient it gives you.
var client = new SdeClient(httpClient, new SdeClientOptions
{
    UserAgent = "MyApp/1.0 (admin@example.com)",
});

// Archives (about 100 MB each) are cached in %LOCALAPPDATA%/NewEdenKit/Sde unless you choose a folder.
var loader = new SdeLoader(client, new SdeLoaderOptions { CacheDirectory = "data/sde-archives" });
```

- Set `UserAgent` to identify your application (name, version, contact). The default is `NewEdenKit.Sde/<version>`.
- `SdeClient(HttpClient, options)` works with `IHttpClientFactory`; `new SdeClient(options)` creates its own `HttpClient`
  and disposes it with the client.
- `SdeLoaderOptions.ArchivesToKeep` (default 1) controls how many archives stay in the cache after a load.
- There is no dependency injection extension; register `SdeClient`, `SdeLoader` and your store as singletons yourself
  (they are stateless apart from files on disk).

## Read an SDE build directly
<!-- snippet: read-archive -->
```csharp
// Opens the latest build, downloading it into the archive cache first if needed.
SdeArchiveReader sde = await loader.OpenAsync(cancellationToken: ct);
SdeBuildInfo build = await sde.GetBuildInfoAsync(ct);

// Records are streamed one at a time; nothing is loaded into memory unless you keep it.
await foreach (ItemType type in sde.ReadAsync(SdeDatasets.Types, ct))
{
    if (type.Published && type.MarketGroupId is not null)
        names.Add(type.Name.En!);
}
```

- `ReadAsync` streams. Use `ReadDictionaryAsync` (keyed by `Id`) or `ReadListAsync` to keep a dataset in memory.
- Readers open a new file stream per call, so several datasets can be read concurrently.
- `GetDatasetNames()` and `HasDataset(name)` tell you what the build contains.
- Only the JSON Lines variant can be read. `SdeVariant.Yaml` is for downloading only.

## Cache models as local files
`SdeFileStore` keeps each dataset you ask for in its own JSON Lines file, in the shape of the model it was imported as.
`LoadAsync` downloads and imports only when there is a new build or when you ask for a dataset the store doesn't have.

<!-- snippet: file-store -->
```csharp
var store = new SdeFileStore(directory);

SdeLoadResult result = await loader.LoadAsync(store, new SdeLoadOptions
{
    Datasets = [SdeDatasets.Types, SdeDatasets.Groups, SdeDatasets.Categories, SdeDatasets.SolarSystems],
}, ct);

if (result.UpToDate)
    Console.WriteLine($"Already on build {result.Build.BuildNumber}");
else
    Console.WriteLine($"Imported {string.Join(", ", result.ImportedDatasets.Keys)} from build {result.Build.BuildNumber}");
```

The store is also an `ISdeReader`, so loading into memory is a `ReadDictionaryAsync` per dataset. A typical
application loads what it needs once, into an immutable object it can share:

<!-- snippet: in-memory -->
```csharp
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
```

- `SdeLoadOptions.Datasets` decides what is cached: any list of `SdeDatasets.*`, the presets `SdeDatasets.Items`
  (types, groups, categories, market and meta groups, dogma, blueprints, icons) or `SdeDatasets.All` (every typed
  dataset), plus `SdeDatasets.Raw("name")` or your own `SdeDataset<T>` for anything else.
- Adding a dataset to the list later imports just that dataset. A full import (new build without change history, or
  `Force = true`) replaces the store's contents with exactly the requested datasets.
- Updates are atomic: an import writes a new folder and switches the `current` pointer only on commit. Readers keep
  reading the old folder until they finish; a failed import leaves the previous data in place.
- Run one import at a time per store directory. Reading while an import runs is safe.
- Size: `types` is about 150 MB and `mapMoons` about 210 MB on disk, mostly translations. Cache only what you use.

## Keep it current
<!-- snippet: keep-current -->
```csharp
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
```

- When the store holds an older build, the loader follows CCP's change lists back to it and imports only the datasets
  that changed (`SdeLoadResult.IsIncremental`). If the history can't be followed (more than
  `SdeLoadOptions.MaxChangeSets` builds, default 50, or a missing change list) it does a full import.
- `SdeLoadResult` tells you what happened: `UpToDate`, `Build`, `PreviousBuildNumber`, `ImportedDatasets`
  (name → record count) and `MissingDatasets` (requested but not in the build).

Progress reporting, useful for the first download:

<!-- snippet: progress -->
```csharp
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
```

## Load into your own storage
Implement `ISdeStore` to put the SDE anywhere. The loader asks for the stored build number, then runs an import
session: one `ImportAsync` call per dataset, then `CommitAsync`. Disposing the session without a commit means the
import failed and must be discarded.

<!-- snippet: custom-store -->
```csharp
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
```

<!-- snippet: custom-store-use -->
```csharp
var store = new TypeNameStore();
await loader.LoadAsync(store, new SdeLoadOptions { Datasets = [SdeDatasets.Types] }, ct);
```

Contract:
- `GetBuildNumberAsync` returns the committed build, or `null` when empty. The loader skips the import when it matches.
- `ImportAsync<T>` receives the **complete** dataset each time; replace what you had for it. `records` can be enumerated
  once. Choose the target by `dataset.Name` or by pattern-matching `records` (`records is IAsyncEnumerable<ItemType>`);
  comparing `dataset == SdeDatasets.Types` does not compile inside the generic method.
- `SdeImportContext.IsIncremental` is `true` when only some datasets are imported; keep the others. When `false`,
  the import replaces everything.
- `CommitAsync` makes the import visible and records `context.Build.BuildNumber`. For a database, use a transaction
  opened in `BeginImportAsync`, committed in `CommitAsync` and disposed in `DisposeAsync`.
- `GetDatasetNamesAsync` is optional (default: `null`, meaning "unknown"). Implement it so that datasets you start
  requesting later are imported without waiting for a new build.

## Models
Typed models (namespace `NewEdenKit.Sde.Models`) exist for 30 datasets:

| Area | `SdeDatasets` property → model |
|---|---|
| Items | `Types` → `ItemType`, `Groups` → `ItemGroup`, `Categories` → `ItemCategory`, `MarketGroups` → `MarketGroup`, `MetaGroups` → `MetaGroup`, `TypeMaterials` → `TypeMaterials`, `Icons` → `Icon`, `Graphics` → `Graphic` |
| Dogma | `TypeDogma` → `TypeDogma`, `DogmaAttributes` → `DogmaAttribute`, `DogmaAttributeCategories` → `DogmaAttributeCategory`, `DogmaEffects` → `DogmaEffect`, `DogmaUnits` → `DogmaUnit` |
| Industry | `Blueprints` → `Blueprint` |
| Map | `Regions` → `Region`, `Constellations` → `Constellation`, `SolarSystems` → `SolarSystem`, `Stargates` → `Stargate`, `Stars` → `Star`, `Planets` → `Planet`, `Moons` → `Moon`, `AsteroidBelts` → `AsteroidBelt` |
| NPC | `NpcStations` → `NpcStation`, `StationOperations` → `StationOperation`, `StationServices` → `StationService`, `NpcCorporations` → `NpcCorporation`, `Factions` → `Faction`, `Races` → `Race`, `Bloodlines` → `Bloodline`, `Ancestries` → `Ancestry` |

Conventions:
- `Id` is the record's key (`_key` in the files): the type ID for `ItemType`, `TypeDogma`, `TypeMaterials`; the
  blueprint's type ID for `Blueprint`; the system ID for `SolarSystem`; and so on. IDs are `int`.
- Foreign keys end in `Id` (`GroupId`, `MarketGroupId`, `SolarSystemId`) and are `int?` when the SDE omits them for
  some records.
- Lists are never null; they are empty when the SDE omits them. Flags such as `SolarSystem.Hub` are `false` when omitted.
- Maps the SDE encodes as `[{"_key": k, "_value": v}]` (for example `Race.Skills`, `NpcCorporation.Investors`) are
  `IReadOnlyDictionary<int, T>`.
- Positions are `SdePosition` (meters, `X`/`Y`/`Z`).
- Fields CCP adds after this version are kept in `AdditionalProperties` (a `JsonElement` per field) instead of being
  dropped.
- Models are immutable `record` types; use `with` to make modified copies.

Translated text is a `LocalizedString`:

<!-- snippet: localized -->
```csharp
string? english = type.Name.En;                 // null when that language is missing
string? german = type.Name.Get("de");
string label = type.Name.GetOrDefault("ja");    // falls back to English, then any language, then ""
string text = type.Name.ToString();             // English
string? description = type.Description?.En;     // optional text fields are nullable
```

## Common lookups
Type → group → category and other cross-references are plain dictionary lookups (see `SdeData.CategoryOf` above).

Dogma attribute values of a type (ship stats, module stats, skill attributes):

<!-- snippet: dogma -->
```csharp
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
```

Manufacturing materials for an item:

<!-- snippet: blueprints -->
```csharp
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
```

Map navigation:

<!-- snippet: map -->
```csharp
Dictionary<int, SolarSystem> systems = await sde.ReadDictionaryAsync(SdeDatasets.SolarSystems, ct);
Dictionary<int, Stargate> stargates = await sde.ReadDictionaryAsync(SdeDatasets.Stargates, ct);

SolarSystem system = systems[solarSystemId];
bool highSec = Math.Round(system.SecurityStatus, 1) >= 0.5;   // in-game display rounds to one decimal

// A system's stargates lead to its neighbours.
List<int> neighbours = system.StargateIds
    .Where(stargates.ContainsKey)
    .Select(id => stargates[id].Destination.SolarSystemId)
    .ToList();
```

## Datasets without a typed model
About 70 datasets (skins, certificates, agents, dungeons, missions, `npcCharacters`, ...) have no typed model. Read
them as `JsonElement`:

<!-- snippet: raw -->
```csharp
// Any dataset without a typed model, as JsonElement. sde.GetDatasetNames() lists them all.
await foreach (JsonElement skin in sde.ReadRawAsync("skins", ct))
{
    int id = skin.GetProperty("_key").GetInt32();
    keys.Add(id.ToString());
}
```

Or define a model with just the fields you need; this also works for typed datasets when you want a smaller shape,
and `SdeFileStore` then caches only those fields:

<!-- snippet: custom-model -->
```csharp
// Only the fields you need. Property names match SDE fields case-insensitively; "_key" needs the attribute.
// Deriving from SdeEntity gives you Id and ReadDictionaryAsync support.
public sealed record TypeSummary : SdeEntity
{
    public LocalizedString Name { get; init; } = LocalizedString.Empty;
    public int GroupId { get; init; }
    public double? Volume { get; init; }
}

public static readonly SdeDataset<TypeSummary> TypeSummaries = new("types");
```

<!-- snippet: custom-model-use -->
```csharp
Dictionary<int, TypeSummary> summaries = await sde.ReadDictionaryAsync(TypeSummaries, ct);
```

Raw and custom datasets can be passed to `SdeLoadOptions.Datasets` like the built-in ones.

## Testing code that uses NewEdenKit.Sde
<!-- snippet: testing -->
```csharp
// In your own tests, read a few hand-picked records from a folder of .jsonl files: no network, no zip.
ISdeReader sde = SdeReader.FromDirectory(testDataDirectory);
Dictionary<int, ItemType> types = await sde.ReadDictionaryAsync(SdeDatasets.Types);
```

- Copy a few lines from the real `.jsonl` files into your test project; `SdeReader.FromDirectory` reads them as-is.
- To test code that calls `SdeLoader`, implement `ISdeClient` with a fake that copies a zip of such a folder in
  `DownloadArchiveAsync` (`System.IO.Compression.ZipFile.CreateFromDirectory`).
- `ISdeReader`, `ISdeStore`, `ISdeLoader` and `ISdeClient` are interfaces, so they can be mocked.

## Rules and common mistakes
- **Don't download the SDE yourself** or parse the zip by hand; `SdeLoader` caches, skips unchanged builds and
  imports incrementally.
- **Don't call `LoadAsync` on every request.** Call it at startup and on a schedule (hours, not seconds), then serve
  from memory or your store.
- **Don't load everything.** `SdeDatasets.All` is about 600,000 records; `mapMoons` alone is 340,000. Ask for the
  datasets you use.
- **Names are not keys.** Look things up by ID; names change and are translated. Use `Name.En` for English.
- **`ItemType.Published == false`** marks internal or removed types; filter on it for anything player-facing.
- **Security status** is the true value; round to one decimal the way the game displays it before comparing
  (`0.45` shows as `0.5`, high-sec).
- **Market group, meta group and many other fields are optional.** Check for `null`.
- **Prices** in the SDE (`BasePrice`) are CCP's base values, not market prices. Use ESI for market data.
- **Icons and graphics** are client resource paths (`res:/ui/...`), not URLs. For type icons on the web, use the image
  server: `https://images.evetech.net/types/{typeId}/icon`.
