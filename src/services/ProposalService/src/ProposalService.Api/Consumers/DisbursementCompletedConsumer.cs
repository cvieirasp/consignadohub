using ConsignadoHub.BuildingBlocks.Messaging.RabbitMq;
using WorkflowWorker.Contracts.Events;
using Microsoft.Extensions.DependencyInjection;
using ProposalService.Application.UseCases;

namespace ProposalService.Api.Consumers;

public sealed class DisbursementCompletedConsumer(
    RabbitMqEventPublisher publisher,
    RabbitMqSettings settings,
    IServiceScopeFactory scopeFactory,
    ILogger<DisbursementCompletedConsumer> logger)
    : RabbitMqConsumerBase<DisbursementCompletedEvent>(publisher, settings, logger)
{
    protected override string QueueName => "proposal.disbursement.completed";
    protected override string RoutingKey => "disbursement.completed";
    protected override string ConsumerName => "DisbursementCompletedConsumer";

    protected override async Task HandleAsync(DisbursementCompletedEvent @event, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<HandleDisbursementCompletedUseCase>();

        var result = await useCase.ExecuteAsync(@event, ct);

        if (result.IsFailure)
        {
            logger.LogWarning(
                "HandleDisbursementCompleted failed for proposal {ProposalId}: {Error}",
                @event.ProposalId, result.Error.Message);
        }
    }
}
