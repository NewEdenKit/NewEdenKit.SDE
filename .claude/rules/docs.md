---
paths:
  - "README.md"
  - "llms.txt"
  - "docs/usage.md"
  - "tests/NewEdenKit.Sde.Tests/Docs/**"
---

# Consumer docs

`README.md`, `docs/usage.md` and `llms.txt` are read by people and AI assistants using the library.

- Code blocks marked `<!-- snippet: name -->` are copies of the `#region snippet: name` blocks in
  `tests/NewEdenKit.Sde.Tests/Docs/UsageExamples.cs`. Edit the region, never the markdown block, then run
  `UPDATE_USAGE_GUIDE=1 dotnet test -f net10.0` to copy it into the docs.
- `UsageGuideTests` fails when a doc block differs from its region, names a region that doesn't exist, or a region isn't
  shown anywhere. Each example that can run offline has a test that runs it against `TestData/sde`.
- Examples that download from CCP or loop forever are compiled but not run; say so in a comment above the method.
- Unmarked code blocks aren't checked. Keep them few (the README's database sketch needs EF Core and is one).
- `llms.txt` follows https://llmstxt.org: title, summary blockquote, quick start, key facts, then `## Docs` and
  `## Optional` link lists with absolute `raw.githubusercontent.com` URLs. Keep it short; detail belongs in `docs/usage.md`.
- When behaviour visible to users changes, update the guide's prose and its "Rules and common mistakes" section too.
