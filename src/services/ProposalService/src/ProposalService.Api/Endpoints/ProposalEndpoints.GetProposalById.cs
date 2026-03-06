using ConsignadoHub.BuildingBlocks.Http;
using ProposalService.Application.UseCases;

namespace ProposalService.Api.Endpoints;

public static partial class ProposalEndpoints
{
    private static async Task<IResult> GetProposalById(
        Guid id,
        GetProposalByIdUseCase useCase,
        HttpContext ctx,
        CancellationToken ct)
    {
        var result = await useCase.ExecuteAsync(id, ct);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToHttpResult(ctx);
    }
}
