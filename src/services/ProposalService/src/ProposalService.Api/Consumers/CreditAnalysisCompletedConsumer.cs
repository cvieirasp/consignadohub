using ConsignadoHub.BuildingBlocks.Messaging.RabbitMq;
using ConsignadoHub.Contracts.Events;
using Microsoft.Extensions.DependencyInjection;
using ProposalService.Application.UseCases;

namespace ProposalService.Api.Consumers;

public sealed class CreditAnalysisCompletedConsumer(
    RabbitMqEventPublisher publisher,
    RabbitMqSettings settings,
    IServiceScopeFactory scopeFactory,
    ILogger<CreditAnalysisCompletedConsumer> logger)
    : RabbitMqConsumerBase<CreditAnalysisCompletedEvent>(publisher, settings, logger)
{
    protected override string QueueName => "proposal.credit.completed";
    protected override string RoutingKey => "proposal.credit.completed";
    protected override string ConsumerName => "CreditAnalysisCompletedConsumer";

    protected override async Task HandleAsync(CreditAnalysisCompletedEvent @event, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<HandleCreditAnalysisCompletedUseCase>();

        var result = await useCase.ExecuteAsync(@event, ct);

        if (result.IsFailure)
        {
            logger.LogWarning(
                "HandleCreditAnalysisCompleted failed for proposal {ProposalId}: {Error}",
                @event.ProposalId, result.Error.Message);
        }
    }
}
