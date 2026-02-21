using CustomerService.Domain.Entities;
using CustomerService.Domain.ValueObjects;
using FluentAssertions;

namespace CustomerService.UnitTests.Domain;

public sealed class CustomerTests
{
    private static Customer CreateValidCustomer() => Customer.Create(
        "John Doe",
        Cpf.Create("529.982.247-25"),
        "john@example.com",
        "11999990000",
        new DateOnly(1990, 1, 1));

    [Fact]
    public void Create_ShouldReturnActiveCustomer_WithCorrectData()
    {
        var customer = CreateValidCustomer();

        customer.FullName.Should().Be("John Doe");
        customer.Email.Should().Be("john@example.com");
        customer.Phone.Should().Be("11999990000");
        customer.IsActive.Should().BeTrue();
        customer.Id.Should().NotBeEmpty();
        customer.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Deactivate_ShouldSetIsActiveFalse()
    {
        var customer = CreateValidCustomer();
        customer.Deactivate();
        customer.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Update_ShouldChangeNameEmailPhone()
    {
        var customer = CreateValidCustomer();
        customer.Update("Jane Doe", "jane@example.com", "11888880000");

        customer.FullName.Should().Be("Jane Doe");
        customer.Email.Should().Be("jane@example.com");
        customer.Phone.Should().Be("11888880000");
        customer.UpdatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }
}
