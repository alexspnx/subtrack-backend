# src/ApplicationServices/

See the root `CLAUDE.md` first for the overall layering and naming rules; this file only covers what's
specific to this folder.

## Purpose

This is the only folder that contains deployables: projects that produce a runnable host (currently the Web
API). Every other `src/` folder is a library referenced by something in here.

Current projects:

- `SubTrack.WebAPI`: the public-facing HTTP API (ASP.NET Core Web SDK) and the contract owner for all clients.
  It registers `SubTrackDbContext` (PostgreSQL, `ConnectionStrings:SubTrack`) and maps `/health`, which checks
  the database.

## Rules

- One subfolder per deployable, one `.csproj` per subfolder, named `SubTrack.<Name>`.
- As the composition root, a project here may reference any other `src/` project it needs
  (`Common`, `Models`, `Infrastructure`, `Services`/`Services.Abstractions`). Concrete `Services` projects are
  referenced here only so their implementations can be registered in DI. Code in this folder should still
  depend on the interfaces.
- Nothing outside this folder may reference a project inside it; these are leaves of the dependency graph.
- Wiring/composition code (DI registration, `Program.cs`, hosted services that bootstrap the process) belongs
  here. Business logic does not: it belongs in `src/Services` and gets called from here.
- Entry points that receive external input stay thin and delegate to `Services`. That means controllers in
  `WebAPI`.
- A new deployable (e.g. a new worker) gets its own subfolder here and its own entry under
  `/src/ApplicationServices/` in `subtrack-backend.slnx`.
- Connection strings and other environment settings come from configuration. Development values live in
  `appsettings.Development.json` and must match `devops/docker-compose.yml`.
