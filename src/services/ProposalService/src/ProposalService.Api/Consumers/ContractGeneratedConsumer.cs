using ConsignadoHub.BuildingBlocks.Messaging.RabbitMq;
using WorkflowWorker.Contracts.Events;
using Microsoft.Extensions.DependencyInjection;
using ProposalService.Application.UseCases;

namespace ProposalService.Api.Consumers;

public sealed class ContractGeneratedConsumer(
    RabbitMqEventPublisher publisher,
    RabbitMqSettings settings,
    IServiceScopeFactory scopeFactory,
    ILogger<ContractGeneratedConsumer> logger)
    : RabbitMqConsumerBase<ContractGeneratedEvent>(publisher, settings, logger)
{
    protected override string QueueName => "proposal.contract.generated";
    protected override string RoutingKey => "contract.generated";
    protected override string ConsumerName => "ContractGeneratedConsumer";

    protected override async Task HandleAsync(ContractGeneratedEvent @event, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<HandleContractGeneratedUseCase>();

        var result = await useCase.ExecuteAsync(@event, ct);

        if (result.IsFailure)
        {
            logger.LogWarning(
                "HandleContractGenerated failed for proposal {ProposalId}: {Error}",
                @event.ProposalId, result.Error.Message);
        }
    }
}
