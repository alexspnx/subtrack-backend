# CLAUDE.md

This file guides Claude Code (and any other contributor, human or AI) when working in this repository.

## What this is

`subtrack-backend` is the backend API of SubTrack and the contract owner for all clients (web, iOS, Android).
It is an ASP.NET Core WebAPI on .NET 10 with EF Core and PostgreSQL, with no Orleans (see ADR 0001 in the
workspace root, `docs/decisions/`). It was bootstrapped from the `in-real-life-backend-example` template
(commit `f93b5a8`) and keeps that template's project layout. Treat the structure and rules below as
load-bearing.

## Commands

```bash
dotnet build subtrack-backend.slnx                    # also enforces the code-style rules below
dotnet test --solution subtrack-backend.slnx          # includes the architecture tests
dotnet format subtrack-backend.slnx --verify-no-changes
```

Before finishing any task that touches `src/`, `tests/`, a `.csproj`, or the `.slnx`, run the build and the
tests and make sure both pass. The architecture tests (`tests/ArchitectureTests`) fail when a project
reference breaks the layering, or when a project is missing from the `.slnx` or listed under the wrong folder.

To run the API locally, start PostgreSQL, then the WebAPI:

```bash
docker compose -f devops/docker-compose.yml up -d
dotnet run --project src/ApplicationServices/SubTrack.WebAPI
curl http://localhost:5053/health        # Healthy when the database is reachable, 503 otherwise
```

The development connection string (`ConnectionStrings:SubTrack`) is in
`src/ApplicationServices/SubTrack.WebAPI/appsettings.Development.json` and matches `devops/docker-compose.yml`.
EF Core migrations use the `dotnet-ef` local tool pinned in `.config/dotnet-tools.json`
(`dotnet tool restore`, then `dotnet ef ... --project src/Infrastructure/SubTrack.EntityFramework --startup-project src/ApplicationServices/SubTrack.WebAPI`).

## Build configuration

Shared settings live at the repository root, not in individual project files:

- `Directory.Build.props`: `net10.0`, `Nullable=enable`, `ImplicitUsings=enable`, and
  `EnforceCodeStyleInBuild=true` for every project. Don't repeat these in a `.csproj`.
- `Directory.Packages.props`: central package management. Every NuGet version is declared here as a
  `<PackageVersion>`. A `.csproj` uses `<PackageReference Include="..." />` with **no** `Version` attribute.
- `tests/Directory.Build.props`: the test setup (xUnit v3 on Microsoft.Testing.Platform, code coverage,
  `using Xunit;`) for every project under `tests/`.
- `global.json`: pins the .NET 10 SDK band and opts `dotnet test` into Microsoft.Testing.Platform.

A new `.csproj` therefore only contains its `Sdk`, its `PackageReference`s, and its `ProjectReference`s.

## Solution structure

The folder layout on disk and the folder layout in `subtrack-backend.slnx` must always match.
The architecture tests enforce this. See "Adding a new project" below.

| Folder | Purpose |
|---|---|
| `src/ApplicationServices/` | The deployables: the only executable/hostable entry points (currently the Web API). See `src/ApplicationServices/CLAUDE.md`. |
| `src/Common/` | Cross-cutting building blocks that depend on nothing but each other, referenced by everything. See `src/Common/CLAUDE.md`. |
| `src/Infrastructure/` | Database (EF Core/PostgreSQL) and other technical/integration concerns. See `src/Infrastructure/CLAUDE.md`. |
| `src/Models/` | All data-shape types: domain models, DTOs, view models, EF entities. See `src/Models/CLAUDE.md`. |
| `src/Services/` | Business/domain logic and outbound client services. See `src/Services/CLAUDE.md`. |
| `tests/UnitTests/` | Unit tests for services. See `tests/UnitTests/CLAUDE.md`. |
| `tests/ArchitectureTests/` | Tests that enforce this file's structural rules. See `tests/ArchitectureTests/CLAUDE.md`. |
| `tests/IntegrationTests/` | Reserved for integration tests. Currently empty. |
| `docs/` | The normative API contract (`docs/api-contract.md`) and the canonical recurrence examples (`docs/recurrence-examples.json`). Not part of the `.slnx`. |
| `devops/` | Local and deployment assets (`docker-compose.yml` for local PostgreSQL, later Dockerfiles, pipelines). Intentionally **excluded** from the `.slnx` because it holds no buildable projects and shouldn't clutter the IDE solution view. |

