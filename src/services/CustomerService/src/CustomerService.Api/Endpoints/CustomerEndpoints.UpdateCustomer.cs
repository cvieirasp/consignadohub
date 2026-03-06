using ConsignadoHub.BuildingBlocks.Http;
using CustomerService.Application.DTOs;
using CustomerService.Application.UseCases;
using Microsoft.AspNetCore.Mvc;

namespace CustomerService.Api.Endpoints;

public static partial class CustomerEndpoints
{
    private static async Task<IResult> UpdateCustomer(
        Guid id,
        [FromBody] UpdateCustomerInput input,
        UpdateCustomerUseCase useCase,
        HttpContext ctx,
        CancellationToken ct)
    {
        var result = await useCase.ExecuteAsync(id, input, ct);
        return result.IsSuccess
            ? Results.NoContent()
            : result.Error.ToHttpResult(ctx);
    }
}
