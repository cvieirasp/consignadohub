using Microsoft.AspNetCore.Http;

namespace ConsignadoHub.BuildingBlocks.Correlation;

/// <summary>
/// Middleware that ensures each HTTP request has a correlation ID, 
/// which is used for tracing and logging across distributed systems. 
/// It checks for an incoming correlation ID in the request headers and 
/// generates a new one if it's missing. The correlation ID is then added to 
/// the response headers and made available through the 
/// <see cref="ICorrelationIdProvider"/> for downstream components to use.
/// </summary>
/// <param name="next"></param>
public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    private const string HeaderName = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context, ICorrelationIdProvider correlationIdProvider)
    {
        var correlationId = context.Request.Headers[HeaderName].FirstOrDefault()
            ?? Guid.NewGuid().ToString();

        ((CorrelationIdProvider)correlationIdProvider).CorrelationId = correlationId;

        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        await next(context);
    }
}
