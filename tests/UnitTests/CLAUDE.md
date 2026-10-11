# tests/UnitTests/

See the root `CLAUDE.md` first for the overall layering and naming rules; this file only covers what's
specific to this folder.

## Purpose

Unit tests for services: `src/Services/**` code, tested in isolation with
their dependencies mocked/faked.

Current projects:

- `SubTrack.ClientServices.UnitTests`: tests `SubTrack.ClientServices`.
- `SubTrack.DomainServices.UnitTests`: tests `SubTrack.DomainServices`.

## Rules

- One test project per tested `src/Services` project, named `<ProjectName>.UnitTests`, in its
  own subfolder here, registered under `/tests/UnitTests/` in the `.slnx`.
- A test project references the project it tests, plus `Common`, `Models` and any `*.Abstractions` it needs to
  mock. Nothing in `src/` may ever reference a test project. The architecture tests enforce both.
- The test framework comes from `tests/Directory.Build.props`: xUnit v3 on Microsoft.Testing.Platform, plus
  code coverage and `using Xunit;`. A test `.csproj` only adds its `ProjectReference`s and any extra test
  packages, such as a mocking library, whose versions go in `Directory.Packages.props`.
- The template projects contain no tests yet, so each one passes `--ignore-exit-code 8` ("zero tests ran").
  Remove that `<PropertyGroup>` from a project once it has real tests.
- Keep these tests fast and isolated: no real database, no real network calls. That kind of test belongs in
  `tests/IntegrationTests` instead, once that folder is populated.
