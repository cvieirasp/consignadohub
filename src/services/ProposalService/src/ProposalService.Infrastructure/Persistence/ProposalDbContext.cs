using Microsoft.EntityFrameworkCore;
using ProposalService.Domain.Entities;

namespace ProposalService.Infrastructure.Persistence;

public sealed class ProposalDbContext(DbContextOptions<ProposalDbContext> options) : DbContext(options)
{
    public DbSet<Proposal> Proposals => Set<Proposal>();
    public DbSet<ProposalTimelineEntry> ProposalTimeline => Set<ProposalTimelineEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ProposalDbContext).Assembly);
    }
}
