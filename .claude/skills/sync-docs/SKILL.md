---
name: sync-docs
description: Update the root CLAUDE.md, the per-folder CLAUDE.md files, README.md, and the architecture tests so they stay accurate after a structural change to this solution (a project added/removed/renamed/moved, a new top-level src/ or tests/ folder, or a changed convention). Use this right after such a change, instead of leaving the docs to drift.
---

# sync-docs

This solution's `CLAUDE.md` files, `README.md`, and the architecture tests in `tests/ArchitectureTests` are
meant to always describe the actual structure and rules. Never let them silently drift from reality. Use this
skill whenever a change alters what they describe.

## When to run this

- A project was added, removed, renamed, or moved between `src/` layers or `tests/` folders.
- A new top-level folder was introduced under `src/` or `tests/`.
- A dependency-direction rule, naming rule, build-configuration rule, or coding-style rule changed.
- Any other change that makes something in `CLAUDE.md` or `README.md` factually wrong.

Do **not** run this for routine feature work that doesn't change the structure or the rules. Most changes
need no doc updates at all.

## Steps

1. **Diff reality against the docs.** Compare the current `src/`/`tests/` tree and
   `subtrack-backend.slnx` against what the root `CLAUDE.md`, the per-folder `CLAUDE.md` files,
   and `README.md` currently claim.

2. **Project added/removed/renamed:**
   - Update the "current projects" list in that layer's `CLAUDE.md`.
   - Update `README.md`'s structure section/tree if it lists individual projects.
   - Confirm (or fix) the project's entry in `subtrack-backend.slnx` under the matching
     `<Folder>`. The architecture tests fail if it's missing or misplaced.

3. **New top-level `src/` or `tests/` folder:**
   - Create a `CLAUDE.md` in it, following the format of the existing per-folder `CLAUDE.md` files: purpose,
     current projects, and rules (what it may reference, what may reference it, naming conventions specific
     to it).
   - Add a row for it to the folder table in the root `CLAUDE.md`, and update the dependency-direction
     diagram if it changes the layering.
   - Add it to `README.md`'s structure section.
   - Add it to `Layer` and `Repository.GetLayer` in `tests/ArchitectureTests/SubTrack.ArchitectureTests`, and
     give it an entry in `AllowedReferences` in `ProjectReferenceTests.cs`. Until then, the architecture tests
     fail with "not under a known layer folder".

4. **Convention changed** (naming, file-scoped namespaces, brace style, dependency direction, build setup):
   - Update the root `CLAUDE.md` section that states the rule.
   - Dependency direction: update `AllowedReferences` in `ProjectReferenceTests.cs` to match.
   - Namespace/brace style: update `.editorconfig` (both the `csharp_*` option and the matching
     `dotnet_diagnostic.IDE*.severity` line).
   - Build setup: update `Directory.Build.props`, `Directory.Packages.props` or `tests/Directory.Build.props`.
   - Update any per-folder `CLAUDE.md` that restates or depends on that rule.
   - Update `.claude/agents/structure-guardian.md` if it checks the rule.
   - Check `README.md` for anything that now contradicts the new rule.

5. **Keep it factual.** Don't invent rationale for a change you don't understand. If it's unclear why a
   change was made or what the new rule should say precisely, ask before rewriting a rule.

6. **Sanity check.** Run `dotnet build` and `dotnet test --solution subtrack-backend.slnx`. Then
   skim the root `CLAUDE.md` table, every per-folder `CLAUDE.md` you touched, and `README.md`'s structure
   section together. They should tell one consistent story with no leftover references to what changed.
