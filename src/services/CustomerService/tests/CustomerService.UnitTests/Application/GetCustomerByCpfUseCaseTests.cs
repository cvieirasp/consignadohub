using CustomerService.Application.Ports;
using CustomerService.Application.UseCases;
using CustomerService.Domain.Entities;
using CustomerService.Domain.Errors;
using CustomerService.Domain.ValueObjects;
using FluentAssertions;
using Moq;

namespace CustomerService.UnitTests.Application;

public sealed class GetCustomerByCpfUseCaseTests
{
    private readonly Mock<ICustomerRepository> _repositoryMock = new();
    private readonly GetCustomerByCpfUseCase _useCase;

    public GetCustomerByCpfUseCaseTests()
    {
        _useCase = new GetCustomerByCpfUseCase(_repositoryMock.Object);
    }

    private const string ValidCpf = "529.982.247-25";
    private const string NormalizedCpf = "52998224725";

    private static Customer ExistingCustomer() =>
        Customer.Create("Jane Doe", Cpf.Create(ValidCpf), "jane@example.com", "11999990000", new DateOnly(1985, 5, 15));

    [Fact]
    public async Task Execute_ShouldReturnDto_WhenCpfIsValidAndCustomerExists()
    {
        var customer = ExistingCustomer();
        _repositoryMock.Setup(r => r.GetByCpfAsync(NormalizedCpf, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        var result = await _useCase.ExecuteAsync(ValidCpf);

        result.IsSuccess.Should().BeTrue();
        result.Value!.FullName.Should().Be("Jane Doe");
        result.Value.Email.Should().Be("jane@example.com");
    }

    [Fact]
    public async Task Execute_ShouldFail_WhenCpfIsInvalid()
    {
        const string invalidCpf = "000.000.000-00";

        var result = await _useCase.ExecuteAsync(invalidCpf);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(CustomerErrors.InvalidCpf(invalidCpf).Code);
        _repositoryMock.Verify(r => r.GetByCpfAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Execute_ShouldFail_WhenCustomerNotFound()
    {
        _repositoryMock.Setup(r => r.GetByCpfAsync(NormalizedCpf, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        var result = await _useCase.ExecuteAsync(ValidCpf);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(CustomerErrors.NotFound(Guid.Empty).Code);
    }
}
