# tests/ArchitectureTests/

See the root `CLAUDE.md` first for the overall layering and naming rules; this file only covers what's
specific to this folder.

## Purpose

Automated enforcement of the structural rules in the `CLAUDE.md` files, so they fail `dotnet test` instead of
relying on someone reading the docs.

Current projects:

- `SubTrack.ArchitectureTests`: reads every `.csproj` under `src/` and `tests/`, plus the `.slnx`, straight
  from disk, and checks that:
  - every project is in a known layer folder, in a subfolder named after it;
  - every `ProjectReference` follows the dependency direction in the root `CLAUDE.md` (a `Common` project
    may also reference other `Common` projects);
  - every unit test project references the `Grains`/`Services` project it is named after;
  - every project on disk is in the `.slnx` under the matching folder, and every `.slnx` entry exists.

## Rules

- These tests parse project files rather than using reflection on built assemblies on purpose. The compiler
  drops unused references from assembly metadata, and the template projects are still empty, so a
  reflection-based check would pass without checking anything.
- This project references no other project in the solution.
- The allowed-reference table in `ProjectReferenceTests.cs` must match the root `CLAUDE.md`. When a layering
  rule changes, update both in the same change. When a new top-level `src/` or `tests/` folder is added,
  extend `Layer` and `Repository.GetLayer`.
- Type-level rules such as naming suffixes are not covered here yet. The `structure-guardian` agent checks
  those.
