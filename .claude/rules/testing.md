---
paths:
  - "tests/**"
---

# Testing

- xUnit with its built-in `Assert`. No other assertion or mocking libraries; write small fakes instead.
- Test names are sentences in snake case: `Reads_build_info`, `Falls_back_to_full_import_when_change_history_is_incomplete`.
- Arrange/act/assert, one behaviour per test. Use `[Theory]` for input variations.
- Tests run on net8.0 and net10.0. Async LINQ (`ToListAsync`, `CountAsync`) comes from `System.Linq.AsyncEnumerable`
  on net8.0; don't use other .NET 10-only APIs in tests.
- **No network.** Never call CCP's endpoints. Use:
  - `TestSde`: the sample in `TestData/sde` (`TestSde.Directory`, `TestSde.CreateArchive(dir, build)`)
  - `SdeLoaderTests.FakeClient`: an `ISdeClient` serving the sample as an archive, with settable change sets and a download counter
  - a `FakeHandler` (`HttpMessageHandler` with canned responses, private to `SdeClientTests`) for `SdeClient` tests
  - `TempDirectory`: a disposable temp folder for caches and stores
- **Models:** `Typed_models_map_every_field` fails when any field in the sample lands in `AdditionalProperties`.
  The sample must contain, across its records, every field the full SDE has for each typed dataset.
- **Loader:** cover up-to-date, full, incremental (several change sets), broken change history, missing datasets,
  datasets added later, and failed imports (nothing committed, session disposed).
- **File store:** cover atomic commit, carry-over on incremental imports, cleanup of old folders, and reading during an import.
- **Docs:** `Docs/UsageExamples.cs` holds every consumer-doc example in `#region snippet: name` blocks.
  `UsageGuideTests` checks the docs match and runs the examples against the sample.
