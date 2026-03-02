using FluentAssertions;
using ProposalService.Domain.Entities;
using ProposalService.Domain.Enums;

namespace ProposalService.UnitTests.Domain;

public sealed class ProposalUpdateStatusTests
{
    private static Proposal CreateSubmittedProposal() =>
        Proposal.Create(Guid.NewGuid(), 10_000m, 12, 1.8m);

    [Fact]
    [Trait("Category", "Unit")]
    public void UpdateStatus_ShouldTransitionToApproved_FromSubmitted()
    {
        var proposal = CreateSubmittedProposal();

        var result = proposal.UpdateStatus(ProposalStatus.Approved, "Score: 750");

        result.IsSuccess.Should().BeTrue();
        proposal.Status.Should().Be(ProposalStatus.Approved);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void UpdateStatus_ShouldTransitionToRejected_FromSubmitted()
    {
        var proposal = CreateSubmittedProposal();

        var result = proposal.UpdateStatus(ProposalStatus.Rejected, "Score: 450");

        result.IsSuccess.Should().BeTrue();
        proposal.Status.Should().Be(ProposalStatus.Rejected);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void UpdateStatus_ShouldAddTimelineEntry_OnSuccess()
    {
        var proposal = CreateSubmittedProposal();

        proposal.UpdateStatus(ProposalStatus.Approved, "Approved");

        proposal.Timeline.Should().ContainSingle(t =>
            t.ToStatus == ProposalStatus.Approved &&
            t.FromStatus == ProposalStatus.Submitted);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void UpdateStatus_ShouldUpdateUpdatedAt_OnSuccess()
    {
        var proposal = CreateSubmittedProposal();
        var before = proposal.UpdatedAt;

        proposal.UpdateStatus(ProposalStatus.Approved);

        proposal.UpdatedAt.Should().BeOnOrAfter(before);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void UpdateStatus_ShouldSucceed_WhenApprovedToContractGenerated()
    {
        var proposal = CreateSubmittedProposal();
        proposal.UpdateStatus(ProposalStatus.Approved);

        var result = proposal.UpdateStatus(ProposalStatus.ContractGenerated, "Contract generated.");

        result.IsSuccess.Should().BeTrue();
        proposal.Status.Should().Be(ProposalStatus.ContractGenerated);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void UpdateStatus_ShouldFail_WhenAlreadyRejected()
    {
        var proposal = CreateSubmittedProposal();
        proposal.UpdateStatus(ProposalStatus.Rejected);

        var result = proposal.UpdateStatus(ProposalStatus.Approved);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Proposal.InvalidStatusTransition");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void UpdateStatus_ShouldFail_WhenAlreadyDisbursed()
    {
        var proposal = CreateSubmittedProposal();
        proposal.UpdateStatus(ProposalStatus.Disbursed);

        var result = proposal.UpdateStatus(ProposalStatus.Approved);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Proposal.InvalidStatusTransition");
    }
}
