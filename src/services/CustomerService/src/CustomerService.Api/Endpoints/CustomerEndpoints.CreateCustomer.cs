using ConsignadoHub.BuildingBlocks.Http;
using CustomerService.Application.DTOs;
using CustomerService.Application.UseCases;
using Microsoft.AspNetCore.Mvc;

namespace CustomerService.Api.Endpoints;

public static partial class CustomerEndpoints
{
    private static async Task<IResult> CreateCustomer(
        [FromBody] CreateCustomerInput input,
        CreateCustomerUseCase useCase,
        HttpContext ctx,
        CancellationToken ct)
    {
        var result = await useCase.ExecuteAsync(input, ct);
        return result.IsSuccess
            ? Results.Created($"/v1/customers/{result.Value}", result.Value)
            : result.Error.ToHttpResult(ctx);
    }
}
