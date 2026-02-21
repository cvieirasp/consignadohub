# ADR-001: Messaging with RabbitMQ

**Status:** Planned (Milestone 2)
**Date:** 2026-02-20

## Context

The proposal workflow requires asynchronous processing across multiple stages (credit analysis, contract generation, disbursement). A message broker is needed to decouple producers from consumers.

## Decision (to be implemented in M2)

- **RabbitMQ** with a single **topic exchange** (`consignadohub.events`).
- Each consumer has a **dedicated queue** with a dead-letter queue (DLQ) for failed messages.
- Routing key pattern: `<aggregate>.<event>` (e.g. `proposal.submitted`, `proposal.credit.completed`).
- Consumers are registered as `IHostedService` implementations.

## Consequences

- Event-driven decoupling between ProposalService and WorkflowWorker.
- Retry and DLQ strategy needed for reliability.
- See ADR-002 for the Outbox pattern used to guarantee at-least-once delivery.
