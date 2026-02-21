using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProposalService.Domain.Entities;

namespace ProposalService.Infrastructure.Persistence.Configurations;

internal sealed class ProposalTimelineConfiguration : IEntityTypeConfiguration<ProposalTimelineEntry>
{
    public void Configure(EntityTypeBuilder<ProposalTimelineEntry> builder)
    {
        builder.ToTable("ProposalTimeline");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.ProposalId).IsRequired();
        builder.Property(t => t.FromStatus).HasConversion<int?>().IsRequired(false);
        builder.Property(t => t.ToStatus).IsRequired().HasConversion<int>();
        builder.Property(t => t.OccurredAt).IsRequired();
        builder.Property(t => t.Reason).HasMaxLength(500);

        builder.HasIndex(t => t.ProposalId).HasDatabaseName("IX_ProposalTimeline_ProposalId");
    }
}
