# CLAUDE.md

This file guides Claude Code (and any other contributor, human or AI) when working in this repository.

## What this is

`subtrack-backend` is a reference/template .NET backend solution. It exists to show a preferred
project layout for a backend built with ASP.NET Core, Microsoft Orleans, and messaging/data infrastructure
(Kafka, RabbitMQ, Redis, EF Core, etc.), so that new projects can be bootstrapped from the same structure
instead of reinventing it each time. Treat the structure and rules below as load-bearing: they are the point
of this repository.

## Commands

```bash
dotnet build subtrack-backend.slnx                    # also enforces the code-style rules below
dotnet test --solution subtrack-backend.slnx          # includes the architecture tests
dotnet format subtrack-backend.slnx --verify-no-changes
```

Before finishing any task that touches `src/`, `tests/`, a `.csproj`, or the `.slnx`, run the build and the
tests and make sure both pass. The architecture tests (`tests/ArchitectureTests`) fail when a project
reference breaks the layering, or when a project is missing from the `.slnx` or listed under the wrong folder.

To run the system locally, start `SubTrack.Silo` first. The other hosts connect to it as Orleans clients.

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
| `src/ApplicationServices/` | The deployables: the only executable/hostable entry points (the Web API, the message Consumer and Publisher hosts, the Orleans Silo host). See `src/ApplicationServices/CLAUDE.md`. |
| `src/Common/` | Cross-cutting building blocks that depend on nothing but each other, referenced by everything. See `src/Common/CLAUDE.md`. |
| `src/Grains/` | Microsoft Orleans grains and their interfaces. See `src/Grains/CLAUDE.md`. |
| `src/Infrastructure/` | Database, Kafka, Redis, RabbitMQ, and other technical/integration concerns. See `src/Infrastructure/CLAUDE.md`. |
| `src/Models/` | All data-shape types: domain models, DTOs, view models, EF entities. See `src/Models/CLAUDE.md`. |
| `src/Services/` | Business/domain logic and outbound client services. See `src/Services/CLAUDE.md`. |
| `tests/UnitTests/` | Unit tests for services and grains. See `tests/UnitTests/CLAUDE.md`. |
| `tests/ArchitectureTests/` | Tests that enforce this file's structural rules. See `tests/ArchitectureTests/CLAUDE.md`. |
| `tests/IntegrationTests/` | Reserved for integration tests. Currently empty. |
| `devops/` | Deployment/CI assets (Dockerfiles, manifests, pipelines). Intentionally **excluded** from the `.slnx` because it holds no buildable projects and shouldn't clutter the IDE solution view. |

### Dependency direction

Dependencies only flow downward through these layers. A layer must never reference something above it:

```
Common                                   (may reference only other Common projects)
  -> Models                              (may reference Common)
    -> Infrastructure, *.Abstractions    (may reference Common, Models)
      -> Grains, Services                (may reference Common, Models, Infrastructure, any *.Abstractions)
        -> ApplicationServices           (composition root: may reference anything below)
```

- `*.Abstractions` means `Grains.Abstractions`, `DomainServices.Abstractions` and `ClientServices.Abstractions`.
  They are a layer of their own because they sit below their implementations.
- Grains and Services never reference each other's **concrete** projects, only the `.Abstractions`. So a
  grain calls a service through `IXxxService`, and a domain service calls a grain through `IGrainFactory` and
  the grain interface in `Grains.Abstractions`.
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
  definition (`I*` types, including grain and service interfaces) must have a `///` summary explaining what
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
- **Grains** always get a `Grain` suffix: `MatchGrain`, `PlayerGrain`. Grain interfaces (in
  `Grains.Abstractions`) are prefixed `I` and also keep the `Grain` suffix: `IMatchGrain`.
- **Hosted/background services** get a `HostedService` suffix (e.g. `GrainInitializationHostedService`).
- **Message handlers** (Kafka/RabbitMQ) are the one deliberate exception to the "no agent nouns" rule. They
  get a `Handler` suffix and are named after the message they handle: `MatchCreatedHandler` handles
  `MatchCreatedDto`. They live in the `Consumer` host (see `src/ApplicationServices/CLAUDE.md`).
- **DTOs** get a `Dto` suffix. **View models** get a `ViewModel` suffix. **EF entities** in `Entities` get an
  `Entity` suffix (`MatchEntity`). Domain models in `DomainModels` are named after the business entity with
  no suffix (`Match`, `Player`).

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
- Callers (grains, other services, hosts) must handle a failed result; they don't assume `Value` is set.

## Commit messages

Every commit subject follows `<~|!><scope>: <message> [<TICKET>]`, e.g.
`~feat: send current statistics to connected clients [IRL-142]`:

- `~` for a safe commit, `!` for a breaking one;
- scope is one of `feat`, `fix`, `chore`, `refactor`, `test`, with `chore` as the fallback;
- the ticket number in square brackets always comes last. Never invent one; ask if it isn't known.

Use the `git-commit` skill (`.claude/skills/git-commit/SKILL.md`) to commit. It covers how to choose each part
and what counts as breaking in this solution.

## Keeping docs in sync

If a change adds/removes/renames a project, introduces a new top-level folder, or changes one of the rules
above, use the `sync-docs` skill (`.claude/skills/sync-docs/SKILL.md`) to update the affected `CLAUDE.md`
files, `README.md`, and the architecture tests so they don't drift from the actual structure.
