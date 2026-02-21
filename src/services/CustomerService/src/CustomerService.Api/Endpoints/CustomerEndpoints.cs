using ConsignadoHub.BuildingBlocks.Http;
using ConsignadoHub.BuildingBlocks.Results;
using CustomerService.Application.DTOs;
using CustomerService.Application.UseCases;
using Microsoft.AspNetCore.Mvc;

namespace CustomerService.Api.Endpoints;

public static class CustomerEndpoints
{
    public static RouteGroupBuilder MapCustomerEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/", CreateCustomer)
            .WithName("CreateCustomer")
            .WithSummary("Create a new customer")
            .Produces<Guid>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet("/{id:guid}", GetCustomerById)
            .WithName("GetCustomerById")
            .WithSummary("Get customer by ID")
            .Produces<CustomerDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/cpf/{cpf}", GetCustomerByCpf)
            .WithName("GetCustomerByCpf")
            .WithSummary("Get customer by CPF")
            .Produces<CustomerDto>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut("/{id:guid}", UpdateCustomer)
            .WithName("UpdateCustomer")
            .WithSummary("Update customer information")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:guid}", DeactivateCustomer)
            .WithName("DeactivateCustomer")
            .WithSummary("Deactivate a customer")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet("/", SearchCustomers)
            .WithName("SearchCustomers")
            .WithSummary("Search customers by name")
            .Produces<PagedResult<CustomerSummaryDto>>();

        return group;
    }

    private static async Task<IResult> CreateCustomer(
        [FromBody] CreateCustomerInput input,
        CreateCustomerUseCase useCase,
        HttpContext ctx,
        CancellationToken ct)
    {
        var result = await useCase.ExecuteAsync(input, ct);
        return result.IsSuccess
            ? Results.Created($"/v1/customers/{result.Value}", result.Value)
            : result.Error.ToHttpResult(ctx);
    }

    private static async Task<IResult> GetCustomerById(
        Guid id,
        GetCustomerByIdUseCase useCase,
        HttpContext ctx,
        CancellationToken ct)
    {
        var result = await useCase.ExecuteAsync(id, ct);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToHttpResult(ctx);
    }

    private static async Task<IResult> GetCustomerByCpf(
        string cpf,
        GetCustomerByCpfUseCase useCase,
        HttpContext ctx,
        CancellationToken ct)
    {
        var result = await useCase.ExecuteAsync(cpf, ct);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToHttpResult(ctx);
    }

    private static async Task<IResult> UpdateCustomer(
        Guid id,
        [FromBody] UpdateCustomerInput input,
        UpdateCustomerUseCase useCase,
        HttpContext ctx,
        CancellationToken ct)
    {
        var result = await useCase.ExecuteAsync(id, input, ct);
        return result.IsSuccess
            ? Results.NoContent()
            : result.Error.ToHttpResult(ctx);
    }

    private static async Task<IResult> DeactivateCustomer(
        Guid id,
        DeactivateCustomerUseCase useCase,
        HttpContext ctx,
        CancellationToken ct)
    {
        var result = await useCase.ExecuteAsync(id, ct);
        return result.IsSuccess
            ? Results.NoContent()
            : result.Error.ToHttpResult(ctx);
    }

    private static async Task<IResult> SearchCustomers(
        [FromQuery] string? name,
        [FromQuery] int page,
        [FromQuery] int pageSize,
        SearchCustomersUseCase useCase,
        HttpContext ctx,
        CancellationToken ct)
    {
        var input = new SearchCustomersInput(name, page == 0 ? 1 : page, pageSize == 0 ? 20 : pageSize);
        var result = await useCase.ExecuteAsync(input, ct);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToHttpResult(ctx);
    }
}
