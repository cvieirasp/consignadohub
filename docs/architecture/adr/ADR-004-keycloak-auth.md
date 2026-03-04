# ADR-004: Keycloak OIDC Authentication and RBAC Authorization

**Status:** Accepted
**Date:** 28/02/2026

## Context
The ConsignadoHub APIs (CustomerService, ProposalService) require authentication and authorization to protect domain operations. The system needs:
- A standard, widely-adopted identity protocol (OIDC/OAuth2).
- Role-based access control (RBAC) for differentiating admin vs analyst access.
- JWT Bearer token validation in ASP.NET Core without maintaining user stores in each service.
- Local development support with minimal friction.

Options considered:
1. **ASP.NET Core Identity** - embeds user management in the service; unsuitable for microservices.
2. **Azure AD / Entra ID** - requires Azure subscription; not portable for local dev.
3. **Keycloak** - open-source IdP, self-hosted, supports OIDC, realm-based multi-tenancy, and Docker-friendly.

## Decision
Use **Keycloak** as the central Identity Provider (IdP) with **JWT Bearer** token validation in each API service.

### Realm / Client Configuration
- **Realm**: `consignadohub`
- **Client**: `consignadohub-api` (public client, direct access grants enabled for dev/testing)
- **Audience mapper**: adds `consignadohub-api` to the `aud` claim so ASP.NET Core JwtBearer validation succeeds
- **Realm roles**: `consignado-admin`, `consignado-analyst`

### Token Validation
Each API service validates JWT tokens using `Microsoft.AspNetCore.Authentication.JwtBearer` (must be added as an explicit NuGet package - it was removed from the `Microsoft.AspNetCore.App` shared framework in .NET 7+). The extension method `AddKeycloakAuthentication` in `ConsignadoHub.BuildingBlocks` reads the `Keycloak` configuration section and registers:
- `AddAuthentication(JwtBearerDefaults.AuthenticationScheme)`
- `AddJwtBearer` with `Authority`, `Audience`, and `RequireHttpsMetadata`

The `Authority` points to the Keycloak realm OIDC discovery endpoint (`/.well-known/openid-configuration`), which JwtBearer uses to fetch signing keys automatically.

### Authorization Policies
Two named policies are defined in `AddKeycloakAuthentication` via `services.AddAuthorization(...)`:

| Policy | Allowed Roles |
|---|---|
| `AdminOnly` | `consignado-admin` |
| `AnalystOrAdmin` | `consignado-admin`, `consignado-analyst` |

`IClaimsTransformation` (`KeycloakClaimsTransformation`) extracts realm roles from the Keycloak JWT `realm_access.roles` JSON claim and maps them to standard `ClaimTypes.Role` claims, enabling `RequireRole(...)` checks used internally by the policies.

### Endpoint RBAC Matrix

**CustomerService**

| Endpoint | Policy |
|---|---|
| `POST /v1/customers` | `AdminOnly` |
| `GET /v1/customers/{id}` | `AnalystOrAdmin` |
| `GET /v1/customers/cpf/{cpf}` | `AnalystOrAdmin` |
| `PUT /v1/customers/{id}` | `AdminOnly` |
| `DELETE /v1/customers/{id}` | `AdminOnly` |
| `GET /v1/customers` (search) | `AnalystOrAdmin` |

**ProposalService**

| Endpoint | Policy |
|---|---|
| `POST /v1/proposals/simulate` | `AllowAnonymous` |
| `POST /v1/proposals` | `AnalystOrAdmin` |
| `GET /v1/proposals/{id}` | `AnalystOrAdmin` |
| `GET /v1/proposals/{id}/timeline` | `AnalystOrAdmin` |
| `GET /v1/proposals` | `AnalystOrAdmin` |

### Endpoint Protection Strategy
- Each endpoint declares its policy explicitly via `.RequireAuthorization("PolicyName")`.
- `/v1/proposals/simulate` uses `.AllowAnonymous()` to allow unauthenticated access (public simulation tool).
- Health check endpoints (`/health/live`, `/health/ready`) are intentionally unauthenticated for infrastructure probes.

### Middleware Order
```
UseExceptionHandler()
UseStatusCodePages()
UseCorrelationId()
UseAuthentication() - must be before UseAuthorization
UseAuthorization()
UseSerilogRequestLogging()
```

### NotificationService / WorkflowWorker
These are worker services (no public HTTP endpoints) and do not require JWT validation. They communicate exclusively via RabbitMQ.

## Test Users (Local Dev)
| Username       | Password    | Role                |
|---------------|-------------|---------------------|
| admin-user    | admin123    | consignado-admin    |
| analyst-user  | analyst123  | consignado-analyst  |

### Option 1 - Scalar Bearer Token UI (recommended for dev)

Both APIs expose a Bearer Token input field directly in the Scalar UI (see ADR-006). Workflow:

1. Acquire a token via curl or Postman (see below).
2. Open `http://localhost:<port>/scalar/v1`.
3. Click **Authentication** → paste the `access_token` value.
4. All subsequent requests in Scalar will include `Authorization: Bearer <token>`.

### Option 2 - curl (scripting / CI)

```bash
curl -X POST http://localhost:8080/realms/consignadohub/protocol/openid-connect/token \
  -d 'grant_type=password&client_id=consignadohub-api&username=admin-user&password=admin123'
```

Extract `access_token` from the response and pass it as `Authorization: Bearer <token>`.

## Configuration
Each API service's `appsettings.json` includes:
```json
"Keycloak": {
  "Authority": "http://localhost:8080/realms/consignadohub",
  "Audience": "consignadohub-api",
  "RequireHttpsMetadata": false
}
```
Production deployments should set `RequireHttpsMetadata: true` and point `Authority` to the production Keycloak instance via environment variables.

## Consequences
### Positive
- Single source of truth for identity; no per-service user stores
- Standard OIDC/JWT approach - familiar to any ASP.NET Core developer
- Local dev realm auto-imported via Docker volume mount (`--import-realm`)
- Fine-grained RBAC implemented via named policies (`AdminOnly`, `AnalystOrAdmin`) applied per-endpoint

### Negative
- Adds Keycloak as a local dev dependency (mitigated by Docker Compose)
- Token acquisition requires an extra step in local dev (Postman or curl)
- Keycloak startup is slow (~60s); services should tolerate delayed Keycloak availability during startup

### Neutral
- `RequireHttpsMetadata: false` is intentional for local dev; enforce HTTPS in production via environment override
