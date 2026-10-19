# NewEdenKit.Sde

Public NuGet library for EVE Online's Static Data Export (SDE, `https://developers.eveonline.com/static-data`).
Repository: https://github.com/NewEdenKit/NewEdenKit.SDE (default branch `main`). License: GPL-3.0-or-later.
Sister library: [NewEdenKit.ESI](https://github.com/NewEdenKit/NewEdenKit.ESI) (live API). Keep conventions aligned with it.

It downloads SDE builds, streams their records as typed models, and loads them into storage the user controls.

```
SdeClient ──download──▶ SdeArchiveCache (zip per build) ──▶ SdeArchiveReader ──records──▶ ISdeImportSession ──▶ ISdeStore
   │ latest.jsonl, changes/<build>.jsonl                          ▲                                 (SdeFileStore or the user's)
   └────────────────────────────── SdeLoader ─────────────────────┘
        decides: up to date? which datasets? incremental (change lists) or full?
```

- `SdeClient` / `ISdeClient`: CCP endpoints only (latest build, change list per build, archive download). Stateless.
- `SdeLoader` / `ISdeLoader`: `OpenAsync` (cache + reader) and `LoadAsync(store)` (the import decision logic).
- `SdeReader` (`SdeArchiveReader`, `SdeDirectoryReader`): streams `.jsonl` records; one stream per call, so concurrent reads are safe.
- `ISdeStore` + `ISdeImportSession`: the user-implemented storage contract. `SdeFileStore` is the built-in implementation
  (one file per dataset, atomic folder swap via a `current` pointer file) and is also an `ISdeReader`.
- `SdeDataset<T>` + `SdeDatasets`: dataset name ↔ record type. `SdeDatasets.Raw(name)` reads any dataset as `JsonElement`.
- `Models/`: immutable `sealed record`s deriving from `SdeEntity` (`Id` = `_key`) or `SdeRecord` (nested objects).

## Core Tech Stack
- Targets: `net8.0;net10.0`, C# latest, nullable enabled. **No package dependencies** in `src/`.
- JSON: `System.Text.Json`, reflection-based, case-insensitive (`SdeJson.Options` to read, `SdeJson.WriteOptions` to write).
  Not trim/AOT-annotated: users can pass their own `SdeDataset<T>` types.
- Testing: xUnit (built-in `Assert`), hand-written fakes. No mocking library.

## Documentation (read the relevant one before working in an area)
- [docs/usage.md](docs/usage.md), [README.md](README.md), [llms.txt](llms.txt): for people and AI assistants **using** the
  library. Their code blocks are generated from `tests/NewEdenKit.Sde.Tests/Docs/UsageExamples.cs`; update them when
  consumer-facing behaviour changes (see `.claude/rules/docs.md`).
- [docs/releasing.md](docs/releasing.md): versioning (SemVer from 0.1.0, MinVer from tags), tag-driven releases, one-time setup.
- CCP's format notes: https://developers.eveonline.com/docs/services/static-data/ (JSON Lines `_key`/`_value` encoding,
  change lists, `schema-changelog.yaml`).

Rules in `.claude/rules/` load automatically. Some are scoped to paths via frontmatter.

## SDE data is huge
A full build is ~100 MB zipped, ~670 MB extracted, 102 `.jsonl` files. `mapMoons.jsonl` is ~220 MB, `types.jsonl` ~150 MB
(eight languages per text field). **Never read an SDE file whole or @-import it.** Inspect with `head -c`, a few lines,
or a short script that streams the file. See `.claude/rules/sde-data.md`.

## Common Commands
- Build: `dotnet build` (warnings are errors, analyzers at `latest-recommended`)
- Test: `dotnet test` (runs on net8.0 and net10.0)
- Format: `dotnet format` (CI runs `dotnet format --verify-no-changes`)
- Record public API changes: `dotnet format analyzers src/NewEdenKit.Sde --diagnostics RS0016 --severity error`
- Refresh doc snippets from `UsageExamples.cs`: `UPDATE_USAGE_GUIDE=1 dotnet test -f net10.0`
- Pack: `dotnet pack -c Release -o artifacts` (version comes from git tags)

## Hard rules
- Model property types come from the **data**, not from names or guesses. Check every record of the dataset in the full
  SDE: a field that is ever a float is `double`; optional fields are nullable; lists default to `[]`. IDs are `int`
  (all SDE keys fit); keep `long` for counts and prices that can exceed it.
- Never drop unknown fields: every model derives from `SdeRecord`, so new CCP fields land in `AdditionalProperties`.
- Library code streams. Never materialize a whole dataset inside `src/` (the user decides what to keep in memory).
- Every `ISdeStore` import is all-or-nothing: nothing is visible before `CommitAsync`, and disposing an uncommitted
  session discards it. Keep `SdeFileStore` and the docs consistent with this contract.
- Never call CCP's endpoints from unit tests. Use `TestData/sde` and the fakes.
