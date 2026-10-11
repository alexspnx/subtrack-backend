# subtrack-backend

A reference/template .NET backend solution. It's not a real product; it's a starting point for spinning up
a new backend with a consistent, opinionated project layout: ASP.NET Core APIs, Microsoft Orleans grains,
messaging (Kafka or RabbitMQ), and the usual infrastructure/service/model layering. Copy this structure when
starting a new project rather than designing a new one from scratch each time.

For the full set of structural, naming, and coding rules (and why they exist), see [`CLAUDE.md`](CLAUDE.md)
and the `CLAUDE.md` file inside each `src/*` and `tests/*` folder. This README covers the "what"; the rules
files cover the "why" and "how to extend it".

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (pinned via `global.json`)
- Visual Studio 2026 or JetBrains Rider 2025.3+ if you want to open the solution in an IDE. Both support
  `net10.0`, the `.slnx` solution format and Microsoft.Testing.Platform. Visual Studio 2022 cannot target
  .NET 10.

## Getting started

```bash
dotnet build subtrack-backend.slnx                    # also enforces the code-style rules below
dotnet test --solution subtrack-backend.slnx          # includes the architecture tests
dotnet format subtrack-backend.slnx --verify-no-changes
```

Or open `subtrack-backend.slnx` directly in Visual Studio or Rider.

Each deployable in `src/ApplicationServices/` can also be run individually. Start the Orleans silo first,
because the other hosts connect to it as Orleans clients:

```bash
dotnet run --project src/ApplicationServices/SubTrack.Silo
dotnet run --project src/ApplicationServices/SubTrack.WebAPI
```

## Solution structure

```
src/
  ApplicationServices/   Deployables: Silo (Orleans host), WebAPI, Consumer (Kafka/RabbitMQ worker),
                         Publisher (Kafka/RabbitMQ producer or SignalR, never both)
  Common/                Shared building blocks (service results, configuration, enums, Orleans packages)
  Grains/                Orleans grains and their abstractions
  Infrastructure/        Database (EF Core), messaging, caching, mapping, etc.
  Models/                DomainModels, DTOs, ViewModels, Entities (EF)
  Services/              DomainServices and ClientServices (+ their Abstractions projects)
tests/
  UnitTests/             Unit tests for Services and Grains
  ArchitectureTests/     Enforce the layering and .slnx rules on every `dotnet test`
  IntegrationTests/      Reserved for integration tests (currently empty)
devops/                  Deployment/CI assets; intentionally not part of the .slnx
```

Dependencies flow one direction only:

```
Common -> Models -> Infrastructure/*.Abstractions -> Grains/Services -> ApplicationServices
```

`ApplicationServices` is the composition root (the deployables); nothing depends on it. `Common` sits at the
base and depends on nothing else in the solution except other `Common` projects. Grains and Services only know each other through their
`.Abstractions` projects. See [`CLAUDE.md`](CLAUDE.md) for the full rules on what each layer may and may not
reference.

Shared build settings live in `Directory.Build.props`, and NuGet versions are managed centrally in
`Directory.Packages.props`. `global.json` pins the .NET 10 SDK band and opts `dotnet test` into
Microsoft.Testing.Platform. A `.csproj` in this solution therefore only ever contains its `Sdk`,
`PackageReference`s (no `Version` attribute), and `ProjectReference`s.

### Current projects, by folder

This is what's already in the template as a worked example; a real project built from this template will add
to (or, for `Consumer`/`Publisher`, choose Kafka **or** RabbitMQ for) these:

| Folder | Projects |
|---|---|
| `src/ApplicationServices/` | `SubTrack.Silo`, `SubTrack.WebAPI`, `SubTrack.Consumer`, `SubTrack.Publisher` |
| `src/Common/` | `SubTrack.Common`, `SubTrack.Common.Configuration`, `SubTrack.Common.Enums`, `SubTrack.Common.Orleans` |
| `src/Grains/` | `SubTrack.Grains`, `SubTrack.Grains.Abstractions` |
| `src/Infrastructure/` | `SubTrack.EntityFramework` (EF Core), `SubTrack.Mapper` (object-to-object mapping) |
| `src/Models/` | `SubTrack.DomainModels`, `SubTrack.DTOs`, `SubTrack.ViewModels`, `SubTrack.Entities` |
| `src/Services/` | `SubTrack.DomainServices` (+ `.Abstractions`), `SubTrack.ClientServices` (+ `.Abstractions`) |
| `tests/UnitTests/` | `SubTrack.DomainServices.UnitTests`, `SubTrack.ClientServices.UnitTests`, `SubTrack.Grains.UnitTests` |
| `tests/ArchitectureTests/` | `SubTrack.ArchitectureTests` |

