# src/Grains/

See the root `CLAUDE.md` first for the overall layering and naming rules; this file only covers what's
specific to this folder.

## Purpose

Anything Microsoft Orleans grain-related goes here.

Current projects:

- `SubTrack.Grains.Abstractions`: grain interfaces and the Orleans-serializable messages they exchange. This
  is what a caller references to call a grain, never the concrete `Grains` project. Callers include hosts
  such as `WebAPI` (via `IClusterClient`) and domain services (via `IGrainFactory`).
- `SubTrack.Grains`: the grain implementations, hosted by `SubTrack.Silo`. Grain state types are an
  implementation detail and live here too.

Both projects reference `SubTrack.Common.Orleans` (see `src/Common/CLAUDE.md`) rather than Orleans packages
directly, which brings in the grain base types and the serializer code generator.

## Rules

- **Every grain class must end with the `Grain` suffix** (`MatchGrain`, `PlayerGrain`), no exceptions.
- **Every grain interface must end with the `Grain` suffix too**, prefixed with `I` (`IMatchGrain`), and
  lives in `Grains.Abstractions`, not in `Grains`.
- `Grains.Abstractions` may reference `Common` and `Models` only.
- `Grains` may reference `Common`, `Models`, `Infrastructure`, `Grains.Abstractions`, and the `Services`
  **`.Abstractions`** projects. It must never reference a concrete `Services` project; implementations are
  registered by the Silo and injected. Business logic that isn't Orleans-specific should live in `Services`
  and be called from the grain, not written directly inside the grain class.
- Anything that needs to call a grain depends on `Grains.Abstractions`, never on `Grains` directly. That
  includes other `ApplicationServices` hosts (through `IClusterClient`) and domain services (through
  `IGrainFactory`).
- Types from `Models` that cross a grain call are marked `[GenerateSerializer]` in `Models` itself (see
  `src/Models/CLAUDE.md`).
