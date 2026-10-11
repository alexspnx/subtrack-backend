# src/Infrastructure/

See the root `CLAUDE.md` first for the overall layering and naming rules; this file only covers what's
specific to this folder.

## Purpose

Technical/integration concerns: database access, caching, messaging. That means anything that talks to an
external system or technology, and anything that maps between technical shapes. One project per concern.

Current projects:

- `SubTrack.EntityFramework`: database access (EF Core with PostgreSQL via Npgsql). `SubTrackDbContext`,
  its entity configurations and `AddSubTrackDbContext(IServiceCollection, IConfiguration)` (reads
  `ConnectionStrings:SubTrack`) live here; the entity classes themselves live in `src/Models/SubTrack.Entities`.
- `SubTrack.Mapper`: object-to-object mapping between `DomainModels`, `DTOs`, `ViewModels` and `Entities`.

Future concerns (caching, messaging, etc.) each get their own `SubTrack.<Concern>` project here.

## Rules

- No business/domain logic here, only plumbing, clients, and adapters. If a type is making a business
  decision rather than moving/transforming data, it belongs in `src/Services` instead.
- An `Infrastructure` project may reference `Common` and `Models` only. It must not reference `Services`,
  `*.Abstractions`, another `Infrastructure` project, or `ApplicationServices`.
- A new infrastructure concern gets its own subfolder and `.csproj` here, named `SubTrack.<Concern>`,
  registered under `/src/Infrastructure/` in the `.slnx`.
