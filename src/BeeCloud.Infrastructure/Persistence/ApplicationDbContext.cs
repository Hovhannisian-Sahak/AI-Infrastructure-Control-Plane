using BeeCloud.Domain.Entities;
using BeeCloud.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BeeCloud.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<ComputeNode> ComputeNodes => Set<ComputeNode>();
    public DbSet<Network> Networks =>
        Set<Network>();

    public DbSet<NetworkAttachment> NetworkAttachments =>
        Set<NetworkAttachment>();
    public DbSet<HealthCheck> HealthChecks => Set<HealthCheck>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ApplicationDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}