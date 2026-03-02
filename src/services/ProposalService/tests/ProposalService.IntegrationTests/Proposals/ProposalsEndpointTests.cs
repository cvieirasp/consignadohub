using System.Net;
using System.Net.Http.Json;
using ConsignadoHub.BuildingBlocks.Auth;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ProposalService.Application.DTOs;
using ProposalService.IntegrationTests.Infrastructure;

namespace ProposalService.IntegrationTests.Proposals;

[Trait("Category", "Integration")]
public sealed class ProposalsEndpointTests(ProposalApiFactory factory)
    : IClassFixture<ProposalApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();
    private readonly HttpClient _analystClient = factory.CreateClientWithRole(Roles.Analyst);

    // --- Simulate (anonymous endpoint) ---

    [Fact]
    public async Task POST_Simulate_Returns200_WithCalculatedValues()
    {
        var input = new { RequestedAmount = 10_000m, TermMonths = 24 };

        var response = await _client.PostAsJsonAsync("/v1/proposals/simulate", input);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<SimulationResultDto>();
        result!.RequestedAmount.Should().Be(10_000m);
        result.TermMonths.Should().Be(24);
        result.InstallmentAmount.Should().BeGreaterThan(0);
        result.TotalAmount.Should().BeGreaterThan(result.RequestedAmount);
        result.CET.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task POST_Simulate_Returns422_WhenAmountBelowMinimum()
    {
        var input = new { RequestedAmount = 50m, TermMonths = 12 };

        var response = await _client.PostAsJsonAsync("/v1/proposals/simulate", input);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task POST_Simulate_Returns422_WhenTermMonthsBelowMinimum()
    {
        var input = new { RequestedAmount = 5_000m, TermMonths = 3 };

        var response = await _client.PostAsJsonAsync("/v1/proposals/simulate", input);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    // --- Submit (authenticated endpoint) ---

    [Fact]
    public async Task POST_SubmitProposal_Returns201_WithProposalId()
    {
        var input = new { CustomerId = Guid.NewGuid(), RequestedAmount = 15_000m, TermMonths = 36 };

        var response = await _client.PostAsJsonAsync("/v1/proposals", input);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var id = await response.Content.ReadFromJsonAsync<Guid>();
        id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task POST_SubmitProposal_CreatesOutboxMessageInDatabase()
    {
        var input = new { CustomerId = Guid.NewGuid(), RequestedAmount = 20_000m, TermMonths = 48 };

        var response = await _client.PostAsJsonAsync("/v1/proposals", input);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var proposalId = await response.Content.ReadFromJsonAsync<Guid>();

        // Assert Outbox entry was persisted in the same transaction
        using var db = factory.CreateDbContext();
        var outboxExists = await db.OutboxMessages
            .AnyAsync(m => m.RoutingKey == "proposal.submitted" && m.Payload.Contains(proposalId.ToString()));

        outboxExists.Should().BeTrue("SubmitProposalUseCase must write an OutboxMessage atomically with the proposal");
    }

    // --- Get (authenticated endpoint) ---

    [Fact]
    public async Task GET_GetProposalById_Returns200_WithCorrectData()
    {
        var customerId = Guid.NewGuid();
        var submitInput = new { CustomerId = customerId, RequestedAmount = 8_000m, TermMonths = 12 };
        var submitResponse = await _client.PostAsJsonAsync("/v1/proposals", submitInput);
        var proposalId = await submitResponse.Content.ReadFromJsonAsync<Guid>();

        var response = await _client.GetAsync($"/v1/proposals/{proposalId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<ProposalDto>();
        dto!.Id.Should().Be(proposalId);
        dto.CustomerId.Should().Be(customerId);
        dto.RequestedAmount.Should().Be(8_000m);
        dto.TermMonths.Should().Be(12);
    }

    [Fact]
    public async Task GET_GetProposalById_Returns404_WhenProposalDoesNotExist()
    {
        var response = await _client.GetAsync($"/v1/proposals/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GET_GetProposalTimeline_Returns200_WithInitialEntry()
    {
        var submitInput = new { CustomerId = Guid.NewGuid(), RequestedAmount = 12_000m, TermMonths = 24 };
        var submitResponse = await _client.PostAsJsonAsync("/v1/proposals", submitInput);
        var proposalId = await submitResponse.Content.ReadFromJsonAsync<Guid>();

        var response = await _client.GetAsync($"/v1/proposals/{proposalId}/timeline");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var timeline = await response.Content.ReadFromJsonAsync<IReadOnlyList<ProposalTimelineEntryDto>>();
        timeline!.Should().NotBeEmpty();
        timeline.Should().ContainSingle(e => e.FromStatus == null);
    }

    // --- RBAC: analyst can access AnalystOrAdmin endpoints ---

    [Fact]
    public async Task POST_Simulate_Returns200_ForAnalyst()
    {
        var input = new { RequestedAmount = 5_000m, TermMonths = 12 };

        var response = await _analystClient.PostAsJsonAsync("/v1/proposals/simulate", input);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task POST_SubmitProposal_Returns201_ForAnalyst()
    {
        var input = new { CustomerId = Guid.NewGuid(), RequestedAmount = 10_000m, TermMonths = 24 };

        var response = await _analystClient.PostAsJsonAsync("/v1/proposals", input);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task GET_GetProposalById_Returns404_ForAnalyst_WhenNotFound()
    {
        // Analysts can access the endpoint; 404 confirms authorization passed and route was reached
        var response = await _analystClient.GetAsync($"/v1/proposals/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
