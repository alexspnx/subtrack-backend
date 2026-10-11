# src/Services/

See the root `CLAUDE.md` first for the overall layering and naming rules; this file only covers what's
specific to this folder.

## Purpose

Business/domain logic, split by direction:

- `SubTrack.DomainServices` (+ `.Abstractions`): core business rules and orchestration internal to this
  system.
- `SubTrack.ClientServices` (+ `.Abstractions`): outbound calls to other systems/APIs.

## Rules

- **Name services for what they do, with a `Service` suffix, never a bare agent noun.** Avoid
  `ContractReplicator`, `MessagePublisher`. Prefer `ContractReplicationService`, `MessagePublishingService`.
- **Every public service method returns a service result** (`SubTrack.Common`), never a bare value, and
  reports failure as an error on the result (`ServiceErrorCode`, optional message and exception). A method that returns a value returns `ServiceResult<T>`
  (`Task<ServiceResult<Customer>> GetCustomerAsync(...)`); a method with no value returns
  `VoidResultServiceResult` (`Task<VoidResultServiceResult>`). This applies to `DomainServices` and
  `ClientServices` alike, and to the interfaces in their `.Abstractions` projects. See "Service result
  pattern" in the root `CLAUDE.md`.
- Every concrete service has a matching interface in the sibling `.Abstractions` project, prefixed `I`
  (`IContractReplicationService` in `DomainServices.Abstractions` for `ContractReplicationService` in
  `DomainServices`). Callers should depend on the `.Abstractions` project and resolve the implementation via
  DI, not reference the concrete `Services` project directly. Only the `ApplicationServices` hosts reference
  concrete services, to register them.
- A `Services` project may reference `Common`, `Models`, `Infrastructure`, and `.Abstractions` projects. It
  must not reference a concrete `Services` project, or anything in `ApplicationServices`.
- `DomainServices` may use `ClientServices.Abstractions` to reach external systems. `ClientServices` should not
  depend on `DomainServices.Abstractions`: outbound clients don't call back into the domain.
- A `Services.Abstractions` project may reference `Common` and `Models` only.
- Unit tests for a service live in `tests/UnitTests/<ProjectName>.UnitTests`. See `tests/UnitTests/CLAUDE.md`.
