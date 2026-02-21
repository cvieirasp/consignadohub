using CustomerService.Domain.ValueObjects;

namespace CustomerService.Domain.Entities;

public sealed class Customer
{
    public Guid Id { get; private set; }
    public string FullName { get; private set; } = default!;
    public Cpf Cpf { get; private set; } = default!;
    public string Email { get; private set; } = default!;
    public string Phone { get; private set; } = default!;
    public DateOnly BirthDate { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private Customer() { }

    public static Customer Create(
        string fullName,
        Cpf cpf,
        string email,
        string phone,
        DateOnly birthDate)
    {
        return new Customer
        {
            Id = Guid.NewGuid(),
            FullName = fullName,
            Cpf = cpf,
            Email = email,
            Phone = phone,
            BirthDate = birthDate,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            IsActive = true,
        };
    }

    public void Update(string fullName, string email, string phone)
    {
        FullName = fullName;
        Email = email;
        Phone = phone;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
