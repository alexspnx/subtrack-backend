# src/Models/

See the root `CLAUDE.md` first for the overall layering and naming rules; this file only covers what's
specific to this folder.

## Purpose

All data-shape types live here, split by role:

- `SubTrack.DomainModels`: core business entities, named after the business concept with no suffix
  (`Match`, `Player`).
- `SubTrack.DTOs`: data transfer objects used at messaging/external boundaries (Kafka/RabbitMQ messages,
  external API payloads). Suffixed `Dto` (`MatchCreatedDto`).
- `SubTrack.ViewModels`: shapes used at the API boundary (request/response bodies). Suffixed `ViewModel`
  (`MatchViewModel`).
- `SubTrack.Entities`: EF Core persistence entities, mapped to tables by `SubTrack.EntityFramework`.
  Suffixed `Entity` (`MatchEntity`). These are **not** the domain models.

## Rules

- Models projects are pure data holders: properties, maybe simple computed properties or validation
  attributes. No business logic, no I/O.
- A `Models` project may reference `Common` only. It must not reference `Infrastructure`, `Services`,
  `Grains`, or `ApplicationServices`, because every layer above depends on `Models`, not the other way round.
  `SubTrack.Common.Orleans` (see `src/Common/CLAUDE.md`) counts as `Common` for this rule.
- The one allowed framework dependency is Orleans serialization, taken via a `ProjectReference` to
  `SubTrack.Common.Orleans` rather than the Orleans packages directly. `DomainModels` and `DTOs` reference
  it so their types can be marked `[GenerateSerializer]`/`[Id(n)]` and passed through grain calls (and get
  `using Orleans;` as a global using from it). Orleans only generates serializers inside the assembly that
  declares the type; `Common.Orleans` references `Microsoft.Orleans.CodeGenerator` without
  `PrivateAssets="all"` specifically so that generator still runs against `DomainModels`/`DTOs` themselves via
  the transitive `ProjectReference`. Add the same `ProjectReference` to another Models project only if its
  types need to cross a grain call. `ViewModels` and `Entities` shouldn't need to.
- Don't reuse a `DomainModel` as a `Dto`, `ViewModel` or `Entity` (or vice versa) just to avoid writing a
  mapping. Keep the kinds distinct and map between them (see `SubTrack.Mapper`) so each boundary can evolve
  independently.
