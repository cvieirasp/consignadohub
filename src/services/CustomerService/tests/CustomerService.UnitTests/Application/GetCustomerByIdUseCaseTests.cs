using CustomerService.Application.Ports;
using CustomerService.Application.UseCases;
using CustomerService.Domain.Entities;
using CustomerService.Domain.Errors;
using CustomerService.Domain.ValueObjects;
using FluentAssertions;
using Moq;

namespace CustomerService.UnitTests.Application;

public sealed class GetCustomerByIdUseCaseTests
{
    private readonly Mock<ICustomerRepository> _repositoryMock = new();
    private readonly GetCustomerByIdUseCase _useCase;

    public GetCustomerByIdUseCaseTests()
    {
        _useCase = new GetCustomerByIdUseCase(_repositoryMock.Object);
    }

    [Fact]
    public async Task Execute_ShouldReturnDto_WhenCustomerExists()
    {
        var customer = Customer.Create(
            "Jane Doe",
            Cpf.Create("529.982.247-25"),
            "jane@example.com",
            "11999990000",
            new DateOnly(1985, 5, 15));

        _repositoryMock.Setup(r => r.GetByIdAsync(customer.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        var result = await _useCase.ExecuteAsync(customer.Id);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(customer.Id);
        result.Value.FullName.Should().Be("Jane Doe");
        result.Value.Email.Should().Be("jane@example.com");
        result.Value.Cpf.Should().Be("52998224725");
        result.Value.Phone.Should().Be("11999990000");
        result.Value.BirthDate.Should().Be(new DateOnly(1985, 5, 15));
        result.Value.CreatedAt.Should().BeCloseTo(customer.CreatedAt, TimeSpan.FromSeconds(1));
        result.Value.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Execute_ShouldReturnNotFound_WhenCustomerDoesNotExist()
    {
        var id = Guid.NewGuid();
        _repositoryMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        var result = await _useCase.ExecuteAsync(id);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(CustomerErrors.NotFound(id).Code);
    }
}
