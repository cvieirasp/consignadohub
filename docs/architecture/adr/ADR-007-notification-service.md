# ADR-007: NotificationService as Fan-out Side-Effects Consumer

**Status:** Accepted
**Date:** 2026-02-26

## Context

Several events in the proposal workflow must trigger external side effects such as sending confirmation emails, result notifications, and disbursement receipts. These side effects must not block the workflow pipeline or risk rolling back domain state if they fail. The question is how to deliver notifications without coupling the workflow services to notification logic.

## Decision

A dedicated **NotificationService** (Worker) consumes all four integration events as a pure fan-out subscriber. It never mutates domain state.

### Architecture

- NotificationService is a separate process (`NotificationService.Worker`) with its own database (`ConsignadoHub_Notifications`) used exclusively for the `InboxMessages` idempotency table.
- It subscribes to every event on the `consignadohub.events` exchange via four dedicated queues, one per event type:

  | Event | Queue |
  |---|---|
  | `proposal.submitted` | `notification.proposal.submitted` |
  | `proposal.credit.completed` | `notification.credit.completed` |
  | `contract.generated` | `notification.contract.generated` |
  | `disbursement.completed` | `notification.disbursement.completed` |

- Each consumer follows the standard inbox idempotency pattern (`EventId`, `ConsumerName`).
- The actual notification logic is encapsulated in a single `NotificationHandler` class with one method per event type. In the current implementation these are **structured-log stubs** (`[NOTIFICATION] ...`) that can be replaced with real email/webhook providers without changing consumer code.

### Why a separate service (not embedded in WorkflowWorker or ProposalService)?

| Option | Problem |
|---|---|
| Embed in ProposalService | Mixes API concerns with fan-out side effects; failures affect proposal state mutations |
| Embed in WorkflowWorker | Couples workflow orchestration to notification delivery; a slow email provider blocks workflow progress |
| Separate NotificationService | Clean separation; failures are isolated; can scale and deploy independently |

### Separation of concerns

- NotificationService **reads** events but **never writes** back to the domain.
- It has no REST API surface — it is a consumer-only worker.
- It can fail or restart without affecting the proposal state machine in ProposalService or WorkflowWorker.

## Consequences

- Notification failures (e.g. email provider unavailable) do not roll back or delay domain state transitions.
- NotificationService can be scaled, redeployed, or replaced independently.
- Adding a new notification channel (SMS, webhook) requires changes only within NotificationService.
- Each consumer is idempotent: redelivered events produce duplicate log entries (no-op), not duplicate emails when real providers are wired in (provider-level idempotency required at that stage).
- The Inbox table in `ConsignadoHub_Notifications` is the sole persistence requirement for this service.
