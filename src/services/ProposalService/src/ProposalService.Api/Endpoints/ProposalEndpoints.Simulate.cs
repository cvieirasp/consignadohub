using ConsignadoHub.BuildingBlocks.Http;
using Microsoft.AspNetCore.Mvc;
using ProposalService.Application.DTOs;
using ProposalService.Application.UseCases;

namespace ProposalService.Api.Endpoints;

public static partial class ProposalEndpoints
{
    private static IResult Simulate(
        [FromBody] SimulateProposalInput input,
        SimulateProposalUseCase useCase,
        HttpContext ctx)
    {
        var result = useCase.Execute(input);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToHttpResult(ctx);
    }
}
