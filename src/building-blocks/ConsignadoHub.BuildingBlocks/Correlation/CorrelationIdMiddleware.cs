using Microsoft.AspNetCore.Http;

namespace ConsignadoHub.BuildingBlocks.Correlation;

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
