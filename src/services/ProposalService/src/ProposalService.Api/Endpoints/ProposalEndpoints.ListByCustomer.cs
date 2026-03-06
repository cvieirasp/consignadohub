using ConsignadoHub.BuildingBlocks.Http;
using Microsoft.AspNetCore.Mvc;
using ProposalService.Application.DTOs;
using ProposalService.Application.UseCases;
using ProposalService.Domain.Enums;

namespace ProposalService.Api.Endpoints;

public static partial class ProposalEndpoints
{
    private static async Task<IResult> ListByCustomer(
        [FromQuery] Guid customerId,
        [FromQuery] ProposalStatus? status,
        [FromQuery] int page,
        [FromQuery] int pageSize,
        ListProposalsByCustomerUseCase useCase,
        HttpContext ctx,
        CancellationToken ct)
    {
        var input = new ListProposalsByCustomerInput(
            customerId,
            status,
            page == 0 ? 1 : page,
            pageSize == 0 ? 20 : pageSize);

        var result = await useCase.ExecuteAsync(input, ct);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToHttpResult(ctx);
    }
}
