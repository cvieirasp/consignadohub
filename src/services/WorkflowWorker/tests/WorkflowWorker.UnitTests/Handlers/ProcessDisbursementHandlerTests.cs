using FluentAssertions;
using WorkflowWorker.Application.Handlers;

namespace WorkflowWorker.UnitTests.Handlers;

public sealed class ProcessDisbursementHandlerTests
{
    private readonly ProcessDisbursementHandler _handler = new();

    [Fact]
    [Trait("Category", "Unit")]
    public void Process_ShouldReturnNonEmptyDisbursementId()
    {
        var (disbursementId, _) = _handler.Process(Guid.NewGuid());

        disbursementId.Should().NotBe(Guid.Empty);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Process_ShouldReturnCompletedAtCloseToUtcNow()
    {
        var before = DateTimeOffset.UtcNow;

        var (_, completedAt) = _handler.Process(Guid.NewGuid());

        var after = DateTimeOffset.UtcNow;
        completedAt.Should().BeOnOrAfter(before);
        completedAt.Should().BeOnOrBefore(after);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Process_ShouldReturnUniqueIdEachCall()
    {
        var (id1, _) = _handler.Process(Guid.NewGuid());
        var (id2, _) = _handler.Process(Guid.NewGuid());

        id1.Should().NotBe(id2);
    }
}
