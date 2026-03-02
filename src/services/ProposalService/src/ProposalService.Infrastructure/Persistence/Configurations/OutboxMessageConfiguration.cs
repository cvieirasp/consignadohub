using ConsignadoHub.BuildingBlocks.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ProposalService.Infrastructure.Persistence.Configurations;

internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.EventType).IsRequired().HasMaxLength(500);
        builder.Property(m => m.Payload).IsRequired().HasColumnType("nvarchar(max)");
        builder.Property(m => m.RoutingKey).IsRequired().HasMaxLength(250);
        builder.Property(m => m.CreatedAt).IsRequired();
        builder.Property(m => m.ProcessedAt).IsRequired(false);
        builder.Property(m => m.AttemptCount).IsRequired();
        builder.Property(m => m.LastError).HasMaxLength(2000).IsRequired(false);

        // Efficient polling: unprocessed messages first, ordered by creation
        builder.HasIndex(m => m.ProcessedAt).HasDatabaseName("IX_OutboxMessages_ProcessedAt");
    }
}
