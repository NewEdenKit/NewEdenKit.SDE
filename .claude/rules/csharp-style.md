---
paths:
  - "**/*.cs"
  - "**/*.csproj"
  - "**/*.props"
---

# C# style

## Project settings (Directory.Build.props, don't override per project)
- `TargetFrameworks`: `net8.0;net10.0`, `LangVersion` latest
- `Nullable` enable, `ImplicitUsings` enable, `TreatWarningsAsErrors` true, `AnalysisLevel` latest-recommended
- Package metadata, MinVer, SourceLink and symbols are set in `Directory.Build.props` for `src/` projects
- Package versions only in `Directory.Packages.props` (central package management). `src/` takes no runtime dependencies.

## API surface
- Only what consumers need is `public`. Helpers are `internal`. Tests see internals via `InternalsVisibleTo`.
- Every public member has XML docs (`GenerateDocumentationFile`, CS1591 is an error).
- Every public API change updates `src/NewEdenKit.Sde/PublicAPI.Unshipped.txt` (RS0016/RS0017 are errors).
- Don't add a second overload with optional parameters (RS0026). Add an overload without them instead.
- New members on public interfaces need a default implementation (as `ISdeStore.GetDatasetNamesAsync` has), or they
  break every user's implementation.
- Async methods end in `Async`, return `Task`/`ValueTask`/`IAsyncEnumerable<T>`, and take `CancellationToken` as the
  last parameter (`cancellationToken = default` on public methods).
- No .NET 9+ only APIs unless guarded with `#if NET9_0_OR_GREATER` and given a net8 fallback.

## Models (`src/NewEdenKit.Sde/Models`)
- `public sealed record` with `{ get; init; }` properties. Top-level records derive from `SdeEntity`; nested objects from `SdeRecord`.
- Small fixed-shape values (`SdePosition`, `StargateDestination`) are `readonly record struct`.
- PascalCase names that match the SDE field case-insensitively (`groupID` → `GroupId`). Use `[JsonPropertyName]` only
  when they can't (`_key`, `research_material`).
- Translated text is `LocalizedString`; required text defaults to `LocalizedString.Empty`, optional text is `LocalizedString?`.
- `[{"_key": k, "_value": v}]` maps are `IReadOnlyDictionary<int, T>` with `[JsonConverter(typeof(KeyedValueListConverter<T>))]`.
- Keep SDE/EVE names even when an analyzer objects (`DogmaAttribute`, `Guid`): suppress with
  `[SuppressMessage(..., Justification = "...")]` on the member, never project-wide.

## Code
- File-scoped namespaces: `NewEdenKit.Sde`, `NewEdenKit.Sde.Models`, `NewEdenKit.Sde.Serialization`.
- `ConfigureAwait(false)` on every `await` in library code (CA2007), including `await using` and `await foreach`.
- Guard public inputs with `ArgumentNullException.ThrowIfNull` / `ArgumentException.ThrowIfNullOrWhiteSpace`.
- Data problems throw `SdeException` with the dataset name (and line number when reading).
- Files written by the library appear atomically: write to a temporary name, then move.
- `var` when the type is obvious from the right-hand side.
- Comments explain *why*, not *what*.
