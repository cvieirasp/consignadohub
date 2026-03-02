using FluentAssertions;
using WorkflowWorker.Application.Handlers;

namespace WorkflowWorker.UnitTests.Handlers;

public sealed class ProcessContractGenerationHandlerTests
{
    private readonly ProcessContractGenerationHandler _handler = new();

    [Fact]
    [Trait("Category", "Unit")]
    public void Generate_ShouldReturnNonEmptyContractId()
    {
        var (contractId, _) = _handler.Generate(Guid.NewGuid());

        contractId.Should().NotBe(Guid.Empty);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Generate_ShouldReturnUrlContainingContractId()
    {
        var proposalId = Guid.NewGuid();
        var (contractId, contractUrl) = _handler.Generate(proposalId);

        contractUrl.Should().Contain(contractId.ToString("N"));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Generate_ShouldReturnUniqueContractIdEachCall()
    {
        var (id1, _) = _handler.Generate(Guid.NewGuid());
        var (id2, _) = _handler.Generate(Guid.NewGuid());

        id1.Should().NotBe(id2);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Generate_ShouldReturnNonEmptyUrl()
    {
        var (_, contractUrl) = _handler.Generate(Guid.NewGuid());

        contractUrl.Should().NotBeNullOrWhiteSpace();
        contractUrl.Should().EndWith(".pdf");
    }
}
