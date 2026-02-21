# ADR-002: Outbox Pattern + Idempotent Consumers

**Status:** Planned (Milestone 2)
**Date:** 2026-02-20

## Context

When a proposal is submitted, the domain state change (write to DB) and the event publication (to RabbitMQ) must happen atomically. Without this guarantee, a crash between the two operations causes either a lost event or a duplicate state change.

## Decision (to be implemented in M2)

### Outbox (ProposalService)

1. Domain state + `OutboxMessage` written in a **single DB transaction**.
2. `OutboxDispatcherHostedService` polls for pending messages and publishes to RabbitMQ.
3. Successfully published messages are marked with `ProcessedAt`, attempt count, and any error.

### Inbox / Idempotent Consumers (WorkflowWorker, NotificationService)

- Each consumer checks `InboxProcessedMessages` table for `(EventId, ConsumerName)` before processing.
- If already processed → ACK and skip.
- `EventId` is a UUID carried in every integration event.

## Consequences

- At-least-once delivery guaranteed end-to-end.
- Consumers must be idempotent (safe to re-process).
- Slight write amplification from Outbox table — acceptable given consistency guarantees.
