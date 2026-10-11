# src/Common/

See the root `CLAUDE.md` first for the overall layering and naming rules; this file only covers what's
specific to this folder.

## Purpose

Cross-cutting building blocks with no domain or infrastructure knowledge — things any other project in the
solution can safely take a dependency on.

Current projects:

- `SubTrack.Common.Configuration` — shared configuration primitives/options types.
- `SubTrack.Common.Enums` — shared enums used across layers.
- `SubTrack.Common` — the solution-wide service result pattern: `ServiceResult<TValue>` (and the
  `ServiceResult<TValue, TErrorCode>` base it builds on) for service methods that return a value,
  `VoidResultServiceResult` (and `VoidResultServiceResult<TErrorCode>`) for service methods that don't, plus
  `ServiceResultError<TErrorCode>`. The default error code type is `ServiceErrorCode` from
  `SubTrack.Common.Enums`. All of these are `[GenerateSerializer]` so they can cross grain calls. See
  "Service result pattern" in the root `CLAUDE.md` for how services must use them.
- `SubTrack.Common.Orleans` — the one place in the solution that references Orleans packages
  (`Microsoft.Orleans.Client`, `Microsoft.Orleans.CodeGenerator`, `Microsoft.Orleans.Sdk`,
  `Microsoft.Orleans.Serialization`, `Microsoft.Orleans.Server`) and declares the `using Orleans;` global
  using. Any project anywhere in the solution that needs Orleans — grain base types, client/server hosting,
  or serializer code generation — takes a `ProjectReference` on this project instead of referencing an
  Orleans package directly. The `Microsoft.Orleans.CodeGenerator` reference intentionally has no
  `PrivateAssets="all"`, so the source generator it brings in flows transitively to whichever project
  references `SubTrack.Common.Orleans`; that's what lets `DomainModels`/`DTOs` get serializers generated
  for their `[GenerateSerializer]` types without referencing the generator package themselves.

## Rules

- **A `Common` project may reference other `Common` projects and nothing else in this solution** — not
  `Models`, not `Infrastructure`, not `Services`. Beyond that, only third-party/framework packages are
  allowed as dependencies. This is what keeps `Common` a base layer that everything else can sit on top of
  without creating cycles. (For example, `SubTrack.Common` references `Common.Enums` and `Common.Orleans`.)
- Keep `Common` projects small and generic. If a type needs a domain concept (e.g. it references a
  `DomainModel` or talks to a database), it does not belong here — it belongs in `Models`, `Infrastructure`,
  or `Services` instead.
- A new cross-cutting concern (e.g. shared constants, shared extension methods) gets its own
  `SubTrack.Common.<Name>` project here, registered under `/src/Common/` in the `.slnx`.
