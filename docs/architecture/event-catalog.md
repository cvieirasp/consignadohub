# Event Catalog

This document is the authoritative reference for all integration events in ConsignadoHub.
It is derived directly from `ConsignadoHub.Contracts` and the consumer registrations across all services.

---

## Infrastructure

| Property | Value |
|---|---|
| Exchange | `consignadohub.events` |
| Exchange type | `topic` |
| Serialization | JSON (UTF-8) |
| Base record | `ConsignadoHub.BuildingBlocks.Messaging.IntegrationEvent` |

### Base fields (inherited by every event)

| Field | Type | Description |
|---|---|---|
| `EventId` | `Guid` | Unique identifier for this event instance. Used as idempotency key in Inbox checks. |
| `OccurredAt` | `DateTimeOffset` | UTC timestamp when the event was created. |
| `CorrelationId` | `string` | Propagated from the originating HTTP request. Appears in all logs and traces. |

---

## Workflow Overview

```
[HTTP POST /v1/proposals]
        │
        ▼
  ProposalService ──(Outbox)──► routing key: proposal.submitted
        │
        ├──► WorkflowWorker (queue: proposal.submitted)
        │         └── credit analysis
        │               └──► routing key: proposal.credit.completed
        │                         │
        │         ┌───────────────┴──────────────────────────┐
        │         ▼                                          ▼
        │   ProposalService                          WorkflowWorker
        │   (queue: proposal.credit.completed)       (queue: workflow.credit.completed)
        │   updates status: Approved|Rejected        if Approved → contract generation
        │                                                  └──► routing key: contract.generated
        │                                                            │
        │                                     ┌────────────────────┴─────────────────────┐
        │                                     ▼                                           ▼
        │                             ProposalService                             WorkflowWorker
        │                             (queue: proposal.contract.generated)        (queue: workflow.contract.generated)
        │                             updates status: ContractGenerated           disbursement processing
        │                                                                               └──► routing key: disbursement.completed
        │                                                                                         │
        │                                                               ┌─────────────────────────┘
        │                                                               ▼
        │                                                       ProposalService
        │                                                       (queue: proposal.disbursement.completed)
        │                                                       updates status: Disbursed
        │
        └──► NotificationService (fan-out, reads all 4 events, emits stubs only)
```

---

## Events

---

### `ProposalSubmittedEvent`

Emitted when a customer successfully submits a credit proposal.

**C# type**: `ConsignadoHub.Contracts.Events.ProposalSubmittedEvent`
**Routing key**: `proposal.submitted`
**Publisher**: ProposalService — written to Outbox inside the same `SaveChanges` transaction as the `Proposal` entity, then dispatched by `OutboxDispatcherHostedService`.

#### Payload

| Field | Type | Description |
|---|---|---|
| `ProposalId` | `Guid` | The newly created proposal. |
| `CustomerId` | `Guid` | The customer who owns the proposal. |
| `RequestedAmount` | `decimal` | Gross loan amount requested (BRL). |
| `TermMonths` | `int` | Number of installments requested. |

#### Consumers

| Service | Queue | Consumer class | Idempotent | Action |
|---|---|---|---|---|
| WorkflowWorker | `proposal.submitted` | `ProposalSubmittedConsumer` | Yes | Runs credit scoring algorithm; publishes `CreditAnalysisCompletedEvent`. |
| NotificationService | `notification.proposal.submitted` | `ProposalSubmittedNotificationConsumer` | Yes | Sends "proposal received" notification stub (email/webhook). |

---

### `CreditAnalysisCompletedEvent`

Emitted when the credit analysis step finishes (approved or rejected).

**C# type**: `ConsignadoHub.Contracts.Events.CreditAnalysisCompletedEvent`
**Routing key**: `proposal.credit.completed`
**Publisher**: WorkflowWorker — `ProposalSubmittedConsumer` publishes directly (no Outbox; WorkflowWorker is stateless).

#### Payload

| Field | Type | Description |
|---|---|---|
| `ProposalId` | `Guid` | The proposal under analysis. |
| `Approved` | `bool` | `true` if the proposal was approved. |
| `Score` | `int` | Credit score computed (0–1000). |
| `Reason` | `string` | Human-readable decision reason. |

#### Consumers

| Service | Queue | Consumer class | Idempotent | Action |
|---|---|---|---|---|
| ProposalService | `proposal.credit.completed` | `CreditAnalysisCompletedConsumer` | Yes (Inbox) | Updates proposal status to `Approved` or `Rejected`. |
| WorkflowWorker | `workflow.credit.completed` | `CreditAnalysisCompletedConsumer` | Yes | If `Approved`: generates contract stub and publishes `ContractGeneratedEvent`. If `Rejected`: records Inbox entry and stops. |
| NotificationService | `notification.credit.completed` | `CreditAnalysisCompletedNotificationConsumer` | Yes | Sends "credit analysis result" notification stub. |

---

### `ContractGeneratedEvent`

Emitted when the contract document is generated for an approved proposal.

**C# type**: `ConsignadoHub.Contracts.Events.ContractGeneratedEvent`
**Routing key**: `contract.generated`
**Publisher**: WorkflowWorker — `CreditAnalysisCompletedConsumer` publishes directly.

#### Payload

| Field | Type | Description |
|---|---|---|
| `ProposalId` | `Guid` | The proposal the contract belongs to. |
| `ContractId` | `Guid` | Unique identifier of the generated contract document. |
| `ContractUrl` | `string` | URL where the contract PDF can be retrieved (stub). |

