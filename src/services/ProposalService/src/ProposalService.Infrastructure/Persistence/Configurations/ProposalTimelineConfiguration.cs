using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProposalService.Domain.Entities;

namespace ProposalService.Infrastructure.Persistence.Configurations;

internal sealed class ProposalTimelineConfiguration : IEntityTypeConfiguration<ProposalTimelineEntry>
{
    public void Configure(EntityTypeBuilder<ProposalTimelineEntry> builder)
    {
        builder.ToTable("ProposalTimeline");

        // Same reasoning as ProposalConfiguration: private set properties require
        // PropertyAccessMode.Property so EF Core uses reflection-based setter
        // instead of the compiled field accessor during materialization.
        builder.UsePropertyAccessMode(PropertyAccessMode.Property);

        builder.HasKey(t => t.Id);

        // Id is always generated client-side (Guid.NewGuid() in the constructor).
        // ValueGeneratedNever tells EF Core not to treat a non-default Guid as
        // "already persisted", so new entries found during DetectChanges are
        // correctly tracked as Added (INSERT) rather than Unchanged/Modified (UPDATE).
        builder.Property(t => t.Id).ValueGeneratedNever();

        builder.Property(t => t.ProposalId).IsRequired();
        builder.Property(t => t.FromStatus).HasConversion<int?>().IsRequired(false);
        builder.Property(t => t.ToStatus).IsRequired().HasConversion<int>();
        builder.Property(t => t.OccurredAt).IsRequired();
        builder.Property(t => t.Reason).HasMaxLength(500);

        builder.HasIndex(t => t.ProposalId).HasDatabaseName("IX_ProposalTimeline_ProposalId");
    }
}
