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

#### Bearer Token UI in Scalar

Both APIs declare a `BearerAuth` HTTP Bearer security scheme in the OpenAPI document via an `IOpenApiDocumentTransformer` registered on `AddOpenApi()`. Scalar is configured to pre-select this scheme:

```csharp
// Register security scheme in OpenAPI document (Microsoft.OpenApi 2.0 namespace)
options.AddDocumentTransformer((document, _, _) =>
{
    document.Components ??= new OpenApiComponents();
    document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
    document.Components.SecuritySchemes["BearerAuth"] = new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "JWT token from Keycloak. ..."
    };
    return Task.CompletedTask;
});

// Configure Scalar to show and pre-select Bearer auth
app.MapScalarApiReference(options =>
{
    options
        .WithTitle("ConsignadoHub — <Service> API")
        .WithPreferredScheme("BearerAuth")
        .WithHttpBearerAuthentication(bearer => bearer.Token = string.Empty);
});
```

This allows developers to paste a Keycloak JWT directly in the Scalar UI and send authenticated requests without external tools. See ADR-004 for token acquisition details.

**Implementation note:** `Microsoft.AspNetCore.OpenApi` 10.x depends on `Microsoft.OpenApi` 2.0, which moved all model types to the root `Microsoft.OpenApi` namespace (previously `Microsoft.OpenApi.Models` in 1.x). `OpenApiComponents.SecuritySchemes` is `IDictionary<string, IOpenApiSecurityScheme>?` and must be null-checked before use.

## Consequences

- Clients can reliably parse errors by `errorCode` without inspecting `detail` text.
- Versioning allows breaking changes via `/v2/...` without impacting existing consumers.
- ProblemDetails middleware handles unhandled exceptions uniformly.
