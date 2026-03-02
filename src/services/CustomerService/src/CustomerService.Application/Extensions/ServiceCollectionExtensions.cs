using System.Diagnostics.CodeAnalysis;
using CustomerService.Application.DTOs;
using CustomerService.Application.UseCases;
using CustomerService.Application.Validators;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace CustomerService.Application.Extensions;

[ExcludeFromCodeCoverage]
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCustomerApplication(this IServiceCollection services)
    {
        services.AddScoped<CreateCustomerUseCase>();
        services.AddScoped<GetCustomerByIdUseCase>();
        services.AddScoped<GetCustomerByCpfUseCase>();
        services.AddScoped<UpdateCustomerUseCase>();
        services.AddScoped<DeactivateCustomerUseCase>();
        services.AddScoped<SearchCustomersUseCase>();

        services.AddScoped<IValidator<CreateCustomerInput>, CreateCustomerValidator>();
        services.AddScoped<IValidator<UpdateCustomerInput>, UpdateCustomerValidator>();

        return services;
    }
}
