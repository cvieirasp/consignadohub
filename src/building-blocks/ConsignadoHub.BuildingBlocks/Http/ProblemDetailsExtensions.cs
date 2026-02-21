using ConsignadoHub.BuildingBlocks.Correlation;
using ConsignadoHub.BuildingBlocks.Results;
using Microsoft.AspNetCore.Http;

namespace ConsignadoHub.BuildingBlocks.Http;

public static class ProblemDetailsExtensions
{
    private static readonly Dictionary<string, int> _errorCodeStatusMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["NotFound"] = StatusCodes.Status404NotFound,
        ["AlreadyExists"] = StatusCodes.Status409Conflict,
        ["Conflict"] = StatusCodes.Status409Conflict,
        ["Invalid"] = StatusCodes.Status400BadRequest,
        ["Validation"] = StatusCodes.Status400BadRequest,
        ["Forbidden"] = StatusCodes.Status403Forbidden,
    };

    public static IResult ToHttpResult(this Error error, HttpContext ctx)
    {
        var status = ResolveStatus(error.Code);
        var correlationId = ctx.RequestServices
            .GetService(typeof(ICorrelationIdProvider)) is ICorrelationIdProvider provider
            ? provider.CorrelationId
            : null;

        var extensions = new Dictionary<string, object?>
        {
            ["errorCode"] = error.Code,
            ["correlationId"] = correlationId,
        };

        return TypedResults.Problem(
            detail: error.Message,
            statusCode: status,
            extensions: extensions);
    }

    private static int ResolveStatus(string errorCode)
    {
        foreach (var (suffix, status) in _errorCodeStatusMap)
        {
            if (errorCode.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                return status;
        }
        return StatusCodes.Status422UnprocessableEntity;
    }
}
