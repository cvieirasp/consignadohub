# ADR-005: Observability with OpenTelemetry

**Status:** Accepted
**Date:** 01/03/2026

## Context

The system spans four services communicating over RabbitMQ. Without distributed tracing, diagnosing latency, failed events, or broken workflows requires correlating logs across services manually. We need a unified observability strategy covering:
- Distributed traces (request → consumer → handler chain)
- Runtime metrics (request latency, error rates, queue processing)
- Structured logs (already addressed via Serilog + CorrelationId middleware)

## Decision

Adopt **OpenTelemetry (OTel) .NET SDK 1.15.0** as the single observability framework across all four services.

### Instrumentation

| Signal | Source | Package |
|---|---|---|
| Traces | ASP.NET Core HTTP pipeline | `OpenTelemetry.Instrumentation.AspNetCore` |
| Traces | Outbound HttpClient calls | `OpenTelemetry.Instrumentation.Http` |
| Traces | SQL Server via EF Core (ADO.NET) | `OpenTelemetry.Instrumentation.SqlClient` |
| Traces | RabbitMQ publish / consume | Custom `ActivitySource` (`ConsignadoHub.Messaging`) |
| Metrics | HTTP request duration / error rate | `OpenTelemetry.Instrumentation.AspNetCore` |
| Metrics | .NET runtime (GC, thread pool, allocations) | `OpenTelemetry.Instrumentation.Runtime` |

### Custom RabbitMQ Spans

The RabbitMQ .NET client does not include native OTel instrumentation. Manual spans are created in the building blocks:

- **`RabbitMqEventPublisher.PublishRawAsync`** - `ActivityKind.Producer` span with tags:
  `messaging.system=rabbitmq`, `messaging.destination`, `messaging.rabbitmq.routing_key`
- **`RabbitMqConsumerBase`** - `ActivityKind.Consumer` span per message with tags:
  `messaging.system=rabbitmq`, `messaging.destination`, `messaging.consumer`, `messaging.message_id`, `messaging.correlation_id`

Error status (`ActivityStatusCode.Error`) is set on the span when message processing fails.

### Exporters

| Exporter | When active |
|---|---|
| Console | `Development` environment only |
| OTLP (gRPC) | When `OpenTelemetry:OtlpEndpoint` is set in configuration |

This allows local development with console output and production/staging pointing to an OTel Collector (Jaeger, Grafana Tempo, etc.).

### Shared Extension Method

A single `AddConsignadoHubObservability(IConfiguration, IHostEnvironment, string serviceName)` extension in `ConsignadoHub.BuildingBlocks` is called from each service's `Program.cs`. This ensures consistent instrumentation without duplication.

The `serviceName` parameter sets the `service.name` resource attribute, making traces filterable per service in any OTel-compatible backend.

### W3C Trace Context Propagation

The OTel SDK defaults to W3C `traceparent`/`tracestate` header propagation. For RabbitMQ messages, trace context is not automatically propagated through AMQP headers in this implementation - the `CorrelationId` carried in each `IIntegrationEvent` serves as the cross-service correlation identifier, visible in both logs and span attributes.

## Consequences

- **All services** gain distributed tracing for HTTP, SQL, and RabbitMQ operations with zero application-layer changes.
- **Console exporter** provides immediate local feedback without an OTel Collector.
- **OTLP support** allows plugging in Jaeger, Grafana Tempo, or any OTel-compatible backend by setting one config value.
- **Custom RabbitMQ spans** are linked to the consumer's processing scope, making queue throughput and failures visible in traces.
- Adding an OTel Collector + backend to `docker-compose.yml` (e.g. Jaeger all-in-one) is a straightforward future step.
- The `ConsignadoHub.BuildingBlocks` project carries the OTel package dependencies, keeping service projects free of direct OTel references.
