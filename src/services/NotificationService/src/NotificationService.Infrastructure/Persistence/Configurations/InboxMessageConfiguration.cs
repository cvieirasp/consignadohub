using ConsignadoHub.BuildingBlocks.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NotificationService.Infrastructure.Persistence.Configurations;

internal sealed class InboxMessageConfiguration : IEntityTypeConfiguration<InboxMessage>
{
    public void Configure(EntityTypeBuilder<InboxMessage> builder)
    {
        builder.ToTable("InboxMessages");

        builder.HasKey(m => new { m.EventId, m.ConsumerName });

        builder.Property(m => m.EventId).IsRequired();
        builder.Property(m => m.ConsumerName).IsRequired().HasMaxLength(250);
        builder.Property(m => m.ProcessedAt).IsRequired();
    }
}
