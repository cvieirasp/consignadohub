using ConsignadoHub.BuildingBlocks.Http;
using CustomerService.Application.UseCases;

namespace CustomerService.Api.Endpoints;

public static partial class CustomerEndpoints
{
    private static async Task<IResult> GetCustomerById(
        Guid id,
        GetCustomerByIdUseCase useCase,
        HttpContext ctx,
        CancellationToken ct)
    {
        var result = await useCase.ExecuteAsync(id, ct);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToHttpResult(ctx);
    }
}
