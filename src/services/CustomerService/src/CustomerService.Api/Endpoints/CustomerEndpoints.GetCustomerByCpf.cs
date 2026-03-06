using ConsignadoHub.BuildingBlocks.Http;
using CustomerService.Application.UseCases;

namespace CustomerService.Api.Endpoints;

public static partial class CustomerEndpoints
{
    private static async Task<IResult> GetCustomerByCpf(
        string cpf,
        GetCustomerByCpfUseCase useCase,
        HttpContext ctx,
        CancellationToken ct)
    {
        var result = await useCase.ExecuteAsync(cpf, ct);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToHttpResult(ctx);
    }
}
