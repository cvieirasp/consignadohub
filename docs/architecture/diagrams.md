# Architecture Diagrams

All diagrams use [Mermaid](https://mermaid.js.org/) and render natively on GitHub.

---

## 1. System Context

Who uses the system and what external dependencies exist.

![Texto_alternativo_imagem](images/ConsignadoHub%20-%20System%20Context.png)

## 2. Container Diagram

All services, their databases, and messaging relationships.

```mermaid
graph TB
    subgraph Clients["Clients"]
        Admin(["👤 Admin"])
        Analyst(["👤 Analyst"])
    end

    subgraph APIs["API Services (.NET 10 · ASP.NET Core)"]
        CS["<b>CustomerService</b><br/>:5000<br/>─────────────<br/>POST /v1/customers<br/>GET  /v1/customers/{id}<br/>GET  /v1/customers/cpf/{cpf}<br/>PUT  /v1/customers/{id}<br/>DELETE /v1/customers/{id}<br/>GET  /v1/customers?name="]
        PS["<b>ProposalService</b><br/>:5001<br/>─────────────<br/>POST /v1/proposals/simulate<br/>POST /v1/proposals<br/>GET  /v1/proposals/{id}<br/>GET  /v1/proposals/{id}/timeline<br/>GET  /v1/proposals?customerId="]
    end

    subgraph Workers["Background Workers (.NET 10 · Worker)"]
        WW["<b>WorkflowWorker</b><br/>─────────────<br/>Consumers:<br/>• proposal.submitted<br/>• workflow.credit.completed<br/>• workflow.contract.generated"]
        NS["<b>NotificationService</b><br/>─────────────<br/>Consumers:<br/>• notification.proposal.submitted<br/>• notification.credit.completed<br/>• notification.contract.generated<br/>• notification.disbursement.completed"]
    end

    subgraph DBs["SQL Server 2025 (port 1444)"]
        DB_C[("ConsignadoHub<br/>_Customers")]
        DB_P[("ConsignadoHub<br/>_Proposals<br/>+ Outbox + Inbox")]
        DB_W[("ConsignadoHub<br/>_Workflow<br/>+ Inbox")]
        DB_N[("ConsignadoHub<br/>_Notifications<br/>+ Inbox")]
    end

    subgraph Infra["Infrastructure"]
        KC["<b>Keycloak 26.2</b><br/>:8080<br/>Realm: consignadohub<br/>Roles: consignado-admin<br/>consignado-analyst"]
        RMQ["<b>RabbitMQ 4</b><br/>:5672 · :15672<br/>Exchange: consignadohub.events<br/>Type: topic"]
    end

    Admin -->|"HTTPS · JWT"| CS
    Admin -->|"HTTPS · JWT"| PS
    Analyst -->|"HTTPS · JWT"| PS
    Analyst -->|"HTTPS · JWT"| CS

    CS -->|"validates JWT"| KC
    PS -->|"validates JWT"| KC

    CS --- DB_C
    PS --- DB_P
    WW --- DB_W
    NS --- DB_N

    PS -->|"Outbox → publish<br/>proposal.submitted"| RMQ
    PS -->|"consume<br/>proposal.credit.completed<br/>contract.generated<br/>disbursement.completed"| RMQ

    WW -->|"consume + publish<br/>workflow events"| RMQ
    NS -->|"consume<br/>notification events"| RMQ

    style CS fill:#dbeafe,stroke:#3b82f6
    style PS fill:#dbeafe,stroke:#3b82f6
    style WW fill:#dcfce7,stroke:#16a34a
    style NS fill:#dcfce7,stroke:#16a34a
    style KC fill:#fef9c3,stroke:#ca8a04
    style RMQ fill:#fef9c3,stroke:#ca8a04
    style DB_C fill:#f3e8ff,stroke:#9333ea
    style DB_P fill:#f3e8ff,stroke:#9333ea
    style DB_W fill:#f3e8ff,stroke:#9333ea
    style DB_N fill:#f3e8ff,stroke:#9333ea
```

---

## 3. Clean Architecture — Per-Service Layers

Each service follows the same layered structure. ProposalService is shown as the most complete example.

```mermaid
graph TB
    subgraph ProposalService["ProposalService"]
        direction TB

        subgraph Api["Api Layer"]
            EP["Endpoints<br/>(Minimal API)"]
            MW["Middleware<br/>(CorrelationId, Auth)"]
            CON["Consumers<br/>(HostedService)"]
            OUTD["OutboxDispatcher<br/>(HostedService)"]
        end

        subgraph App["Application Layer"]
            UC["Use Cases<br/>(Scoped)"]
            DTO["DTOs / Inputs"]
            PORTS["Ports<br/>(IProposalRepository<br/>IOutboxRepository<br/>IInboxRepository)"]
        end

        subgraph Dom["Domain Layer"]
            ENT["Entities<br/>(Proposal, ProposalTimelineEntry)"]
            VO["Value Objects<br/>(Money, Status)"]
            ERR["Domain Errors"]
        end

        subgraph Infra["Infrastructure Layer"]
            REPO["Repositories<br/>(EF Core)"]
            CFG["EF Configurations"]
            MIG["Migrations"]
            OUTB["OutboxMessage<br/>InboxMessage"]
        end

        subgraph BB["BuildingBlocks"]
            RES["Result&lt;T&gt; / Error"]
            RMQP["RabbitMqPublisher"]
            RMQB["RabbitMqConsumerBase"]
            OTel["OpenTelemetry"]
            AUTH["Keycloak Auth<br/>ClaimsTransformation"]
        end

        EP --> UC
        CON --> UC
        OUTD --> RMQP
        UC --> PORTS
        UC --> ENT
        PORTS --> REPO
        REPO --> OUTB
        UC --> RES
        EP --> RES
        CON --> RMQB
        EP --> AUTH
    end

    style Api fill:#dbeafe,stroke:#3b82f6
    style App fill:#dcfce7,stroke:#16a34a
    style Dom fill:#fef9c3,stroke:#ca8a04
    style Infra fill:#f3e8ff,stroke:#9333ea
    style BB fill:#fee2e2,stroke:#dc2626
```

---

## 4. Event-Driven Workflow — Sequence Diagram

The complete proposal pipeline from HTTP submit to disbursement.

```mermaid
sequenceDiagram
    actor Client
    participant PS  as ProposalService
    participant DB_P as ProposalDB
    participant OD  as OutboxDispatcher
    participant RMQ as RabbitMQ
    participant WW  as WorkflowWorker
    participant NS  as NotificationService

    Client->>+PS: POST /v1/proposals {customerId, amount, termMonths}
    PS->>+DB_P: BEGIN TX<br/>INSERT Proposals<br/>INSERT OutboxMessages (proposal.submitted)
    DB_P-->>-PS: COMMIT
    PS-->>-Client: 201 Created {proposalId}

    loop every 5s
        OD->>+DB_P: SELECT pending OutboxMessages
        DB_P-->>-OD: [ProposalSubmittedEvent]
        OD->>RMQ: publish → routing key: proposal.submitted
        OD->>DB_P: UPDATE OutboxMessages SET ProcessedAt=now
    end

    par WorkflowWorker consumer
        RMQ->>+WW: deliver (queue: proposal.submitted)
        WW->>WW: credit scoring algorithm
        WW->>RMQ: publish → routing key: proposal.credit.completed
        WW->>DB_W: INSERT InboxMessages (idempotency)
        WW-->>-RMQ: ACK
    and NotificationService fan-out
        RMQ->>+NS: deliver (queue: notification.proposal.submitted)
        NS->>NS: SendProposalSubmittedAsync (stub)
        NS->>DB_N: INSERT InboxMessages
        NS-->>-RMQ: ACK
    end

    par ProposalService status update
        RMQ->>+PS: deliver (queue: proposal.credit.completed)
        PS->>DB_P: UPDATE Proposals SET Status=Approved|Rejected<br/>INSERT InboxMessages + Timeline entry
        PS-->>-RMQ: ACK
    and WorkflowWorker (approved path)
        RMQ->>+WW: deliver (queue: workflow.credit.completed)
        alt Approved
            WW->>WW: generate contract (stub)
            WW->>RMQ: publish → routing key: contract.generated
        else Rejected
            WW->>WW: log rejection, skip
        end
        WW->>DB_W: INSERT InboxMessages
        WW-->>-RMQ: ACK
    and NotificationService fan-out
        RMQ->>+NS: deliver (queue: notification.credit.completed)
        NS->>NS: SendCreditAnalysisCompletedAsync (stub)
        NS->>DB_N: INSERT InboxMessages
        NS-->>-RMQ: ACK
    end

    par ProposalService status update
        RMQ->>+PS: deliver (queue: proposal.contract.generated)
        PS->>DB_P: UPDATE Proposals SET Status=ContractGenerated<br/>INSERT InboxMessages + Timeline entry
        PS-->>-RMQ: ACK
    and WorkflowWorker
        RMQ->>+WW: deliver (queue: workflow.contract.generated)
        WW->>WW: process disbursement (stub)
        WW->>RMQ: publish → routing key: disbursement.completed
        WW->>DB_W: INSERT InboxMessages
        WW-->>-RMQ: ACK
    and NotificationService fan-out
        RMQ->>+NS: deliver (queue: notification.contract.generated)
        NS->>NS: SendContractGeneratedAsync (stub)
        NS->>DB_N: INSERT InboxMessages
        NS-->>-RMQ: ACK
    end

    par ProposalService status update (terminal)
        RMQ->>+PS: deliver (queue: proposal.disbursement.completed)
        PS->>DB_P: UPDATE Proposals SET Status=Disbursed<br/>INSERT InboxMessages + Timeline entry
        PS-->>-RMQ: ACK
    and NotificationService fan-out
        RMQ->>+NS: deliver (queue: notification.disbursement.completed)
        NS->>NS: SendDisbursementCompletedAsync (stub)
        NS->>DB_N: INSERT InboxMessages
        NS-->>-RMQ: ACK
    end
```

---

## 5. Messaging Topology — Exchange Bindings

All queues bound to the `consignadohub.events` topic exchange.

```mermaid
graph LR
    subgraph Exchange["Exchange: consignadohub.events (topic)"]
    end

    subgraph Publishers
        PS_OUT["ProposalService<br/>(Outbox)"]
        WW_PUB["WorkflowWorker<br/>(direct publish)"]
    end

    subgraph QueueGroup_PS["ProposalService queues"]
        Q1["proposal.credit.completed"]
        Q2["proposal.contract.generated"]
        Q3["proposal.disbursement.completed"]
    end

    subgraph QueueGroup_WW["WorkflowWorker queues"]
        Q4["proposal.submitted"]
        Q5["workflow.credit.completed"]
        Q6["workflow.contract.generated"]
    end

    subgraph QueueGroup_NS["NotificationService queues"]
        Q7["notification.proposal.submitted"]
        Q8["notification.credit.completed"]
        Q9["notification.contract.generated"]
        Q10["notification.disbursement.completed"]
    end

    PS_OUT -->|"proposal.submitted"| Exchange
    WW_PUB -->|"proposal.credit.completed"| Exchange
    WW_PUB -->|"contract.generated"| Exchange
    WW_PUB -->|"disbursement.completed"| Exchange

    Exchange -->|"proposal.submitted"| Q4
    Exchange -->|"proposal.submitted"| Q7

    Exchange -->|"proposal.credit.completed"| Q1
    Exchange -->|"proposal.credit.completed"| Q5
    Exchange -->|"proposal.credit.completed"| Q8

    Exchange -->|"contract.generated"| Q2
    Exchange -->|"contract.generated"| Q6
    Exchange -->|"contract.generated"| Q9

    Exchange -->|"disbursement.completed"| Q3
    Exchange -->|"disbursement.completed"| Q10

    Q1 --> PS_CONS["ProposalService<br/>CreditAnalysisCompletedConsumer"]
    Q2 --> PS_CONS2["ProposalService<br/>ContractGeneratedConsumer"]
    Q3 --> PS_CONS3["ProposalService<br/>DisbursementCompletedConsumer"]

    Q4 --> WW_CONS["WorkflowWorker<br/>ProposalSubmittedConsumer"]
    Q5 --> WW_CONS2["WorkflowWorker<br/>CreditAnalysisCompletedConsumer"]
    Q6 --> WW_CONS3["WorkflowWorker<br/>ContractGeneratedConsumer"]

    Q7 --> NS_CONS["NotificationService<br/>ProposalSubmittedNotificationConsumer"]
    Q8 --> NS_CONS2["NotificationService<br/>CreditAnalysisCompletedNotificationConsumer"]
    Q9 --> NS_CONS3["NotificationService<br/>ContractGeneratedNotificationConsumer"]
    Q10 --> NS_CONS4["NotificationService<br/>DisbursementCompletedNotificationConsumer"]

    style Exchange fill:#fef9c3,stroke:#ca8a04
    style QueueGroup_PS fill:#dbeafe,stroke:#3b82f6
    style QueueGroup_WW fill:#dcfce7,stroke:#16a34a
    style QueueGroup_NS fill:#f3e8ff,stroke:#9333ea
```

---

## 6. Database Schema

One database per service. All use EF Core with SQL Server.

```mermaid
erDiagram
    %% ── ConsignadoHub_Customers ──────────────────────────────
    Customers {
        uniqueidentifier Id PK
        nvarchar FullName
        nvarchar Cpf UK
        nvarchar Email
        nvarchar Phone
        bit IsActive
        datetimeoffset CreatedAt
        datetimeoffset UpdatedAt
    }

    %% ── ConsignadoHub_Proposals ──────────────────────────────
    Proposals {
        uniqueidentifier Id PK
        uniqueidentifier CustomerId
        decimal RequestedAmount
        decimal MonthlyRate
        int TermMonths
        decimal MonthlyInstallment
        decimal TotalCet
        nvarchar Status
        datetimeoffset CreatedAt
        datetimeoffset UpdatedAt
    }

    ProposalTimeline {
        uniqueidentifier Id PK
        uniqueidentifier ProposalId FK
        nvarchar Status
        nvarchar Notes
        datetimeoffset OccurredAt
    }

    OutboxMessages_P {
        uniqueidentifier Id PK
        nvarchar Type
        nvarchar Payload
        nvarchar RoutingKey
        datetimeoffset CreatedAt
        datetimeoffset ProcessedAt
        int Attempt
        nvarchar LastError
    }

    InboxMessages_P {
        uniqueidentifier EventId PK
        nvarchar ConsumerName PK
        datetimeoffset ProcessedAt
    }

    Proposals ||--o{ ProposalTimeline : "has"
    Proposals ||--o{ OutboxMessages_P : "emits"
    Proposals ||--o{ InboxMessages_P : "guards"

    %% ── ConsignadoHub_Workflow ───────────────────────────────
    InboxMessages_W {
        uniqueidentifier EventId PK
        nvarchar ConsumerName PK
        datetimeoffset ProcessedAt
    }

    %% ── ConsignadoHub_Notifications ─────────────────────────
    InboxMessages_N {
        uniqueidentifier EventId PK
        nvarchar ConsumerName PK
        datetimeoffset ProcessedAt
    }
```

---

## 7. Deployment — Local Docker Compose

```mermaid
graph TB
    subgraph DockerNetwork["Docker network: consignadohub (bridge)"]
        subgraph Infrastructure
            SQL["SQL Server 2025<br/>container: consignadohub-sqlserver<br/>host port: 1444 → 1433<br/>volume: sqlserver-data"]
            RMQ["RabbitMQ 4<br/>container: consignadohub-rabbitmq<br/>host port: 5672 + 15672<br/>volume: rabbitmq-data"]
            KC["Keycloak 26.2<br/>container: consignadohub-keycloak<br/>host port: 8080<br/>realm import: realm-export.json<br/>volume: keycloak-data"]
        end

        subgraph Services["Services (run locally or as containers)"]
            SVC_C["CustomerService<br/>:5000"]
            SVC_P["ProposalService<br/>:5001"]
            SVC_W["WorkflowWorker"]
            SVC_N["NotificationService"]
        end
    end

    Developer(["💻 Developer / Postman"])

    Developer -->|"HTTP :5000"| SVC_C
    Developer -->|"HTTP :5001"| SVC_P
    Developer -->|"HTTP :8080<br/>(token endpoint)"| KC

    SVC_C -->|":1433"| SQL
    SVC_P -->|":1433"| SQL
    SVC_W -->|":1433"| SQL
    SVC_N -->|":1433"| SQL

    SVC_C -->|"OIDC"| KC
    SVC_P -->|"OIDC"| KC

    SVC_P -->|":5672"| RMQ
    SVC_W -->|":5672"| RMQ
    SVC_N -->|":5672"| RMQ

    Developer -->|"Management UI :15672"| RMQ

    style Infrastructure fill:#fef9c3,stroke:#ca8a04
    style Services fill:#dbeafe,stroke:#3b82f6
```

---

## 8. Authentication & Authorization Flow

```mermaid
sequenceDiagram
    actor User
    participant KC  as Keycloak :8080
    participant API as CustomerService / ProposalService
    participant MW  as Auth Middleware
    participant EP  as Endpoint

    User->>+KC: POST /realms/consignadohub/protocol/openid-connect/token<br/>{client_id, username, password}
    KC-->>-User: {access_token (JWT), expires_in}

    User->>+API: HTTP request<br/>Authorization: Bearer {access_token}
    API->>+MW: UseAuthentication()
    MW->>MW: JwtBearerHandler validates signature<br/>issuer, audience, expiry
    MW->>MW: KeycloakClaimsTransformation<br/>parses realm_access.roles<br/>→ ClaimTypes.Role claims
    MW-->>-API: ClaimsPrincipal populated

    API->>+EP: UseAuthorization()
    EP->>EP: policy check<br/>AdminOnly → requires consignado-admin<br/>AnalystOrAdmin → requires either role
    alt authorized
        EP-->>User: 200 / 201 / 204
    else unauthorized
        EP-->>User: 401 Unauthorized
    else forbidden
        EP-->>User: 403 Forbidden
    end
```

---

## 9. Outbox Pattern — Reliability Detail

```mermaid
sequenceDiagram
    participant UC  as Use Case
    participant DB  as ProposalDB (SQL Server)
    participant OD  as OutboxDispatcher (HostedService)
    participant RMQ as RabbitMQ

    Note over UC,DB: Same SaveChanges transaction
    UC->>+DB: BEGIN TX
    UC->>DB: INSERT Proposals (domain state)
    UC->>DB: INSERT OutboxMessages {type, payload, routingKey}
    DB-->>-UC: COMMIT (atomic)

    loop every 5 seconds
        OD->>+DB: SELECT TOP N WHERE ProcessedAt IS NULL ORDER BY CreatedAt
        DB-->>-OD: [pending messages]

        loop for each message
            OD->>+RMQ: BasicPublish(exchange, routingKey, payload)
            RMQ-->>-OD: broker ACK
            OD->>DB: UPDATE OutboxMessages SET ProcessedAt=now, Attempt+=1
        end
    end

    Note over OD,RMQ: If publish fails, ProcessedAt stays null<br/>→ retried next poll cycle (at-least-once)
    Note over DB: Consumer-side Inbox prevents duplicate processing
```
