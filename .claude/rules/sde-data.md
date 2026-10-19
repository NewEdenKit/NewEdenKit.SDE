---
paths:
  - "src/NewEdenKit.Sde/Models/**"
  - "src/NewEdenKit.Sde/SdeDatasets.cs"
  - "tests/NewEdenKit.Sde.Tests/TestData/**"
---

# SDE data and models

## Looking at the data
- Get a build with the library (`SdeLoader.OpenAsync`) or download
  `https://developers.eveonline.com/static-data/eve-online-static-data-latest-jsonl.zip` into a scratch folder.
  Never into the repository.
- Never read a `.jsonl` file whole: `mapMoons` is ~220 MB, `types` ~150 MB. Use `head -n 2`, or a script that streams
  lines and reports what you need (field names, their JSON types, how many records have them).
- Field types must be checked across **every** record: a field can be an integer in most records and a float in a few.
- Keys (`_key`) are integers in every dataset except `_sde`, `characterTitles`, `militaryCampaigns`,
  `militaryCampaignObjectives` and `translationLanguages` (strings).
- Schema changes between builds: `https://developers.eveonline.com/static-data/tranquility/schema-changelog.yaml`.

## Adding a typed dataset
1. Add the model in `Models/` (see `csharp-style.md`, "Models"), with property types taken from the full data.
2. Add the `SdeDataset<T>` property to `SdeDatasets`, and add it to `All` (and to `Items` if it's item data).
3. Add records to `TestData/sde/<dataset>.jsonl`: real lines copied from the SDE, as few as possible but together
   containing every field the dataset has. Append them; keep lines byte-for-byte as CCP wrote them.
4. Verify against the full SDE once: read every record of the dataset and check that `AdditionalProperties` is empty
   everywhere (including nested `SdeRecord`s).
5. Record the API (`RS0016`), and update the model tables in `docs/usage.md` and `README.md`.

## Changing a model when CCP changes the data
- New field: map it (minor change). Until then it's in `AdditionalProperties`, so nothing breaks.
- Field removed or retyped: change the model, mark the PR breaking (`!`).
- Refresh the affected sample records from the new build.
