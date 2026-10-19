# Git and releases

Full detail: [docs/releasing.md](../../docs/releasing.md).

- `main` is protected ("Protect main" ruleset): no direct pushes. Create a branch (`feat/`, `fix/`, `docs/`, `chore/`)
  and open a PR. PRs need a code-owner review and a green `build` check, and are squash-merged.
- Commit or push only when the user asks.
- PR titles use Conventional Commits (`feat(models): …`, `fix: …`, `!` for breaking). The squash commit takes the PR title.
- Never create, move or push `v*` tags unless the user explicitly asks. A tag is a NuGet release.
- Never put version numbers in `.csproj`/`.props`. MinVer takes them from tags.
- Any change to the public API updates `PublicAPI.Unshipped.txt` in the same PR.
- When a change is breaking (including a model change forced by CCP removing or retyping a field), say so in the PR
  title (`!`) and description. Before 1.0, breaking means a minor bump; from 1.0, a major bump.
- Never commit SDE downloads, extracted archives or `artifacts/`. Test data stays a few hand-picked records.
