using ConsignadoHub.BuildingBlocks.Http;
using ConsignadoHub.BuildingBlocks.Results;
using Microsoft.AspNetCore.Mvc;
using ProposalService.Application.DTOs;
using ProposalService.Application.UseCases;
using ProposalService.Domain.Enums;

namespace ProposalService.Api.Endpoints;

public static class ProposalEndpoints
{
    public static RouteGroupBuilder MapProposalEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/simulate", Simulate)
            .WithName("SimulateProposal")
            .WithSummary("Simulate a proposal (no DB write)")
            .AllowAnonymous()
            .Produces<SimulationResultDto>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapPost("/", SubmitProposal)
            .WithName("SubmitProposal")
            .WithSummary("Submit a proposal")
            .Produces<Guid>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapGet("/{id:guid}", GetProposalById)
            .WithName("GetProposalById")
            .WithSummary("Get proposal by ID")
            .Produces<ProposalDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/{id:guid}/timeline", GetTimeline)
            .WithName("GetProposalTimeline")
            .WithSummary("Get proposal timeline")
            .Produces<IReadOnlyList<ProposalTimelineEntryDto>>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/", ListByCustomer)
            .WithName("ListProposalsByCustomer")
            .WithSummary("List proposals by customer")
            .Produces<PagedResult<ProposalSummaryDto>>();

        return group;
    }

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

    private static async Task<IResult> GetTimeline(
        Guid id,
        GetProposalTimelineUseCase useCase,
        HttpContext ctx,
        CancellationToken ct)
    {
        var result = await useCase.ExecuteAsync(id, ct);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToHttpResult(ctx);
    }

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
