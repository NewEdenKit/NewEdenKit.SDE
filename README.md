<img src="assets/icon-120.png" alt="NewEdenKit.Sde" width="120" align="right" />

# NewEdenKit.Sde

A .NET library for the [EVE Online Static Data Export](https://developers.eveonline.com/static-data) (SDE).
It downloads and caches SDE builds, streams their records as typed models, and loads them into storage
you control, such as a database or a cache, through one small interface.

- **No dependencies** beyond the .NET base library. Targets .NET 8 and .NET 10.
- **Streaming.** Records are read one at a time straight from the zip, so even `mapMoons` (about 340,000 records) uses little memory.
- **Typed models** for 30 core datasets (items, dogma, blueprints, map, NPCs). The other 70+ datasets can be read as raw JSON.
- **Incremental updates.** Uses CCP's change sets to re-import only the datasets that changed.
- **Future-proof.** Fields CCP adds later are kept in `AdditionalProperties` instead of being dropped.

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

**Read next:** the [usage guide](docs/usage.md) covers choosing an approach, the file store, keeping data current,
custom stores, models, common lookups (dogma, blueprints, map), untyped datasets, testing, and common mistakes.
Every example in it is compiled by the tests.

**For AI coding assistants:** start from [`llms.txt`](llms.txt) and the [usage guide](docs/usage.md).

## Caching models as local files

`SdeFileStore` is a ready-made store that keeps each dataset you ask for in its own file. Pick the models
you use, let the loader keep them current, and load them into memory when you need them:

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

Files are written in the shape of the model they were imported as, so a custom model
(`new SdeDataset<MyType>("types")`) caches only the fields it maps. Updates are atomic: an import writes
a new folder and switches to it only on commit, so readers never see partial data and a failed
update leaves the previous data in place.

```
sde-cache/
  current                  -> "3607705-1a2b3c4d5e6f"
  3607705-1a2b3c4d5e6f/
    _sde.jsonl
    types.jsonl
    groups.jsonl
    mapSolarSystems.jsonl
```

## Loading into your own storage

To persist the SDE, implement `ISdeStore`. `SdeLoader.LoadAsync` checks which build the store holds,
does nothing if it is current, and otherwise streams each dataset into an import session that you commit.

```csharp
public sealed class MyDbStore(MyDbContext db) : ISdeStore
{
    public async Task<long?> GetBuildNumberAsync(CancellationToken ct) =>
        (await db.SdeInfo.SingleOrDefaultAsync(ct))?.BuildNumber;

    public async Task<ISdeImportSession> BeginImportAsync(SdeImportContext context, CancellationToken ct) =>
        new Session(db, context, await db.Database.BeginTransactionAsync(ct));

    private sealed class Session(MyDbContext db, SdeImportContext context, IDbContextTransaction tx) : ISdeImportSession
    {
        public async Task ImportAsync<T>(SdeDataset<T> dataset, IAsyncEnumerable<T> records, CancellationToken ct)
        {
            // Each call holds the complete dataset: replace what you had.
            if (records is IAsyncEnumerable<ItemType> types)
            {
                await db.Types.ExecuteDeleteAsync(ct);
                await foreach (var type in types)
                    db.Types.Add(new TypeRow(type.Id, type.Name.En, type.GroupId, type.Volume));
                await db.SaveChangesAsync(ct);
            }
            // ... other datasets, or a generic table keyed by dataset.Name
        }

        public async Task CommitAsync(CancellationToken ct)
        {
            db.SdeInfo.Update(new SdeInfo { BuildNumber = context.Build.BuildNumber });
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }

        public ValueTask DisposeAsync() => tx.DisposeAsync(); // rolls back if not committed
    }
}
```

Then run it at startup or on a schedule:

```csharp
var result = await loader.LoadAsync(new MyDbStore(db), new SdeLoadOptions
{
    Datasets = SdeDatasets.Items, // or SdeDatasets.All, or your own list
    Progress = new Progress<SdeLoadProgress>(p => Console.WriteLine($"{p.Dataset} {p.RecordCount}")),
});

Console.WriteLine(result.UpToDate ? "Already current" : $"Imported build {result.Build.BuildNumber}");
```

When the store holds an older build, the loader follows CCP's change sets back to that build and imports
only the datasets that changed (`SdeImportContext.IsIncremental` is `true`; keep datasets that are not in
`SdeImportContext.Datasets`). If the change history cannot be followed, it falls back to a full import.

If your store implements the optional `GetDatasetNamesAsync`, the loader also imports requested datasets
the store doesn't have yet, so adding a model to `Datasets` takes effect without waiting for a new build.

## Components

| Type | Purpose |
| --- | --- |
| `SdeClient` / `ISdeClient` | CCP's endpoints: latest build, change sets, archive download (JSON Lines or YAML). |
| `SdeLoader` / `ISdeLoader` | Caches archives locally and loads them into an `ISdeStore`. |
| `SdeArchiveCache` | The cache directory (default `%LOCALAPPDATA%/NewEdenKit/Sde`). |
| `SdeReader.FromArchive` / `FromDirectory` | Streams records from a zip or an extracted folder. |
| `ISdeStore` / `ISdeImportSession` | What you implement to persist data. |
| `SdeFileStore` | Built-in store: one file per dataset, readable back into memory. |
| `SdeDatasets` | The typed datasets, `All` and `Items` presets, and `Raw(name)` for anything else. |

## Typed datasets

| Area | Datasets |
| --- | --- |
| Items | `Types`, `Groups`, `Categories`, `MarketGroups`, `MetaGroups`, `TypeMaterials`, `Icons`, `Graphics` |
| Dogma | `TypeDogma`, `DogmaAttributes`, `DogmaAttributeCategories`, `DogmaEffects`, `DogmaUnits` |
| Industry | `Blueprints` |
| Map | `Regions`, `Constellations`, `SolarSystems`, `Stargates`, `Stars`, `Planets`, `Moons`, `AsteroidBelts` |
| NPC | `NpcStations`, `StationOperations`, `StationServices`, `NpcCorporations`, `Factions`, `Races`, `Bloodlines`, `Ancestries` |

Any other dataset in the archive (for example `skins`, `certificates`, `npcCharacters`) can be read as `JsonElement`:

```csharp
await foreach (var skin in sde.ReadRawAsync("skins"))
    Console.WriteLine(skin.GetProperty("_key"));
```

You can also use your own model type for any dataset: `sde.ReadAsync(new SdeDataset<MySkin>("skins"))`.

## Notes

- Text fields are `LocalizedString`: `name.En`, `name.De`, `name.Get("ja")`, or `name.ToString()` for English.
- Map fields that the SDE encodes as `[{"_key": k, "_value": v}]` are exposed as `IReadOnlyDictionary<int, T>`.
- Set `SdeClientOptions.UserAgent` to identify your application to CCP.

## Building

```sh
dotnet build
dotnet test
dotnet pack src/NewEdenKit.Sde -c Release
```

## License

GNU General Public License v3.0. See [LICENSE](LICENSE).

EVE Online and the EVE logo are trademarks of CCP hf. This project is not affiliated with CCP.