`SubTrack.Common.Orleans` is the one place in the solution that references Orleans packages directly;
every other project that needs Orleans (grain base types, client/server hosting, or the serializer code
generator) takes a `ProjectReference` on it instead. Kafka, RabbitMQ, Redis and similar concerns aren't
wired up yet — each gets its own `SubTrack.<Concern>` project under `src/Infrastructure/` when a project
built from this template needs it.

## Naming conventions

Names should describe the role a type plays, not just the noun it acts on:

- **Services** get a `Service` suffix that names the responsibility, not an agent noun — prefer
  `ContractReplicationService` over `ContractReplicator`. Interfaces live in the matching `*.Abstractions`
  project, prefixed `I` (`IContractReplicationService`).
- **Grains** always get a `Grain` suffix (`MatchGrain`, `PlayerGrain`); grain interfaces (in
  `Grains.Abstractions`) are prefixed `I` and keep the `Grain` suffix (`IMatchGrain`).
- **Hosted/background services** get a `HostedService` suffix (e.g. `GrainInitializationHostedService`).
- **Message handlers** (Kafka/RabbitMQ) are the one deliberate exception to the "no agent nouns" rule: they
  get a `Handler` suffix and are named after the message they handle (`MatchCreatedHandler` handles
  `MatchCreatedDto`). They live in the `Consumer` host.
- **DTOs** get a `Dto` suffix, **view models** get a `ViewModel` suffix, and **EF entities** get an `Entity`
  suffix (`MatchEntity`). **Domain models** are named after the business entity with no suffix (`Match`,
  `Player`).

## Service result pattern

Every public service method returns a result from `SubTrack.Common` instead of a bare value:
`ServiceResult<T>` when it produces a value (`Task<ServiceResult<Customer>> GetCustomerAsync(...)`) and
`VoidResultServiceResult` when it doesn't. Failures are carried on the result as a `ServiceErrorCode` with an
optional message and exception, so callers check `Success`/`Failed` instead of catching exceptions. See
[`CLAUDE.md`](CLAUDE.md) for the details.

## Coding conventions

Enforced at build time (`dotnet build` fails on a violation, via `.editorconfig` + `EnforceCodeStyleInBuild`
in `Directory.Build.props`):

- File-scoped namespaces everywhere: `namespace Foo.Bar;`, never `namespace Foo.Bar { ... }`.
- Braces around every `if`/`else`/`for`/`foreach`/`while` body, even single-line ones.

Files are UTF-8 without a BOM; line endings are LF in the repository (enforced by `.gitattributes`).

## Adding a new project

1. Create the `.csproj` in its own `src/<Layer>/<ProjectName>/` (or `tests/<Kind>/<ProjectName>/`) folder
   matching its purpose — see the `CLAUDE.md` in that layer for what belongs there. The folder name must
   equal the project name.
2. Register it under the matching `<Folder Name="...">` element in `subtrack-backend.slnx`, in
   the same relative location as on disk (`devops/` is the one folder deliberately excluded). The
   architecture tests fail if the disk layout and the `.slnx` don't match.
3. Follow the naming conventions above, use file-scoped namespaces, and add only `Sdk`, `PackageReference`s
   (no `Version` attribute — declare it in `Directory.Packages.props`), and `ProjectReference`s to the
   `.csproj`.
4. Run the build and the tests.

The `structure-guardian` agent (`.claude/agents/structure-guardian.md`) checks a change against these rules,
including the ones the architecture tests can't (type naming, where a type belongs).

## Commit messages

Every commit subject follows `<~|!><scope>: <message> [<TICKET>]`, e.g.
`~feat: send current statistics to connected clients [IRL-142]`:

- `~` for a safe commit, `!` for a breaking one;
- scope is one of `feat`, `fix`, `chore`, `refactor`, `test`, falling back to `chore`;
- the ticket number in square brackets always comes last, and is never invented.

## Working with Claude Code

This repo is set up for [Claude Code](https://claude.com/claude-code):

- `CLAUDE.md` (root and per-folder) describes the structure, dependency rules, and naming/style conventions.
- `.claude/settings.json` pre-approves the build/test/format commands and read-only git commands.
- `.claude/agents/structure-guardian.md`: an agent that audits new/changed files against those rules,
  including the ones the architecture tests can't check (type naming, type placement).
- `.claude/skills/git-commit/SKILL.md`: a skill that commits using the team's message format
  (`~feat: message [TICKET-123]`, with `!` marking breaking changes).
- `.claude/skills/sync-docs/SKILL.md`: a skill that keeps the `CLAUDE.md` files, this README and the
  architecture tests in sync after a structural change.
