# ADR-008: Consumer prefetchCount=1 for Sequential Message Processing

**Status:** Accepted
**Date:** 26/02/2026

## Context

`RabbitMqConsumerBase<TEvent>` configured `BasicQos` with `prefetchCount: 10`, allowing up to 10
unacknowledged messages to be in-flight simultaneously within a single consumer instance. Each
incoming message fires an async `ReceivedAsync` handler which runs concurrently with any other
in-flight handlers.

This caused a **TOCTOU (Time-Of-Check, Time-Of-Use) race condition** in the inbox idempotency
check:

```
Thread A:  ExistsAsync → false ─────────────────────────── SaveChangesAsync ← commits OK
Thread B:  ExistsAsync → false ── load proposal ── ... ─── SaveChangesAsync ← unique constraint violation
```

Both threads read "not yet processed" from the inbox before either commits. The second thread's
`SaveChangesAsync` fails with a unique constraint violation on `(EventId, ConsumerName)`, which EF
Core surfaces as `DbUpdateException`. Because the base consumer NACKs with `requeue: false` on any
exception, the message is sent to the DLQ - even though the event was successfully processed by the
first thread.

The proposal workflow is a **sequential state machine** (`Submitted → Approved/Rejected → Contract
Generated → Disbursed`). Processing two events for the same proposal concurrently in the same consumer
instance is never correct.

## Decision

Set `prefetchCount: 1` in `RabbitMqConsumerBase<TEvent>`.

```csharp
await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, ...);
```

Each consumer instance now ACKs or NACKs a message before RabbitMQ delivers the next one,
serialising processing within a single instance.

## Alternatives Considered

### Catch `DbUpdateException` and re-check the inbox (rejected)

Re-check the inbox after the exception; return success if the entry now exists. This prevents the DLQ
but does not eliminate the race - two threads still run concurrently, write the proposal state twice,
and add duplicate timeline entries. It couples the Application layer to an EF Core exception type and
adds a second round-trip on the failure path. Treating a race condition as a recoverable exception is
a symptom fix, not a root-cause fix.

### Pessimistic locking via `SELECT ... WITH (UPDLOCK, ROWLOCK)` (rejected)

A database-level write lock prevents concurrent reads-for-update. This is correct but requires raw
SQL or a custom EF Core interceptor, adds latency for every message, and still does not prevent
double-processing if two instances of the service run (the lock would need to be distributed).
Overkill given that `prefetchCount: 1` solves the problem at a lower cost.

### Replace `ExistsAsync` + `AddAsync` with insert-and-catch-duplicate (rejected)

Skip the pre-check and let the unique constraint on `(EventId, ConsumerName)` be the idempotency
guard. Catch `DbUpdateException` for SQL error 2627. This is a valid pattern for high-throughput
scenarios but introduces SQL Server-specific error handling in the consumer. With `prefetchCount: 1`
the race cannot occur, so the pre-check approach remains simple and sufficient.

### Scale throughput via multiple service instances (accepted as complement)

`prefetchCount: 1` reduces per-instance throughput. Horizontal scaling (multiple `WorkflowWorker` or
`ProposalService` replicas) recovers throughput without re-introducing in-process concurrency. Each
replica processes one message at a time; RabbitMQ distributes messages across replicas via competing
consumers. This is the preferred scaling strategy for this domain.

## Consequences

- **Correctness (TOCTOU race):** The inbox idempotency check is race-free within a single consumer
  instance. Duplicate concurrent `SaveChanges` for the same event can no longer occur in-process.
- **Sequential processing per instance:** At most one message is processed at a time per consumer
  instance. This matches the sequential nature of the proposal state machine.
- **Throughput:** Reduced per-instance throughput. Acceptable for the current load profile; scale-out
  via additional replicas if needed.
- **Simplicity:** No changes required to use-case logic, repository layer, or exception handling.
  The fix is a single line in the shared infrastructure base class.
- **Applies to all consumers:** `prefetchCount: 1` affects every consumer that extends
  `RabbitMqConsumerBase<TEvent>`. This is intentional - all consumers in this system share the same
  sequential-state-machine semantics where correctness outweighs per-instance concurrency.

## Post-Decision: Separate EF Core Tracking Bug

After `prefetchCount: 1` was adopted, a `DbUpdateConcurrencyException` continued to appear on the
path through `HandleCreditAnalysisCompletedUseCase`. Investigation showed this was a **distinct root
cause** unrelated to in-process concurrency:

**Root cause:** `GetByIdForUpdateAsync` loaded the `Proposal` without `.Include(p => p.Timeline)`.
When `UpdateStatus` → `AddTimelineEntry` added a new `ProposalTimelineEntry` (with a client-generated
non-default Guid) to the `_timeline` field, EF Core's `DetectChanges()` found the entry with no
navigation baseline. Because `ProposalTimelineEntry.Id` was `ValueGeneratedOnAdd()` by convention,
EF Core could not distinguish "just created" from "already persisted" and tracked the entry as
`Unchanged`. Relationship fixup then set `ProposalId` (already the correct value), transitioning the
entry to `Modified`. EF Core generated `UPDATE [ProposalTimeline] ... WHERE [Id] = @newGuid` - the
row does not exist yet - `@@ROWCOUNT = 0` → `DbUpdateConcurrencyException`.

**Fixes applied (infrastructure layer):**

1. `GetByIdForUpdateAsync` now includes `.Include(p => p.Timeline)`. EF Core has a clear baseline:
   existing entries are tracked as `Unchanged`; the new entry (not in the identity map) is correctly
   tracked as `Added` → `INSERT`.
2. `ProposalTimelineConfiguration` adds `ValueGeneratedNever()` for `Id`, making explicit that the
   key is always set client-side. This eliminates the ambiguity even if the Include is ever removed.
3. `ProposalTimelineConfiguration` adds `UsePropertyAccessMode(PropertyAccessMode.Property)` for
   correct materialization of loaded entries (consistent with `ProposalConfiguration`).
