# Releasing

## Principles
- **Releases are driven by tags.** Pushing a `vX.Y.Z` tag on `main` is the release. Nothing else publishes to nuget.org.
- **Versions come from git** via [MinVer](https://github.com/adamralph/minver). No version numbers are
  written in project files.
- **SemVer**, starting at `0.1.0`.

## Version numbers

### Before 1.0 (`0.x`)
The public API isn't stable yet.

| Change | Bump | Example |
|---|---|---|
| Breaking (incl. a model change forced by CCP removing or retyping an SDE field) | minor | `0.3.2` → `0.4.0` |
| Additive or fix | patch | `0.3.2` → `0.3.3` |

### From 1.0 on
`1.0.0` is tagged once the public API is considered stable.

| Change | Bump |
|---|---|
| Breaking: public type or member removed or renamed, property type changed, required parameter added, any breaking change to `ISdeStore`, `ISdeImportSession`, `ISdeReader`, `ISdeClient` or `ISdeLoader` | **major** |
| Additive: new typed dataset or model property, new option, new optional interface member with a default | minor |
| Fixes, docs, internal changes | patch |

New SDE fields don't force a release: until a model maps them, they're kept in `AdditionalProperties`.
Mapping a new field is a minor change.

### How breaking changes are detected
- `Microsoft.CodeAnalysis.PublicApiAnalyzers`: every public API change shows up in
  `src/NewEdenKit.Sde/PublicAPI.Unshipped.txt` in the PR diff. The build fails when the file is out of date; fix it
  with `dotnet format analyzers src/NewEdenKit.Sde --diagnostics RS0016 --severity error`.
- `EnablePackageValidation` with `PackageValidationBaselineVersion` set to the last released version:
  the build fails on breaking changes unless they're expected. From `1.0` on, this baseline is what
  stops a breaking change from shipping in a minor or patch.
- Before `0.1.0` is released there's no baseline, so package validation is off until then.

## Prereleases (on demand)
There are no automatic prereleases. When a preview is needed, tag `main` with a prerelease suffix:

```
git tag v0.4.0-preview.1
git push origin v0.4.0-preview.1
```
The same release workflow publishes it to nuget.org as a prerelease. A preview leaves the public API files as they
are: entries stay in `PublicAPI.Unshipped.txt` until a stable `vX.Y.Z` ships, since a preview's API may still change.

Untagged CI builds get MinVer's height-based versions (e.g. `0.1.0-alpha.0.3`). They're build artefacts only
and are never published.

## Release steps
1. Make sure `main` is green and contains everything for the release.
2. Decide the version using the tables above. Check the PublicAPI diff since the last tag.
3. Move the entries from `PublicAPI.Unshipped.txt` into `PublicAPI.Shipped.txt` (a `chore: prepare vX.Y.Z` PR).
   Keep `#nullable enable` as the first line of both files.
4. Tag and push (only the `maintainers` team can create `v*` tags; the "Protect release tags" ruleset blocks
   everyone else):
   ```
   git tag vX.Y.Z
   git push origin vX.Y.Z
   ```
5. The `release` GitHub Actions workflow, triggered by `v*` tags, runs in the `nuget` environment. It **waits for a
   maintainer to approve the run** (Actions → the run → Review deployments), then:
   - builds and tests every target
   - packs `NewEdenKit.Sde` with the version from the tag
   - publishes `.nupkg` + `.snupkg` to nuget.org using **NuGet Trusted Publishing** (OIDC, no stored API key). The
     nuget.org trusted-publishing policy must name `NewEdenKit/NewEdenKit.SDE`, `release.yml` and the `nuget`
     environment.
   - creates a GitHub Release with generated notes (from squash-merged PR titles)
6. After the release, set `PackageValidationBaselineVersion` to the new version (in `Directory.Build.props`).

## Package requirements
The packable project (`src/`) gets, from `Directory.Build.props`:
- `PackageId` = project name, `Authors`
- `PackageLicenseExpression` = `GPL-3.0-or-later` (full text in `LICENSE`)
- `PackageProjectUrl` / `RepositoryUrl` = `https://github.com/NewEdenKit/NewEdenKit.SDE`, `RepositoryType` = `git`
- `PackageReadmeFile`, `PackageIcon`; `Description` and `PackageTags` are in the project file
- SourceLink, `ContinuousIntegrationBuild` in CI, deterministic builds
- `IncludeSymbols` + `SymbolPackageFormat=snupkg`
- `GenerateDocumentationFile`

`tests/` projects are not packable.

## One-time setup
Already done for `NewEdenKit.ESI`; repeat for this repository:
- **nuget.org:** add a Trusted Publishing policy for owner `NewEdenKit`, repository `NewEdenKit.SDE`, workflow
  `release.yml`, environment `nuget`.
- **Repository secret** `NUGET_USER`: the nuget.org profile name the policy belongs to.
- **Environment** `nuget` with required reviewers (`maintainers`) and deployments limited to `v*` tags.
- **Rulesets:** "Protect main" (pull requests, code-owner review, the `build` check) and "Protect release tags"
  (only `maintainers` can create `v*` tags).

## Servicing older versions
Normally there's only one supported line: the latest. If a fix is needed for an older line after a newer
one has shipped, branch `release/X.Y` from that line's last tag, fix it there, and tag `vX.Y.Z+1` from that branch.
