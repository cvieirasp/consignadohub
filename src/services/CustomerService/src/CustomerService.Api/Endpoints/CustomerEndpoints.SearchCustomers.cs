using ConsignadoHub.BuildingBlocks.Http;
using CustomerService.Application.DTOs;
using CustomerService.Application.UseCases;
using Microsoft.AspNetCore.Mvc;

namespace CustomerService.Api.Endpoints;

public static partial class CustomerEndpoints
{
    private static async Task<IResult> SearchCustomers(
        SearchCustomersUseCase useCase,
        HttpContext ctx,
        CancellationToken ct,
        [FromQuery] string? name = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var input = new SearchCustomersInput(name, page, pageSize);
        var result = await useCase.ExecuteAsync(input, ct);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToHttpResult(ctx);
    }
}
