using ConsignadoHub.BuildingBlocks.Results;
using CustomerService.Application.DTOs;
using CustomerService.Application.Mappers;
using CustomerService.Application.Ports;

namespace CustomerService.Application.UseCases;

public sealed class SearchCustomersUseCase(ICustomerRepository repository)
{
    public async Task<Result<PagedResult<CustomerSummaryDto>>> ExecuteAsync(
        SearchCustomersInput input,
        CancellationToken ct = default)
    {
        var page = Math.Max(1, input.Page);
        var pageSize = Math.Clamp(input.PageSize, 1, 100);

        var (items, total) = await repository.SearchAsync(input.Name, page, pageSize, ct);

        var dtos = items.Select(CustomerMapper.ToSummaryDto).ToList();

        return new PagedResult<CustomerSummaryDto>(dtos, total, page, pageSize);
    }
}