### Dependency direction

Dependencies only flow downward through these layers. A layer must never reference something above it:

```
Common                                   (may reference only other Common projects)
  -> Models                              (may reference Common)
    -> Infrastructure, *.Abstractions    (may reference Common, Models)
      -> Services                        (may reference Common, Models, Infrastructure, any *.Abstractions)
        -> ApplicationServices           (composition root: may reference anything below)
```

- `*.Abstractions` means `DomainServices.Abstractions` and `ClientServices.Abstractions`. They are a layer of
  their own because they sit below their implementations.
- Services never reference each other's **concrete** projects, only the `.Abstractions`.
- The architecture tests still define a `Grains` layer. It is reserved and has no projects (ADR 0001); don't
  remove it from the tests.
- Nothing in `src/` may reference `ApplicationServices/`. Nothing in `src/` may reference a test project.
- `tests/UnitTests` projects reference the project they test plus `Common`, `Models` and `*.Abstractions` as
  needed.

`tests/ArchitectureTests/SubTrack.ArchitectureTests/ProjectReferenceTests.cs` encodes this table. If you
change a rule here, change it there in the same change, and vice versa.

## Adding a new project

1. Put the new `.csproj` in its own `src/<Layer>/<ProjectName>/` (or `tests/<Kind>/<ProjectName>/`) folder
   that matches its purpose, following the per-folder `CLAUDE.md` in that layer. The folder name must equal
   the project name.
2. Register the project under the matching `<Folder Name="...">` element in
   `subtrack-backend.slnx`, in the same relative location as on disk. Visual Studio and Rider both
   render the solution from this file, so a project that exists on disk but isn't listed here, or is listed
   under the wrong folder, is a bug. The architecture tests catch this.
3. `devops/` is the one exception: it is deliberately never added to the `.slnx`.
4. Follow the naming rules below, and use file-scoped namespaces from the first line of every new file.
5. Run the build and the tests.

When in doubt about whether a change follows these rules, use the `structure-guardian` agent
(`.claude/agents/structure-guardian.md`) to check it before finishing the task. It covers the rules the
architecture tests can't, such as type naming and where a type belongs.

## C# coding conventions

These are enforced at build time: `.editorconfig` sets IDE0161 and IDE0011 to `error`, and
`EnforceCodeStyleInBuild` in `Directory.Build.props` makes `dotnet build` fail on them. They're stated here
too because they're easy to miss when skimming code:

- **File-scoped namespaces, always.** Use `namespace SubTrack.Foo;` followed by a blank line, never
  `namespace SubTrack.Foo { ... }`.
- **Braces around every `if`, even single-line bodies.** Write:
  ```csharp
  if (condition)
  {
      DoSomething();
  }
  ```
  never `if (condition) DoSomething();`. The same applies to `else`, `for`, `foreach`, `while`.
- **Summaries only on interface members and model public properties.** Every member of an interface
  definition (`I*` types, including service interfaces) must have a `///` summary explaining what
  it does. Public properties on model types (`DomainModels`, DTOs, view models, entities) get the same
  treatment when their purpose isn't obvious from the name. Public methods on concrete implementations do
  **not** get summaries or comments: the interface's summary is the documentation, and the implementation
  should read clearly from its own code. Code should be self-documenting; add a comment to a piece of logic
  only when it's absolutely necessary to explain something the code itself can't (a workaround, a
  non-obvious constraint), never to restate what the code already says.

