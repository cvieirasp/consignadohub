using CustomerService.Application.DTOs;
using CustomerService.Application.Ports;
using CustomerService.Application.UseCases;
using CustomerService.Application.Validators;
using CustomerService.Domain.Entities;
using CustomerService.Domain.Errors;
using FluentAssertions;
using Moq;

namespace CustomerService.UnitTests.Application;

public sealed class CreateCustomerUseCaseTests
{
    private readonly Mock<ICustomerRepository> _repositoryMock = new();
    private readonly CreateCustomerUseCase _useCase;

    public CreateCustomerUseCaseTests()
    {
        _useCase = new CreateCustomerUseCase(_repositoryMock.Object, new CreateCustomerValidator());
    }

    private static CreateCustomerInput ValidInput() => new(
        "John Doe",
        "529.982.247-25",
        "john@example.com",
        "11999990000",
        new DateOnly(1990, 1, 1));

    [Fact]
    public async Task Execute_ShouldReturnGuid_WhenInputIsValid()
    {
        _repositoryMock.Setup(r => r.ExistsByCpfAsync("52998224725", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _repositoryMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _useCase.ExecuteAsync(ValidInput());

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Execute_ShouldFail_WhenCpfAlreadyExists()
    {
        _repositoryMock.Setup(r => r.ExistsByCpfAsync("52998224725", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _useCase.ExecuteAsync(ValidInput());

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(CustomerErrors.CpfAlreadyExists("52998224725").Code);
    }

    [Fact]
    public async Task Execute_ShouldFail_WhenCpfIsInvalid()
    {
        var input = ValidInput() with { Cpf = "000.000.000-00" };

        var result = await _useCase.ExecuteAsync(input);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(CustomerErrors.InvalidCpf("000.000.000-00").Code);
    }

    [Fact]
    public async Task Execute_ShouldFail_WhenValidationFails()
    {
        var input = ValidInput() with { FullName = "" };

        var result = await _useCase.ExecuteAsync(input);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(CustomerErrors.ValidationFailed().Code);
    }
}
