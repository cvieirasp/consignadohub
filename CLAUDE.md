# ConsignadoHub (Portfolio Project Context)

## 1) Project Summary
**ConsignadoHub** is a senior-level .NET portfolio project that demonstrates how to build **scalable, resilient, and high-quality** systems using:
- **ASP.NET Core (.NET 10)**, **C#**
- **EF Core** with **SQL Server**
- **Microservices** + **event-driven workflow**
- **RabbitMQ** messaging with **Outbox Pattern** and **idempotent consumers**
- **Keycloak** for **OIDC authentication** and **RBAC authorization**
- **Automated testing** (unit + integration, with Testcontainers)
- **Observability** (structured logs, correlation IDs, OpenTelemetry traces/metrics)
- **CI/CD** via GitHub Actions + SonarQube + CodeQL

This repository is designed to closely match a real-world **Senior .NET Developer** role: APIs, performance, security, quality, integrations, code review standards, and operational readiness.

---

## 2) Business Domain (Fictional but Realistic)
The system simulates a **consigned credit** proposal pipeline (common in financial systems):
1. Customer registration and lookup
2. Proposal simulation (installments, rates, simplified CET)
3. Proposal submission
4. Asynchronous workflow:
   - Credit analysis
   - Contract generation
   - Disbursement processing
5. Notifications sent asynchronously (email/webhook stubs)

The domain rules are simplified for portfolio scope but implemented with production-grade engineering practices.

---

## 3) Architecture Overview (Monorepo, Multiple Services)
This is a **monorepo** containing four services and shared building blocks:

### Services
1. **CustomerService** (API)
   - Customer CRUD and search
   - SQL Server database: `ConsignadoHub_Customers`
2. **ProposalService** (API)
   - Simulation, proposal submission, status/timeline
   - SQL Server database: `ConsignadoHub_Proposals`
   - Outbox table for reliable event publishing
3. **WorkflowWorker** (Worker)
   - RabbitMQ consumers for the workflow stages:
     - credit analysis → contract generation → disbursement
4. **NotificationService** (Worker)
   - RabbitMQ consumer that performs side effects (email/webhook stubs)
   - Never mutates domain state

### Core Integration
- RabbitMQ exchange: `consignadohub.events` (topic)
- Routing keys example: `proposal.submitted`, `proposal.credit.completed`, `contract.generated`, `disbursement.completed`

### Clean Architecture per service
Each service follows:
- `Domain` (entities, value objects, domain rules/events)
- `Application` (use cases, handlers, validation, ports)
- `Infrastructure` (EF Core, repositories, outbox, messaging adapters)
- `Api` or `Worker` (controllers/hosted services, DI, middleware, endpoints)

---

## 4) Repository Layout (Expected)
Key paths:
- `src/building-blocks/ConsignadoHub.BuildingBlocks/`  
  Cross-cutting utilities: Result/Error, Guard, messaging abstractions, RabbitMQ adapters, Outbox dispatcher, observability, health checks, correlation middleware.
- `src/services/*`  
  One folder per service with `src/` and `tests/`.
- `docs/architecture/`  
  ADRs, event flow, diagrams.
- `infra/local/`  
  Local resources (RabbitMQ defs, SQL init, optional Keycloak realm import).
- `.github/workflows/`  
  CI/CD workflows: `ci.yml`, `release.yml`, `codeql.yml`.

---

## 5) Key Technical Decisions (ADRs)
The project includes ADRs that define “why” and “how” decisions were made:
- **ADR-001** Messaging with RabbitMQ (topic exchange, dedicated queues, hosted consumers)
- **ADR-002** Outbox Pattern + Idempotent Consumers (Inbox table)
- **ADR-003** EF Core + SQL Server (migrations, indices, concurrency)
- **ADR-004** Keycloak OIDC AuthN/AuthZ (JWT bearer, RBAC policies)
- **ADR-005** Observability with OpenTelemetry (logs/traces/metrics)
- **ADR-006** API versioning + ProblemDetails error contract
- **ADR-007** NotificationService as fan-out side effects consumer

---

## 6) Authentication & Authorization (Keycloak OIDC)
Keycloak is the Identity Provider (IdP). APIs are Resource Servers validating JWT tokens.

### Realm / Client (Recommended)
- Realm: `consignadohub`
- Client: `consignadohub-api` (confidential)
- Roles (realm roles):
  - `consignado-admin`
  - `consignado-analyst`

### Authorization Model
- Use **policy-based authorization** in ASP.NET Core.
- Protect endpoints by roles; keep public endpoints minimal (e.g., health endpoints may be unauthenticated for local dev).
- **NotificationService** does not expose public APIs by default; it consumes events only.

### Testing Tokens (Local)
- Use Postman to acquire tokens from Keycloak token endpoint (password grant for dev/testing).
- Swagger UI may be configured with OAuth2 if desired; otherwise use Bearer tokens directly.

---

## 7) Messaging Reliability Rules
### Outbox (ProposalService)
- When writing domain changes that must emit events:
  1) Persist domain state + Outbox message in the **same DB transaction**
  2) Commit
  3) `OutboxDispatcherHostedService` publishes pending messages
  4) Mark as processed with attempt/error tracking

### Idempotency (All Consumers)
- Each consumer keeps `InboxProcessedMessages` (or equivalent):
  - Key: `(EventId, ConsumerName)`
- If already processed, ACK and ignore.
- Consumers must tolerate redelivery, retries, and partial failures.

