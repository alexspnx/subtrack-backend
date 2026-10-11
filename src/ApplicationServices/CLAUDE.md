# src/ApplicationServices/

See the root `CLAUDE.md` first for the overall layering and naming rules; this file only covers what's
specific to this folder.

## Purpose

This is the only folder that contains deployables: projects that produce a runnable host (a Web API, a
worker, the Orleans Silo). Every other `src/` folder is a library referenced by something in here.

Current projects:

- `SubTrack.Silo`: the Orleans Silo host (`Microsoft.NET.Sdk.Worker`, server hosting via
  `SubTrack.Common.Orleans`). Hosts the grains from `src/Grains` and registers the services they use.
- `SubTrack.WebAPI`: the public-facing HTTP API (ASP.NET Core Web SDK). An Orleans client, so it calls grains
  through `IClusterClient`/`IGrainFactory`.
- `SubTrack.Consumer`: a message-consuming worker (`Microsoft.NET.Sdk.Worker`) for Kafka **or** RabbitMQ,
  whichever the project built from this template uses. An Orleans client.
- `SubTrack.Publisher`: receives messages from the silo (e.g. via grain observers or Orleans streams) and
  forwards them to **exactly one** outlet: either a Kafka/RabbitMQ producer, or front-end clients over
  SignalR. Never both. An Orleans client. It uses the Web SDK so SignalR can be hosted; a broker-only
  publisher may switch to `Microsoft.NET.Sdk.Worker`.

## Rules

- One subfolder per deployable, one `.csproj` per subfolder, named `SubTrack.<Name>`.
- As the composition root, a project here may reference any other `src/` project it needs
  (`Common`, `Models`, `Infrastructure`, `Grains`/`Grains.Abstractions`, `Services`/`Services.Abstractions`).
  Concrete `Services` projects are referenced here only so their implementations can be registered in DI.
  Code in this folder should still depend on the interfaces.
- Every host here is an Orleans client or server, so every host references `SubTrack.Common.Orleans` (see
  `src/Common/CLAUDE.md`) rather than an Orleans package directly.
- Only `SubTrack.Silo` references the concrete `SubTrack.Grains` project. Every other host talks to grains
  through `Grains.Abstractions` and an Orleans client (`UseOrleansClient`).
- Nothing outside this folder may reference a project inside it; these are leaves of the dependency graph.
- Wiring/composition code (DI registration, `Program.cs`, hosted services that bootstrap the process, e.g.
  `GrainInitializationHostedService`) belongs here. Business logic does not: it belongs in `src/Services` or
  `src/Grains` and gets called from here.
- Entry points that receive external input stay thin and delegate to `Services`/grains. That means
  controllers in `WebAPI` and message handlers in `Consumer`. Handlers are named `<Message>Handler`
  (`MatchCreatedHandler` for `MatchCreatedDto`) and live in a `Handlers/` folder of the `Consumer` project.
  The broker polling loop that dispatches to them is a `...HostedService`.
- If a Publisher ever needs both a broker and SignalR, split it into two deployables (a broker publisher and
  a client publisher) instead of combining them.
- A new deployable (e.g. a new worker or a gRPC host) gets its own subfolder here and its own entry under
  `/src/ApplicationServices/` in `subtrack-backend.slnx`.
- Orleans clustering is `UseLocalhostClustering()` for local development. Replace it with the real
  clustering provider per environment.
