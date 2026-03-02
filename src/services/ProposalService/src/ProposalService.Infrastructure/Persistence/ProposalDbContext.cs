using ConsignadoHub.BuildingBlocks.Messaging;
using Microsoft.EntityFrameworkCore;
using ProposalService.Domain.Entities;

namespace ProposalService.Infrastructure.Persistence;

public sealed class ProposalDbContext(DbContextOptions<ProposalDbContext> options) : DbContext(options)
{
    public DbSet<Proposal> Proposals => Set<Proposal>();
    public DbSet<ProposalTimelineEntry> ProposalTimeline => Set<ProposalTimelineEntry>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ProposalDbContext).Assembly);
    }
}
