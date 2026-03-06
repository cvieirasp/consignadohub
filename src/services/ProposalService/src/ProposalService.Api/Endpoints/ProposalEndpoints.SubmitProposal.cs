using ConsignadoHub.BuildingBlocks.Http;
using Microsoft.AspNetCore.Mvc;
using ProposalService.Application.DTOs;
using ProposalService.Application.UseCases;

namespace ProposalService.Api.Endpoints;

public static partial class ProposalEndpoints
{
    private static async Task<IResult> SubmitProposal(
        [FromBody] SubmitProposalInput input,
        SubmitProposalUseCase useCase,
        HttpContext ctx,
        CancellationToken ct)
    {
        var result = await useCase.ExecuteAsync(input, ct);
        return result.IsSuccess
            ? Results.Created($"/v1/proposals/{result.Value}", result.Value)
            : result.Error.ToHttpResult(ctx);
    }
}
