# ADR-009: Publisher-Owned Contracts Projects

**Status:** Accepted
**Date:** 05/03/2026

## Context

The original design placed all integration event types in a single shared building block:
`src/building-blocks/ConsignadoHub.Contracts/`. While convenient for a first pass, this introduced a
structural coupling problem:

- A change to any single event type (e.g. adding a field to `DisbursementCompletedEvent`) required rebuilding and redeploying every service that referenced the shared library, even services that do not consume that event.
- The shared library also violated the principle that the **publisher owns its contract**: the team responsible for WorkflowWorker should control the schema of the events WorkflowWorker emits, without having to coordinate with an unrelated shared library.
- In a real polyrepo setup (one Git repo per service), a shared building block would need to be published as a NuGet package with its own versioning and release cycle — a significant operational burden.

## Decision

Replace `ConsignadoHub.Contracts` with thin, **publisher-owned** Contracts projects, one per publishing service:

| Project | Location | Events owned |
|---|---|---|
| `ProposalService.Contracts` | `src/services/ProposalService/src/ProposalService.Contracts/` | `ProposalSubmittedEvent` |
| `WorkflowWorker.Contracts` | `src/services/WorkflowWorker/src/WorkflowWorker.Contracts/` | `CreditAnalysisCompletedEvent`, `ContractGeneratedEvent`, `DisbursementCompletedEvent` |

### Rules

1. **Publisher owns the event schema.** Only the team that publishes an event may change its Contracts project.
2. **Consumers reference the publisher's Contracts project** as a project reference (monorepo) or NuGet package (polyrepo).
3. **No circular dependencies.** Both Contracts projects reference only `ConsignadoHub.BuildingBlocks` (for `IntegrationEvent`). They do not reference each other or any service project.
4. **Namespace matches project name.** Events live under `ProposalService.Contracts.Events` and `WorkflowWorker.Contracts.Events` respectively, making the owning service unambiguous from any using statement.

### Dependency graph after this change

```
ConsignadoHub.BuildingBlocks
        ▲               ▲
        │               │
ProposalService     WorkflowWorker
  .Contracts          .Contracts
    ▲   ▲               ▲   ▲
    │   └───────────────┘   │
    │   WorkflowWorker.*    │
    │                       │
ProposalService.*      NotificationService.*
```

No service-level project depends on another service's application or infrastructure layer — only on its published Contracts.

## Consequences

- **Decoupled deployments**: a change to a WorkflowWorker event only requires rebuilding services that consume WorkflowWorker events, not the entire solution.
- **Clear ownership**: the publisher namespace (`WorkflowWorker.Contracts.Events`) communicates authorship unambiguously from any `using` statement.
- **Forward compatibility**: consumers can pin a specific version of a Contracts project without being dragged into upgrades of unrelated events.
- **Slight initial friction**: a new publisher requires creating a new `*.Contracts` project. The cost is low and the layout is consistent.
- **CustomerService has no Contracts project** because it currently publishes no integration events. One should be created if CustomerService ever needs to emit events consumed by other services.
