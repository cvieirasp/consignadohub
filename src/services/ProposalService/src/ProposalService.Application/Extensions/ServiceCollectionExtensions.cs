using System.Diagnostics.CodeAnalysis;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using ProposalService.Application.Configuration;
using ProposalService.Application.DTOs;
using ProposalService.Application.UseCases;
using ProposalService.Application.Validators;

namespace ProposalService.Application.Extensions;

[ExcludeFromCodeCoverage]
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddProposalApplication(
        this IServiceCollection services,
        Action<ProposalSettings>? configureSettings = null)
    {
        if (configureSettings is not null)
            services.Configure(configureSettings);
        else
            services.AddOptions<ProposalSettings>();

        services.AddScoped<SimulateProposalUseCase>();
        services.AddScoped<SubmitProposalUseCase>();
        services.AddScoped<GetProposalByIdUseCase>();
        services.AddScoped<GetProposalTimelineUseCase>();
        services.AddScoped<ListProposalsByCustomerUseCase>();
        services.AddScoped<HandleCreditAnalysisCompletedUseCase>();
        services.AddScoped<HandleContractGeneratedUseCase>();
        services.AddScoped<HandleDisbursementCompletedUseCase>();

        services.AddScoped<IValidator<SubmitProposalInput>, SubmitProposalValidator>();

        return services;
    }
}
