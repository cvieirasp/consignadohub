using ConsignadoHub.BuildingBlocks.Auth;
using ConsignadoHub.BuildingBlocks.Results;
using CustomerService.Application.DTOs;

namespace CustomerService.Api.Endpoints;

public static partial class CustomerEndpoints
{
    public static RouteGroupBuilder MapCustomerEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/", CreateCustomer)
            .WithName("CreateCustomer")
            .WithSummary("Create a new customer")
            .RequireAuthorization(Policies.AdminOnly)
            .Produces<Guid>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet("/{id:guid}", GetCustomerById)
            .WithName("GetCustomerById")
            .WithSummary("Get customer by ID")
            .RequireAuthorization(Policies.AnalystOrAdmin)
            .Produces<CustomerDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/cpf/{cpf}", GetCustomerByCpf)
            .WithName("GetCustomerByCpf")
            .WithSummary("Get customer by CPF")
            .RequireAuthorization(Policies.AnalystOrAdmin)
            .Produces<CustomerDto>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut("/{id:guid}", UpdateCustomer)
            .WithName("UpdateCustomer")
            .WithSummary("Update customer information")
            .RequireAuthorization(Policies.AdminOnly)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:guid}", DeactivateCustomer)
            .WithName("DeactivateCustomer")
            .WithSummary("Deactivate a customer")
            .RequireAuthorization(Policies.AdminOnly)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet("/", SearchCustomers)
            .WithName("SearchCustomers")
            .WithSummary("Search customers by name")
            .RequireAuthorization(Policies.AnalystOrAdmin)
            .Produces<PagedResult<CustomerSummaryDto>>();

        return group;
    }
}
