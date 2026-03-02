# ADR-001: Messaging with RabbitMQ

**Status:** Accepted
**Date:** 2026-02-20
**Updated:** 2026-02-26 (Milestone 3 — full event catalog documented)

## Context

The proposal workflow requires asynchronous processing across multiple stages (credit analysis, contract generation, disbursement). A message broker is needed to decouple producers from consumers and support fan-out to side-effect services.

## Decision

- **RabbitMQ** with a single **topic exchange** (`consignadohub.events`).
- Each consumer has a **dedicated queue** bound to the exchange with its routing key. Multiple queues can bind to the same routing key for fan-out.
- Routing key pattern: `<aggregate>.<event>` (e.g. `proposal.submitted`, `contract.generated`).
- Consumers are implemented as `IHostedService` subclasses of `RabbitMqConsumerBase<TEvent>`.
- `prefetchCount: 1` per consumer channel (see ADR-008 for rationale).

## Event Catalog

| Routing Key | Published by | Queues (subscribers) |
|---|---|---|
| `proposal.submitted` | ProposalService (Outbox) | `proposal.submitted` (WorkflowWorker), `notification.proposal.submitted` (NotificationService) |
| `proposal.credit.completed` | WorkflowWorker | `proposal.credit.completed` (ProposalService), `workflow.credit.completed` (WorkflowWorker), `notification.credit.completed` (NotificationService) |
| `contract.generated` | WorkflowWorker | `proposal.contract.generated` (ProposalService), `workflow.contract.generated` (WorkflowWorker), `notification.contract.generated` (NotificationService) |
| `disbursement.completed` | WorkflowWorker | `proposal.disbursement.completed` (ProposalService), `notification.disbursement.completed` (NotificationService) |

## Queue Naming Convention

Queues are named `<service-prefix>.<routing-key>` where the prefix identifies the consuming service:
- `proposal.*` — ProposalService (updates domain state)
- `workflow.*` — WorkflowWorker (drives the next workflow stage)
- `notification.*` — NotificationService (fan-out side effects only)

## Consequences

- Event-driven decoupling between all services; producers have no knowledge of consumers.
- Multiple consumers on the same routing key are supported without producer changes.
- At-least-once delivery guaranteed via the Outbox pattern (see ADR-002).
- Per-queue dedicated bindings make it straightforward to add new consumers without reconfiguring existing ones.
