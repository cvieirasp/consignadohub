# ADR-002: Outbox Pattern + Idempotent Consumers

**Status:** Accepted
**Date:** 2026-02-20
**Updated:** 2026-02-26 (Milestone 3 — implementation details and service coverage documented)

## Context

When a proposal is submitted, the domain state change (write to DB) and the event publication (to RabbitMQ) must happen atomically. Without this guarantee, a crash between the two operations causes either a lost event or a duplicate state change. Conversely, RabbitMQ's at-least-once delivery means consumers can receive the same event more than once and must handle it safely.

## Decision

### Outbox (ProposalService only)

ProposalService is the sole publisher that originates events from user-facing API requests. The Outbox pattern is applied exclusively here:

1. Domain state + `OutboxMessage` written in a **single `SaveChangesAsync` call** (same `DbContext` transaction).
2. `OutboxDispatcherHostedService` polls every 5 seconds for unprocessed messages and publishes to RabbitMQ.
3. Successfully published messages are marked with `ProcessedAt`; failed attempts record the error.

WorkflowWorker publishes events synchronously within the consumer handler (after inbox recording) because the events it emits are a direct side-effect of a consumed message — delivery guarantees come from the consumer ACK lifecycle, not a second Outbox.

### Inbox / Idempotent Consumers (all services that consume events)

Every consumer in every service follows the same pattern:

1. **Idempotency check** — query `InboxMessages` for `(EventId, ConsumerName)`.
2. If already processed → ACK and return early (no side effects).
3. Execute business logic.
4. Write the `InboxMessage` record and any domain changes in the **same `SaveChangesAsync` call**.

`EventId` is a UUID generated at event creation time and carried in every `IntegrationEvent`. `ConsumerName` is a stable string constant per consumer class.

### Service Coverage

| Service | Outbox | Inbox |
|---|---|---|
| ProposalService | Yes (`OutboxMessages` table) | Yes (`InboxMessages` table) |
| WorkflowWorker | No | Yes (`InboxMessages` table) |
| NotificationService | No | Yes (`InboxMessages` table) |

Each service maintains its own `InboxMessages` table in its own database, ensuring full bounded-context isolation.

## Consequences

- **At-least-once delivery** guaranteed end-to-end via Outbox + consumer ACK.
- **Idempotency** guaranteed per `(EventId, ConsumerName)` pair — safe to redeliver.
- **Atomicity** for domain state + inbox entry: if `SaveChangesAsync` fails, the message is NACKed and redelivered; on retry the inbox check prevents double-processing.
- Slight write amplification from Outbox and Inbox tables — acceptable given the consistency guarantees.
- Consumers must not perform irreversible external side effects (e.g. real emails) before recording the inbox entry.