Files are UTF-8 without a BOM. Line endings are normalized by `.gitattributes`: LF in the repository, and
always LF for `*.sh` and Dockerfiles.

## Naming conventions

Names should describe the role a type plays, not just the noun it acts on. In particular:

- **Services** get a `Service` suffix that names the responsibility, not an agent noun.
  Avoid `ContractReplicator`, `MessagePublisher`. Prefer `ContractReplicationService`,
  `MessagePublishingService`. Interfaces live in the matching `*.Abstractions` project and are prefixed
  `I` (`IContractReplicationService`).
- **Hosted/background services** get a `HostedService` suffix (e.g. `RecurrenceRefreshHostedService`).
- **DTOs** get a `Dto` suffix. **View models** get a `ViewModel` suffix. **EF entities** in `Entities` get an
  `Entity` suffix (`SubscriptionEntity`). Domain models in `DomainModels` are named after the business entity with
  no suffix (`Subscription`, `Category`).

## Service result pattern

Every public method on a service (`DomainServices` and `ClientServices`, and their `I*Service` interfaces in
the `.Abstractions` projects) returns a result type from `SubTrack.Common` instead of a bare value:

- A method that produces a value returns `ServiceResult<T>`, e.g.
  `Task<ServiceResult<Customer>> GetCustomerAsync(Guid id)` on `ICustomerService`.
- A method with no value returns `VoidResultServiceResult`, e.g. `Task<VoidResultServiceResult> DeleteCustomerAsync(Guid id)`.
- Failure is carried on the result: `Error.Code` is a `ServiceErrorCode` (`GenericError`, `ValidationError`,
  `DatabaseError`, `Conflict`, `NotFound`), with an optional `Message` and `Exception`. Check `Success` /
  `Failed` before reading `Value`; `Value` is only meaningful when the result succeeded.
- Success and failure convert implicitly, so a service can `return customer;` or `return ServiceErrorCode.NotFound;`
  directly, or `return new ServiceResultError<ServiceErrorCode>(ServiceErrorCode.NotFound, "message")` when it
  has a message.
- Add a new value to `ServiceErrorCode` (`SubTrack.Common.Enums`) rather than a custom error code type,
  unless a service genuinely needs its own; the two-parameter `ServiceResult<TValue, TErrorCode>` exists for
  that, and an enum is the preferred error code type.
- Callers (other services, controllers) must handle a failed result; they don't assume `Value` is set.

## API contract

This repo owns the API contract for all clients. [`docs/api-contract.md`](docs/api-contract.md) is normative
for everything on the wire (versioning and compatibility, JSON, IDs, money, dates, errors, concurrency,
idempotency, paging, ordering) and for the recurrence, monthly-equivalent and forecast calculations.
[`docs/recurrence-examples.json`](docs/recurrence-examples.json) holds the canonical calculation examples that
the recurrence engine's tests must pass. API changes follow the contract doc: update it (and the fixture) in
the same change, and mark a change the doc calls breaking with `!`.

## Commit messages

Every commit subject follows `<~|!><scope>: <message> [<TICKET>]`, e.g.
`~feat: add subscription endpoints [ST-004]`:

- `~` for a safe commit, `!` for a breaking one;
- scope is one of `feat`, `fix`, `chore`, `refactor`, `test`, with `chore` as the fallback;
- the ticket number in square brackets always comes last. Never invent one; ask if it isn't known.

Use the `git-commit` skill (`.claude/skills/git-commit/SKILL.md`) to commit. It covers how to choose each part
and what counts as breaking in this solution.

Commit directly on `main`: no feature branches, no PRs. Only `main` is pushed, and only when asked.

## Keeping docs in sync

If a change adds/removes/renames a project, introduces a new top-level folder, or changes one of the rules
above, use the `sync-docs` skill (`.claude/skills/sync-docs/SKILL.md`) to update the affected `CLAUDE.md`
files, `README.md`, and the architecture tests so they don't drift from the actual structure.
