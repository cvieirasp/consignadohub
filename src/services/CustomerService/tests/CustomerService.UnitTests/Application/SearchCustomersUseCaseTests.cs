using CustomerService.Application.DTOs;
using CustomerService.Application.Ports;
using CustomerService.Application.UseCases;
using CustomerService.Domain.Entities;
using CustomerService.Domain.ValueObjects;
using FluentAssertions;
using Moq;

namespace CustomerService.UnitTests.Application;

public sealed class SearchCustomersUseCaseTests
{
    private readonly Mock<ICustomerRepository> _repositoryMock = new();
    private readonly SearchCustomersUseCase _useCase;

    public SearchCustomersUseCaseTests()
    {
        _useCase = new SearchCustomersUseCase(_repositoryMock.Object);
    }

    private static Customer MakeCustomer(string name, string cpf, string email, string phone, DateOnly birthDate) =>
        Customer.Create(name, Cpf.Create(cpf), email, phone, birthDate);

    private void SetupSearch(string? name, int page, int pageSize, IReadOnlyList<Customer> items, int total) =>
        _repositoryMock
            .Setup(r => r.SearchAsync(name, page, pageSize, It.IsAny<CancellationToken>()))
            .ReturnsAsync((items, total));

    [Fact]
    public async Task Execute_ShouldReturnMappedPagedResult_WhenCustomersExist()
    {
        var customers = new[] {
            MakeCustomer("Jane", "529.982.247-25", "jane@example.com", "11999990001", new DateOnly(1990, 1, 1)),
            MakeCustomer("John", "111.444.777-35", "john@example.com", "11999990002", new DateOnly(1991, 2, 2))
        };
        var input = new SearchCustomersInput(Name: null, Page: 1, PageSize: 20);
        SetupSearch(null, 1, 20, customers, 2);

        var result = await _useCase.ExecuteAsync(input);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(2);
        result.Value.TotalCount.Should().Be(2);
        result.Value.Page.Should().Be(1);
        result.Value.PageSize.Should().Be(20);
        result.Value.Items[0].Id.Should().Be(customers[0].Id);
        result.Value.Items[1].Id.Should().Be(customers[1].Id);
        result.Value.Items[0].FullName.Should().Be("Jane");
        result.Value.Items[1].FullName.Should().Be("John");
        result.Value.Items[0].Cpf.Should().Be("52998224725");
        result.Value.Items[1].Cpf.Should().Be("11144477735");
    }

    [Fact]
    public async Task Execute_ShouldReturnEmptyResult_WhenNoCustomersMatch()
    {
        var input = new SearchCustomersInput(Name: "Unknown", Page: 1, PageSize: 20);
        SetupSearch("Unknown", 1, 20, Array.Empty<Customer>(), 0);

        var result = await _useCase.ExecuteAsync(input);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Execute_ShouldClampPageToOne_WhenPageIsZero()
    {
        var input = new SearchCustomersInput(Name: null, Page: 0, PageSize: 20);
        SetupSearch(null, 1, 20, Array.Empty<Customer>(), 0);

        var result = await _useCase.ExecuteAsync(input);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Page.Should().Be(1);
        _repositoryMock.Verify(r => r.SearchAsync(null, 1, 20, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Execute_ShouldClampPageSizeToMaximum_WhenPageSizeExceedsLimit()
    {
        var input = new SearchCustomersInput(Name: null, Page: 1, PageSize: 200);
        SetupSearch(null, 1, 100, Array.Empty<Customer>(), 0);

        var result = await _useCase.ExecuteAsync(input);

        result.IsSuccess.Should().BeTrue();
        result.Value!.PageSize.Should().Be(100);
        _repositoryMock.Verify(r => r.SearchAsync(null, 1, 100, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Execute_ShouldClampPageSizeToOne_WhenPageSizeIsZero()
    {
        var input = new SearchCustomersInput(Name: null, Page: 1, PageSize: 0);
        SetupSearch(null, 1, 1, Array.Empty<Customer>(), 0);

        var result = await _useCase.ExecuteAsync(input);

        result.IsSuccess.Should().BeTrue();
        result.Value!.PageSize.Should().Be(1);
        _repositoryMock.Verify(r => r.SearchAsync(null, 1, 1, It.IsAny<CancellationToken>()), Times.Once);
    }
}