### Correlation
- Every event includes:
  - `EventId` (UUID)
  - `OccurredAt` (UTC)
  - `CorrelationId` (propagated from the initiating HTTP request)
- CorrelationId must appear in logs, traces, and ProblemDetails responses.

---

## 8) API Design Conventions
### Versioning
- URL-based versioning: `/v1/...`

### Error Handling
- Use RFC 7807 **ProblemDetails** consistently:
  - `type`, `title`, `status`, `detail`, `instance`
  - `extensions`: `correlationId`, `errorCode`, `validationErrors`

### REST Guidelines
- Use proper HTTP semantics:
  - 201 Created for resource creation
  - 400 for validation
  - 401/403 for auth issues
  - 404 for not found
  - 409/422 for domain rule violations (choose and document)
  - 503 for dependency failures (optionally include retry guidance)

### Performance Guidelines
- Use `AsNoTracking()` for read-only queries.
- Consider `AddDbContextPool`.
- Avoid N+1 patterns; load only what is needed.
- Add indices for query-heavy columns (document/CPF, customerId, proposal status).

---

## 9) Observability Standards
### Logging
- Structured logging (no string concatenation for key fields).
- Always include:
  - `CorrelationId`
  - `EventId` (for consumers)
  - `ProposalId` / `CustomerId` when relevant

### OpenTelemetry
- Instrument:
  - ASP.NET Core
  - HttpClient
  - EF Core
  - RabbitMQ publish/consume (manual spans if needed)
- Export:
  - Console for dev
  - OTLP exporter optionally (collector)

### Health Checks
- `/health/live` (process up)
- `/health/ready` (db + rabbit connectivity)

---

## 10) Testing Strategy
### Unit Tests
- Domain invariants, state transitions, calculation rules
- Application handlers and validation
- Result/Error mapping and exception handling rules

### Integration Tests
- WebApplicationFactory for API tests
- Testcontainers for:
  - SQL Server
  - RabbitMQ
- Critical flows:
  - Create customer
  - Simulate proposal
  - Submit proposal → Outbox entry created
  - Dispatcher publishes message (optional verification)
  - WorkflowWorker consumes and progresses the process (can be partial per milestone)

---

## 11) CI/CD Expectations
### Pull Request CI (`.github/workflows/ci.yml`)
- Restore / Build
- Unit tests
- Integration tests
- SonarQube scan + quality gate
- Upload test artifacts (trx/junit) where applicable

### Security
- CodeQL workflow enabled (`codeql.yml`)
- Secret scanning (avoid committing secrets; use env vars)

### Release (`release.yml`)
- Build and push Docker images (GHCR recommended)
- Optional deploy step to AWS/Azure (portfolio enhancement)

---

## 12) Coding Standards & PR Checklist
### Commit Conventions
Use Conventional Commits:
- `feat: ...`
- `fix: ...`
- `refactor: ...`
- `test: ...`
- `docs: ...`
- `chore: ...`

### PR Checklist (Required)
- [ ] Unit tests added/updated
- [ ] Integration tests added/updated (if applicable)
- [ ] ProblemDetails mapping preserved
- [ ] CorrelationId propagated and logged
- [ ] Consumers are idempotent (Inbox check)
- [ ] No secrets committed
- [ ] Swagger updated (API changes)
- [ ] ADR created/updated if architecture changed

### Definition of Done
- Build passes locally and in CI
- Tests pass
- Errors are consistent (ProblemDetails)
- Observability is present (logs + basic tracing)
- Migrations consistent and runnable
- Documentation updated (README/docs)

---

## 13) Event Catalog (Minimum)
All events implement a shared `IIntegrationEvent` with required metadata.

### `ProposalSubmitted`
- `ProposalId`, `CustomerId`, `RequestedAmount`, `TermMonths`

### `CreditAnalysisCompleted`
- `ProposalId`, `Approved`, `Score`, `Reason`

### `ContractGenerated`
- `ProposalId`, `ContractId`, `ContractUrl`

### `DisbursementCompleted`
- `ProposalId`, `DisbursementId`, `CompletedAt`

NotificationService consumes these for fan-out side effects.

---

## 14) Local Development (Docker Compose Baseline)
Local environment should include:
- SQL Server
- RabbitMQ (management UI enabled)
- Keycloak
- Services running as containers or locally (hybrid supported)

Use environment variables for:
- Connection strings
- RabbitMQ host/user/pass
- Keycloak issuer/audience/client settings

---

## 15) Scope & Milestones (Suggested Implementation Plan)
1. **Milestone 1**: Customer + Proposal APIs (sync), DB migrations, Swagger, ProblemDetails, basic logs/health
2. **Milestone 2**: RabbitMQ + Outbox in ProposalService, WorkflowWorker credit analysis
3. **Milestone 3**: Contract + Disbursement stages, NotificationService fan-out
4. **Milestone 4**: Observability (OTel), Testcontainers integration tests, CI + Sonar + CodeQL
5. **Milestone 5**: Optional deploy to AWS/Azure + runbooks

---

## 16) What This Project Intentionally Demonstrates (Interview Talking Points)
- Designing APIs with clear contracts, versioning, and robust error handling
- Event-driven workflows with reliability (Outbox) and safety (idempotency)
- Security with Keycloak (OIDC) and RBAC policies
- Testing pyramid with real infrastructure integration tests
- Production-minded observability and operational endpoints
- CI/CD automation and quality gates

---