#### Consumers

| Service | Queue | Consumer class | Idempotent | Action |
|---|---|---|---|---|
| ProposalService | `proposal.contract.generated` | `ContractGeneratedConsumer` | Yes (Inbox) | Updates proposal status to `ContractGenerated`. |
| WorkflowWorker | `workflow.contract.generated` | `ContractGeneratedConsumer` | Yes | Processes disbursement stub and publishes `DisbursementCompletedEvent`. |
| NotificationService | `notification.contract.generated` | `ContractGeneratedNotificationConsumer` | Yes | Sends "contract ready" notification stub. |

---

### `DisbursementCompletedEvent`

Emitted when the loan funds are disbursed to the customer. Terminal event of the happy path.

**C# type**: `ConsignadoHub.Contracts.Events.DisbursementCompletedEvent`
**Routing key**: `disbursement.completed`
**Publisher**: WorkflowWorker — `ContractGeneratedConsumer` publishes directly.

#### Payload

| Field | Type | Description |
|---|---|---|
| `ProposalId` | `Guid` | The proposal that has been funded. |
| `DisbursementId` | `Guid` | Unique identifier of the disbursement record. |
| `CompletedAt` | `DateTimeOffset` | UTC timestamp when disbursement completed. |

#### Consumers

| Service | Queue | Consumer class | Idempotent | Action |
|---|---|---|---|---|
| ProposalService | `proposal.disbursement.completed` | `DisbursementCompletedConsumer` | Yes (Inbox) | Updates proposal status to `Disbursed` (terminal). |
| NotificationService | `notification.disbursement.completed` | `DisbursementCompletedNotificationConsumer` | Yes | Sends "funds disbursed" notification stub. |

---

## Queue Registry

Complete list of all queues bound to `consignadohub.events`.

| Queue | Routing key | Bound to service |
|---|---|---|
| `proposal.submitted` | `proposal.submitted` | WorkflowWorker |
| `proposal.credit.completed` | `proposal.credit.completed` | ProposalService |
| `proposal.contract.generated` | `contract.generated` | ProposalService |
| `proposal.disbursement.completed` | `disbursement.completed` | ProposalService |
| `workflow.credit.completed` | `proposal.credit.completed` | WorkflowWorker |
| `workflow.contract.generated` | `contract.generated` | WorkflowWorker |
| `notification.proposal.submitted` | `proposal.submitted` | NotificationService |
| `notification.credit.completed` | `proposal.credit.completed` | NotificationService |
| `notification.contract.generated` | `contract.generated` | NotificationService |
| `notification.disbursement.completed` | `disbursement.completed` | NotificationService |

---

## Reliability Guarantees

### Publishing (ProposalService)
- Events are written to the `OutboxMessages` table in the **same database transaction** as the domain change.
- `OutboxDispatcherHostedService` polls every 5 seconds, publishes pending messages to RabbitMQ, and marks them as processed.
- Ensures **at-least-once delivery**: if the process restarts before marking a message as processed, it will be re-published.

### Consuming (all services)
- All consumers use the **Inbox pattern**: `InboxMessages(EventId, ConsumerName)` composite PK.
- Before processing, each consumer checks whether the event was already handled. If so, it ACKs and returns immediately.
- Guarantees **exactly-once side effects** despite at-least-once delivery.
- On unhandled exception, the consumer NACKs with `requeue=false` (dead-letter ready).
- `prefetchCount=1` per consumer ensures sequential processing per queue, removing the need for optimistic concurrency tokens on `Proposal`.

### CorrelationId propagation
- Every consumer reads `event.CorrelationId` and passes it to downstream events it publishes.
- All log entries include `CorrelationId` so a single trace can be reconstructed across services.

---

## Proposal Status Transitions

The proposal status is updated exclusively through event consumers in ProposalService.

```
Submitted
    │
    ├─[Approved = false]─► Rejected  (terminal)
    │
    └─[Approved = true]──► Approved
                               │
                               ▼
                        ContractGenerated
                               │
                               ▼
                           Disbursed  (terminal)
```

| Status | Set by consumer of | Queue |
|---|---|---|
| `Approved` | `CreditAnalysisCompletedEvent` | `proposal.credit.completed` |
| `Rejected` | `CreditAnalysisCompletedEvent` | `proposal.credit.completed` |
| `ContractGenerated` | `ContractGeneratedEvent` | `proposal.contract.generated` |
| `Disbursed` | `DisbursementCompletedEvent` | `proposal.disbursement.completed` |

---

## Adding a New Event (Checklist)

1. Add record to `ConsignadoHub.Contracts/Events/` inheriting `IntegrationEvent`.
2. Define a routing key (lower-kebab, e.g. `resource.verb`).
3. Implement publisher: use `IEventPublisher.PublishAsync(event, routingKey, ct)`.
   - If published from a domain write: use the Outbox (same `SaveChanges` transaction).
   - If published from a stateless worker: publish directly.
4. Implement consumer(s): extend `RabbitMqConsumerBase<TEvent>`, declare `QueueName`, `RoutingKey`, `ConsumerName`.
5. Add Inbox idempotency check at the top of `HandleAsync`.
6. Register consumer as `IHostedService` in the service's DI setup.
7. Update this document: add event section, update Queue Registry, update status transition table if applicable.
8. Create or update an ADR if the event introduces a new architectural pattern.
