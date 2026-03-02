# ConsignadoHub

A senior-level .NET portfolio project that simulates a **consigned credit proposal pipeline** using microservices, event-driven architecture, and production-grade engineering practices.

---

## Table of Contents

- [Overview](#overview)
- [Architecture](#architecture)
- [Tech Stack](#tech-stack)
- [Project Structure](#project-structure)
- [Prerequisites](#prerequisites)
- [Local Development](#local-development)
- [Running the Services](#running-the-services)
- [API Reference](#api-reference)
- [Authentication](#authentication)
- [Event Flow](#event-flow)
- [Running Tests](#running-tests)
- [CI/CD](#cicd)
- [Architecture Decisions](#architecture-decisions)

---

## Overview

ConsignadoHub models the back-office pipeline of a financial institution that processes consigned credit proposals:

1. A customer is registered and looked up by CPF
2. An analyst simulates and submits a credit proposal
3. The proposal flows asynchronously through credit analysis → contract generation → disbursement
4. Notifications are dispatched as side effects after each stage

The domain rules are intentionally simplified for portfolio scope, but the engineering practices—reliability, security, observability, and testability—reflect production standards.

---

## Architecture

Four services communicate through a **RabbitMQ topic exchange** (`consignadohub.events`). Reliable publishing is guaranteed by the **Outbox Pattern**. Consumers are **idempotent** via an Inbox table.

```
┌─────────────────┐    HTTP     ┌──────────────────┐
│  Client / UI    │────────────▶│  CustomerService  │  :5100
└─────────────────┘             │  (CRUD, search)  │
                                └──────────────────┘

┌─────────────────┐    HTTP     ┌──────────────────┐
│  Client / UI    │────────────▶│  ProposalService  │  :5200
└─────────────────┘             │ (simulate/submit) │
                                └────────┬─────────┘
                                         │ ProposalSubmitted (Outbox)
                                         ▼
                                ┌──────────────────┐
                          ┌────▶│  WorkflowWorker   │
                          │     │  (credit analysis │
                          │     │   → contract      │
                          │     │   → disbursement) │
                          │     └──────────────────┘
                          │
                          │     ┌──────────────────┐
                          └────▶│NotificationService│
                                │  (email/webhook   │
                                │   stubs)          │
                                └──────────────────┘
```

Each service follows **Clean Architecture**:

| Layer | Responsibility |
|---|---|
| `Domain` | Entities, value objects, domain rules |
| `Application` | Use cases, ports, validation |
| `Infrastructure` | EF Core, repositories, messaging adapters |
| `Api` / `Worker` | HTTP endpoints or hosted consumers, DI wiring |

---

## Tech Stack

| Concern | Choice |
|---|---|
| Runtime | .NET 10 / C# |
| APIs | ASP.NET Core Minimal APIs |
| ORM | EF Core 10 + SQL Server |
| Messaging | RabbitMQ 4 (topic exchange) |
| Reliability | Outbox Pattern + Idempotent consumers (Inbox) |
| Auth | Keycloak 26 — OIDC JWT Bearer + RBAC |
| API Docs | Scalar (OpenAPI 3) |
| Logging | Serilog (structured, correlation ID enrichment) |
| Observability | OpenTelemetry (traces + metrics + logs) |
| Testing | xUnit, Testcontainers |
| CI | GitHub Actions + SonarCloud + CodeQL |
| Containers | Docker (multi-stage, non-root) + GHCR |

---

## Project Structure

```
ConsignadoHub/
├── src/
│   ├── building-blocks/
│   │   ├── ConsignadoHub.BuildingBlocks/   # Auth, messaging, outbox, observability, correlation
│   │   └── ConsignadoHub.Contracts/        # Integration event types (shared)
│   └── services/
│       ├── CustomerService/                # Customer CRUD and search
│       ├── ProposalService/                # Simulation, submission, workflow consumers
│       ├── WorkflowWorker/                 # Credit analysis, contract, disbursement
│       └── NotificationService/            # Fan-out notification consumers
├── infra/
│   └── local/
│       ├── docker-compose.yml
│       └── keycloak/
│           └── realm-export.json           # Pre-configured realm (auto-imported)
├── docs/
│   └── architecture/
│       └── adr/                            # Architecture Decision Records
├── .github/
│   └── workflows/
│       ├── ci.yml                          # Build → unit → integration → SonarCloud
│       ├── release.yml                     # Docker image build and push to GHCR
│       └── codeql.yml                      # Static security analysis
├── Makefile                                # infra, migrations, coverage targets
└── Directory.Packages.props               # Central NuGet version management
```

---

## Prerequisites

| Tool | Version |
|---|---|
| .NET SDK | 10.0+ |
| Docker | 24+ with Compose v2 |
| `dotnet-ef` | 10.0+ |
| `make` | Any |

Install the EF Core CLI tool if needed:

```bash
dotnet tool install --global dotnet-ef
```

---

## Local Development

### 1. Start the infrastructure

```bash
make infra-up
```

This starts SQL Server, RabbitMQ, and Keycloak via Docker Compose.

| Service | URL | Credentials |
|---|---|---|
| SQL Server | `localhost,1444` | `sa` / `ConsignadoHub!2026` |
| RabbitMQ Management | http://localhost:15672 | `consignadohub` / `ConsignadoHub!2026` |
| Keycloak Admin | http://localhost:8080 | `admin` / `admin` |

Wait for all containers to report healthy (takes ~60 s the first time due to Keycloak startup):

```bash
make infra-ps
```

### 2. Apply database migrations

Once SQL Server is healthy:

```bash
make migrate-all
```

Or per service:

```bash
make migrate-customer
make migrate-proposal
make migrate-workflow
make migrate-notification
```

### 3. Run the services

Open four terminals and run each service:

```bash
# Terminal 1
dotnet run --project src/services/CustomerService/src/CustomerService.Api

# Terminal 2
dotnet run --project src/services/ProposalService/src/ProposalService.Api

# Terminal 3
dotnet run --project src/services/WorkflowWorker/src/WorkflowWorker.Worker

# Terminal 4
dotnet run --project src/services/NotificationService/src/NotificationService.Worker
```

Migrations are also applied automatically on startup in the `Development` environment.

### Stopping the infrastructure

```bash
make infra-down        # stop containers, keep volumes
make infra-down-v      # stop containers and delete volumes
```

---

## API Reference

Interactive API documentation is available in the `Development` environment via **Scalar**:

| Service | Scalar UI |
|---|---|
| CustomerService | http://localhost:5100/scalar/v1 |
| ProposalService | http://localhost:5200/scalar/v1 |

### CustomerService — `localhost:5100`

| Method | Path | Role | Description |
|---|---|---|---|
| `GET` | `/v1/customers` | analyst, admin | Search customers |
| `GET` | `/v1/customers/{id}` | analyst, admin | Get by ID |
| `GET` | `/v1/customers/cpf/{cpf}` | analyst, admin | Get by CPF |
| `POST` | `/v1/customers` | admin | Create customer |
| `PUT` | `/v1/customers/{id}` | admin | Update customer |
| `DELETE` | `/v1/customers/{id}` | admin | Deactivate customer |
| `GET` | `/health/live` | — | Liveness probe |
| `GET` | `/health/ready` | — | Readiness probe (DB) |

### ProposalService — `localhost:5200`

| Method | Path | Role | Description |
|---|---|---|---|
| `POST` | `/v1/proposals/simulate` | public | Simulate installments and CET |
| `POST` | `/v1/proposals` | analyst, admin | Submit proposal |
| `GET` | `/v1/proposals/{id}` | analyst, admin | Get proposal + timeline |
| `GET` | `/health/live` | — | Liveness probe |
| `GET` | `/health/ready` | — | Readiness probe (DB + RabbitMQ) |

### Error Format

All errors follow **RFC 7807 ProblemDetails**:

```json
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "Validation failed",
  "status": 400,
  "detail": "One or more fields are invalid.",
  "instance": "/v1/customers",
  "extensions": {
    "correlationId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "errorCode": "VALIDATION_FAILED",
    "validationErrors": {
      "cpf": ["CPF is invalid."]
    }
  }
}
```

---

## Authentication

Keycloak is the Identity Provider. The `realm-export.json` in `infra/local/keycloak/` is auto-imported on first startup with the `consignadohub` realm and the `consignadohub-api` client pre-configured.

### Realm roles

| Role | Access |
|---|---|
| `consignado-admin` | Full access — CRUD customers, manage proposals |
| `consignado-analyst` | Read customers, simulate and submit proposals |

### Acquiring a token (local development)

```bash
curl -s -X POST http://localhost:8080/realms/consignadohub/protocol/openid-connect/token \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=password" \
  -d "client_id=consignadohub-api" \
  -d "username=<user>" \
  -d "password=<password>" \
  | jq -r '.access_token'
```

Use the token as a Bearer header in subsequent requests:

```
Authorization: Bearer <token>
```

---

## Event Flow

All events are published to the **`consignadohub.events`** topic exchange.

```
ProposalService ──[proposal.submitted]──────────────▶ WorkflowWorker
                                                             │
                                               [workflow.credit.completed]
                                                             │
                   ┌─────────────────────────────────────────┘
                   │
                   ├──▶ ProposalService   (updates status → UnderAnalysis / Rejected)
                   └──▶ WorkflowWorker    (if approved → generates contract)
                              │
                    [workflow.contract.generated]
                              │
                   ┌──────────┘
                   ├──▶ ProposalService   (updates status → ContractGenerated)
                   └──▶ WorkflowWorker    (processes disbursement)
                              │
                    [workflow.disbursement.completed]
                              │
                   ┌──────────┘
                   └──▶ ProposalService   (updates status → Disbursed)

NotificationService subscribes to all events for fan-out side effects.
```

### Proposal lifecycle

```
Submitted → UnderAnalysis → Approved → ContractGenerated → Disbursed
                         ↘ Rejected
```

### Reliability guarantees

- **Outbox**: domain state and outbound event are persisted in the same SQL transaction; a hosted dispatcher publishes pending messages every 5 seconds.
- **Inbox**: each consumer records `(EventId, ConsumerName)` before processing; duplicate deliveries are acknowledged and discarded.
- **prefetchCount = 1**: consumers process one message at a time, providing natural concurrency control without optimistic locking.

---

## Running Tests

### Unit tests

```bash
dotnet test --filter "Category=Unit|FullyQualifiedName!~IntegrationTests"
```

### Integration tests

Integration tests use **Testcontainers** — no manual setup required; SQL Server and RabbitMQ containers are started automatically.

```bash
dotnet test --filter "Category=Integration|FullyQualifiedName~IntegrationTests"
```

### Coverage reports

```bash
make coverage-all        # merged HTML report for all services
make coverage-customer   # CustomerService only
make coverage-proposal   # ProposalService only
```

Reports are generated under `.coverage/html/index.html`.

---

## CI/CD

### Pull Request / Push CI (`.github/workflows/ci.yml`)

```
Build → Unit Tests → Integration Tests → SonarCloud Analysis
                                          CodeQL (weekly + push)
```

- NuGet packages cached per lockfile hash
- Test results published as GitHub checks via `dorny/test-reporter`
- Coverage uploaded to SonarCloud (requires `SONAR_TOKEN` secret)
- SonarCloud skipped for PRs from forks

### Release (`.github/workflows/release.yml`)

Triggered by a semver tag (`v*.*.*`):

```
Validate (build + unit tests)
    │
    ├── Docker: CustomerService    ──▶ ghcr.io/<owner>/consignadohub-customer:<tag>
    ├── Docker: ProposalService    ──▶ ghcr.io/<owner>/consignadohub-proposal:<tag>
    ├── Docker: WorkflowWorker     ──▶ ghcr.io/<owner>/consignadohub-workflow:<tag>
    └── Docker: NotificationService ──▶ ghcr.io/<owner>/consignadohub-notification:<tag>
            │
    GitHub Release (auto-generated notes + test artifacts)
```

All images are multi-platform (`linux/amd64`, `linux/arm64`) with SBOM and provenance attestation.

---

## Architecture Decisions

| ADR | Decision |
|---|---|
| [ADR-001](docs/architecture/adr/ADR-001-messaging.md) | RabbitMQ topic exchange with dedicated queues per consumer |
| [ADR-002](docs/architecture/adr/ADR-002-outbox.md) | Outbox Pattern + Inbox idempotency guard |
| [ADR-003](docs/architecture/adr/ADR-003-efcore-sqlserver.md) | EF Core + SQL Server with central migrations |
| [ADR-004](docs/architecture/adr/ADR-004-keycloak-auth.md) | Keycloak OIDC JWT Bearer + RBAC policies |
| [ADR-005](docs/architecture/adr/ADR-005-observability.md) | OpenTelemetry for traces, metrics, and logs |
| [ADR-006](docs/architecture/adr/ADR-006-api-versioning-problemdetails.md) | URL-based API versioning + RFC 7807 ProblemDetails |
| [ADR-007](docs/architecture/adr/ADR-007-notification-service.md) | NotificationService as fan-out side-effects consumer |
| [ADR-008](docs/architecture/adr/ADR-008-consumer-prefetch-concurrency.md) | prefetchCount=1 as the concurrency guard for consumers |
