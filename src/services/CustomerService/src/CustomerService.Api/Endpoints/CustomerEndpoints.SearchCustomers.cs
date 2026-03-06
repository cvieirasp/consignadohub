using ConsignadoHub.BuildingBlocks.Http;
using CustomerService.Application.DTOs;
using CustomerService.Application.UseCases;
using Microsoft.AspNetCore.Mvc;

namespace CustomerService.Api.Endpoints;

public static partial class CustomerEndpoints
{
    private static async Task<IResult> SearchCustomers(
        [FromQuery] string? name,
        [FromQuery] int page,
        [FromQuery] int pageSize,
        SearchCustomersUseCase useCase,
        HttpContext ctx,
        CancellationToken ct)
    {
        var input = new SearchCustomersInput(name, page == 0 ? 1 : page, pageSize == 0 ? 20 : pageSize);
        var result = await useCase.ExecuteAsync(input, ct);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToHttpResult(ctx);
    }
}
