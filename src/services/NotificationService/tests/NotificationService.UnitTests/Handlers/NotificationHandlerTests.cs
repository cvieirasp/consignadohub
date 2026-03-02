using FluentAssertions;
using Microsoft.Extensions.Logging;
using NotificationService.Application.Handlers;

namespace NotificationService.UnitTests.Handlers;

public sealed class NotificationHandlerTests
{
    private readonly NotificationHandler _handler;

    public NotificationHandlerTests()
    {
        var logger = LoggerFactory.Create(b => b.AddConsole())
            .CreateLogger<NotificationHandler>();
        _handler = new NotificationHandler(logger);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task SendProposalSubmittedAsync_ShouldCompleteWithoutThrowing()
    {
        var act = async () => await _handler.SendProposalSubmittedAsync(
            Guid.NewGuid(), "corr-123", CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task SendCreditAnalysisCompletedAsync_ShouldCompleteWithoutThrowing_WhenApproved()
    {
        var act = async () => await _handler.SendCreditAnalysisCompletedAsync(
            Guid.NewGuid(), approved: true, "corr-123", CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task SendCreditAnalysisCompletedAsync_ShouldCompleteWithoutThrowing_WhenRejected()
    {
        var act = async () => await _handler.SendCreditAnalysisCompletedAsync(
            Guid.NewGuid(), approved: false, "corr-123", CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task SendContractGeneratedAsync_ShouldCompleteWithoutThrowing()
    {
        var act = async () => await _handler.SendContractGeneratedAsync(
            Guid.NewGuid(),
            "https://contracts.consignadohub.internal/abc.pdf",
            "corr-123",
            CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task SendDisbursementCompletedAsync_ShouldCompleteWithoutThrowing()
    {
        var act = async () => await _handler.SendDisbursementCompletedAsync(
            Guid.NewGuid(), DateTimeOffset.UtcNow, "corr-123", CancellationToken.None);

        await act.Should().NotThrowAsync();
    }
}
