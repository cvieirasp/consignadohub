# ADR-003: EF Core + SQL Server

**Status:** Accepted
**Date:** 20/02/2026

## Context

We need a persistence strategy for each bounded context (CustomerService, ProposalService). The options considered were Dapper, EF Core, and raw ADO.NET.

## Decision

Use **EF Core 9** with **SQL Server** (Azure SQL compatible) for all services.

- Each service has its **own database** (database-per-service) to ensure bounded context isolation.
- **Code-first migrations** managed per service under `Infrastructure/Persistence/Migrations/`.
- **`AsNoTracking()`** applied to all read-only queries.
- **`AddDbContextPool`** used for improved performance under load.
- **Fluent API** configurations in `IEntityTypeConfiguration<T>` classes - no data annotations on domain entities.
- **Indices** added for all query-heavy columns: CPF (unique), FullName, CustomerId, Status.

## Consequences

- Simple and productive ORM with strong .NET ecosystem support.
- Owned migration history makes rollback and forward-only migrations straightforward.
- EF Core adds a thin overhead vs. raw SQL; acceptable given the domain complexity.
- Domain entities are kept clean - no EF Core attributes pollute the domain model.
