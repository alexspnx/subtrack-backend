# src/Models/

See the root `CLAUDE.md` first for the overall layering and naming rules; this file only covers what's
specific to this folder.

## Purpose

All data-shape types live here, split by role:

- `SubTrack.DomainModels`: core business entities, named after the business concept with no suffix
  (`Subscription`, `Category`).
- `SubTrack.DTOs`: data transfer objects used at messaging/external boundaries (external API payloads,
  messages). Suffixed `Dto` (`ExchangeRateDto`).
- `SubTrack.ViewModels`: shapes used at the API boundary (request/response bodies). Suffixed `ViewModel`
  (`SubscriptionViewModel`).
- `SubTrack.Entities`: EF Core persistence entities, mapped to tables by `SubTrack.EntityFramework`.
  Suffixed `Entity` (`SubscriptionEntity`). These are **not** the domain models.

## Rules

- Models projects are pure data holders: properties, maybe simple computed properties or validation
  attributes. No business logic, no I/O.
- A `Models` project may reference `Common` only. It must not reference `Infrastructure`, `Services`, or
  `ApplicationServices`, because every layer above depends on `Models`, not the other way round.
- No serialization framework dependencies: types are plain C# classes (System.Text.Json at the API boundary).
- Don't reuse a `DomainModel` as a `Dto`, `ViewModel` or `Entity` (or vice versa) just to avoid writing a
  mapping. Keep the kinds distinct and map between them (see `SubTrack.Mapper`) so each boundary can evolve
  independently.
