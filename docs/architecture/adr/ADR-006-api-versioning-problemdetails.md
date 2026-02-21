# ADR-006: API Versioning + ProblemDetails Error Contract

**Status:** Accepted
**Date:** 2026-02-20

## Context

APIs must be versioned from day one to support future non-breaking evolution. Error responses must be consistent and machine-readable across all endpoints.

## Decisions

### Versioning

- **URL-based versioning** (`/v1/...`) — simple, unambiguous, and cache-friendly.
- Library: `Asp.Versioning.Http` + `Asp.Versioning.ApiExplorer`.
- Default version: `1.0`. Unknown versions return 400.

### Error Contract

- All errors use **RFC 7807 ProblemDetails**, including validation errors.
- Extended with custom fields:
  - `errorCode` — machine-readable error code (e.g. `Customer.NotFound`)
  - `correlationId` — propagated from `X-Correlation-Id` header for observability
- HTTP status mapping by error code suffix:
  | Suffix | HTTP Status |
  |---|---|
  | `*NotFound` | 404 |
  | `*AlreadyExists`, `*Conflict` | 409 |
  | `*Invalid`, `*Validation` | 400 |
  | `*Forbidden` | 403 |
  | _(default)_ | 422 |

### API Documentation

- **OpenAPI** via `Microsoft.AspNetCore.OpenApi` (built-in .NET 10).
- **Scalar** as the interactive API reference UI at `/scalar/v1`.

## Consequences

- Clients can reliably parse errors by `errorCode` without inspecting `detail` text.
- Versioning allows breaking changes via `/v2/...` without impacting existing consumers.
- ProblemDetails middleware handles unhandled exceptions uniformly.
