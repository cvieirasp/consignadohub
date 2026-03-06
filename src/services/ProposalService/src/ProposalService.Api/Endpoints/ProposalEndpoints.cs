using ConsignadoHub.BuildingBlocks.Auth;
using ConsignadoHub.BuildingBlocks.Results;
using ProposalService.Application.DTOs;
using ProposalService.Domain.Enums;

namespace ProposalService.Api.Endpoints;

public static partial class ProposalEndpoints
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
            .RequireAuthorization(Policies.AnalystOrAdmin)
            .Produces<Guid>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapGet("/{id:guid}", GetProposalById)
            .WithName("GetProposalById")
            .WithSummary("Get proposal by ID")
            .RequireAuthorization(Policies.AnalystOrAdmin)
            .Produces<ProposalDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/{id:guid}/timeline", GetTimeline)
            .WithName("GetProposalTimeline")
            .WithSummary("Get proposal timeline")
            .RequireAuthorization(Policies.AnalystOrAdmin)
            .Produces<IReadOnlyList<ProposalTimelineEntryDto>>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/", ListByCustomer)
            .WithName("ListProposalsByCustomer")
            .WithSummary("List proposals by customer")
            .RequireAuthorization(Policies.AnalystOrAdmin)
            .Produces<PagedResult<ProposalSummaryDto>>();

        return group;
    }
}
