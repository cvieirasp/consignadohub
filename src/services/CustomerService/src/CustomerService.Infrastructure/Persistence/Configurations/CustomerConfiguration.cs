using CustomerService.Domain.Entities;
using CustomerService.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustomerService.Infrastructure.Persistence.Configurations;

internal sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.FullName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.Cpf)
            .IsRequired()
            .HasMaxLength(11)
            .HasConversion(
                cpf => cpf.Value,
                value => Cpf.Create(value));

        builder.HasIndex(c => c.Cpf)
            .IsUnique()
            .HasDatabaseName("IX_Customers_Cpf");

        builder.Property(c => c.Email)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.Phone)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(c => c.BirthDate)
            .IsRequired();

        builder.Property(c => c.CreatedAt)
            .IsRequired();

        builder.Property(c => c.UpdatedAt)
            .IsRequired();

        builder.Property(c => c.IsActive)
            .IsRequired();

        builder.HasIndex(c => c.FullName)
            .HasDatabaseName("IX_Customers_FullName");
    }
}
