using FluentAssertions;
using WorkflowWorker.Application.Handlers;

namespace WorkflowWorker.UnitTests.Handlers;

public sealed class ProcessCreditAnalysisHandlerTests
{
    private readonly ProcessCreditAnalysisHandler _handler = new();

    [Fact]
    [Trait("Category", "Unit")]
    public void Analyze_ShouldReturnApproved_WhenScoreIsAtLeast600()
    {
        // We run many iterations to increase confidence that score >= 600 → Approved
        // The handler uses Random.Shared so scores vary; we verify logical consistency
        var approvedResults = Enumerable.Range(0, 200)
            .Select(_ => _handler.Analyze(Guid.NewGuid(), 10_000m, 12))
            .Where(r => r.Score >= 600)
            .ToList();

        approvedResults.Should().AllSatisfy(r =>
        {
            r.Approved.Should().BeTrue(
                because: $"score {r.Score} is >= 600 and should result in approval");
            r.Reason.Should().Contain(r.Score.ToString());
        });
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Analyze_ShouldReturnDenied_WhenScoreIsBelow600()
    {
        var deniedResults = Enumerable.Range(0, 200)
            .Select(_ => _handler.Analyze(Guid.NewGuid(), 10_000m, 12))
            .Where(r => r.Score < 600)
            .ToList();

        deniedResults.Should().AllSatisfy(r =>
        {
            r.Approved.Should().BeFalse(
                because: $"score {r.Score} is < 600 and should result in denial");
            r.Reason.Should().Contain(r.Score.ToString());
        });
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Analyze_ShouldReturnScoreInValidRange()
    {
        var results = Enumerable.Range(0, 100)
            .Select(_ => _handler.Analyze(Guid.NewGuid(), 50_000m, 24))
            .ToList();

        results.Should().AllSatisfy(r =>
        {
            r.Score.Should().BeInRange(300, 900);
        });
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Analyze_ShouldIncludeScoreInReason()
    {
        var (_, score, reason) = _handler.Analyze(Guid.NewGuid(), 10_000m, 12);

        reason.Should().Contain(score.ToString());
    }
}
