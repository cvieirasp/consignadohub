using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProposalService.Domain.Entities;
using ProposalService.Domain.Enums;

namespace ProposalService.Infrastructure.Persistence.Configurations;

internal sealed class ProposalConfiguration : IEntityTypeConfiguration<Proposal>
{
    public void Configure(EntityTypeBuilder<Proposal> builder)
    {
        builder.ToTable("Proposals");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.CustomerId).IsRequired();
        builder.Property(p => p.RequestedAmount).IsRequired().HasPrecision(18, 2);
        builder.Property(p => p.TermMonths).IsRequired();
        builder.Property(p => p.MonthlyRate).IsRequired().HasPrecision(10, 4);
        builder.Property(p => p.InstallmentAmount).IsRequired().HasPrecision(18, 2);
        builder.Property(p => p.TotalAmount).IsRequired().HasPrecision(18, 2);
        builder.Property(p => p.CET).IsRequired().HasPrecision(10, 6);
        builder.Property(p => p.Status).IsRequired().HasConversion<int>();
        builder.Property(p => p.CreatedAt).IsRequired();
        builder.Property(p => p.UpdatedAt).IsRequired();

        builder.HasIndex(p => p.CustomerId).HasDatabaseName("IX_Proposals_CustomerId");
        builder.HasIndex(p => p.Status).HasDatabaseName("IX_Proposals_Status");

        builder.HasMany(p => p.Timeline)
            .WithOne()
            .HasForeignKey(t => t.ProposalId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata.FindNavigation(nameof(Proposal.Timeline))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
