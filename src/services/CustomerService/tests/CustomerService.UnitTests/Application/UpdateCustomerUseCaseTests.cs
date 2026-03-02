using CustomerService.Application.DTOs;
using CustomerService.Application.Ports;
using CustomerService.Application.UseCases;
using CustomerService.Application.Validators;
using CustomerService.Domain.Entities;
using CustomerService.Domain.Errors;
using CustomerService.Domain.ValueObjects;
using FluentAssertions;
using Moq;

namespace CustomerService.UnitTests.Application;

public sealed class UpdateCustomerUseCaseTests
{
    private readonly Mock<ICustomerRepository> _repositoryMock = new();
    private readonly UpdateCustomerUseCase _useCase;

    public UpdateCustomerUseCaseTests()
    {
        _useCase = new UpdateCustomerUseCase(_repositoryMock.Object, new UpdateCustomerValidator());
    }

    private static UpdateCustomerInput ValidInput() =>
        new("John Updated", "updated@example.com", "11888880000");

    private static Customer ActiveCustomer() =>
        Customer.Create("John Doe", Cpf.Create("529.982.247-25"), "john@example.com", "11999990000", new DateOnly(1990, 1, 1));

    [Fact]
    public async Task Execute_ShouldSucceed_WhenInputIsValid()
    {
        var customer = ActiveCustomer();
        _repositoryMock.Setup(r => r.GetByIdTrackedAsync(customer.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);
        _repositoryMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _useCase.ExecuteAsync(customer.Id, ValidInput());

        result.IsSuccess.Should().BeTrue();
        customer.FullName.Should().Be("John Updated");
        customer.Email.Should().Be("updated@example.com");
        customer.Phone.Should().Be("11888880000");
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Execute_ShouldFail_WhenValidationFails()
    {
        var input = ValidInput() with { FullName = "" };

        var result = await _useCase.ExecuteAsync(Guid.NewGuid(), input);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(CustomerErrors.ValidationFailed().Code);
        _repositoryMock.Verify(r => r.GetByIdTrackedAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Execute_ShouldFail_WhenCustomerNotFound()
    {
        var id = Guid.NewGuid();
        _repositoryMock.Setup(r => r.GetByIdTrackedAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        var result = await _useCase.ExecuteAsync(id, ValidInput());

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(CustomerErrors.NotFound(id).Code);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Execute_ShouldFail_WhenCustomerIsInactive()
    {
        var customer = ActiveCustomer();
        customer.Deactivate();
        _repositoryMock.Setup(r => r.GetByIdTrackedAsync(customer.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        var result = await _useCase.ExecuteAsync(customer.Id, ValidInput());

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(CustomerErrors.CustomerInactive.Code);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
