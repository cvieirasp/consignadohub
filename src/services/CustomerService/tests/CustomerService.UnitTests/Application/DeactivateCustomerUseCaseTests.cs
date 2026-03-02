using CustomerService.Application.Ports;
using CustomerService.Application.UseCases;
using CustomerService.Domain.Entities;
using CustomerService.Domain.Errors;
using CustomerService.Domain.ValueObjects;
using FluentAssertions;
using Moq;

namespace CustomerService.UnitTests.Application;

public sealed class DeactivateCustomerUseCaseTests
{
    private readonly Mock<ICustomerRepository> _repositoryMock = new();
    private readonly DeactivateCustomerUseCase _useCase;

    public DeactivateCustomerUseCaseTests()
    {
        _useCase = new DeactivateCustomerUseCase(_repositoryMock.Object);
    }

    private static Customer ActiveCustomer() =>
        Customer.Create("John Doe", Cpf.Create("529.982.247-25"), "john@example.com", "11999990000", new DateOnly(1990, 1, 1));

    [Fact]
    public async Task Execute_ShouldSucceed_WhenCustomerIsActive()
    {
        var customer = ActiveCustomer();
        _repositoryMock.Setup(r => r.GetByIdTrackedAsync(customer.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);
        _repositoryMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _useCase.ExecuteAsync(customer.Id);

        result.IsSuccess.Should().BeTrue();
        customer.IsActive.Should().BeFalse();
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Execute_ShouldFail_WhenCustomerNotFound()
    {
        var id = Guid.NewGuid();
        _repositoryMock.Setup(r => r.GetByIdTrackedAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        var result = await _useCase.ExecuteAsync(id);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(CustomerErrors.NotFound(id).Code);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Execute_ShouldFail_WhenCustomerIsAlreadyInactive()
    {
        var customer = ActiveCustomer();
        customer.Deactivate();
        _repositoryMock.Setup(r => r.GetByIdTrackedAsync(customer.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        var result = await _useCase.ExecuteAsync(customer.Id);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(CustomerErrors.CustomerInactive.Code);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
