using BeeCloud.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BeeCloud.Infrastructure.Persistence.Configurations;

public class HealthCheckConfiguration
    : IEntityTypeConfiguration<HealthCheck>
{
    public void Configure(EntityTypeBuilder<HealthCheck> builder)
    {
        builder.ToTable("health_checks");

        builder.HasKey(healthCheck => healthCheck.Id);

        builder.Property(healthCheck => healthCheck.ComputeNodeId)
            .IsRequired();

        builder.Property(healthCheck => healthCheck.IsHealthy)
            .IsRequired();

        builder.Property(healthCheck => healthCheck.CpuUsagePercent);

        builder.Property(healthCheck => healthCheck.GpuUsagePercent);

        builder.Property(healthCheck => healthCheck.GpuTemperatureCelsius);

        builder.Property(healthCheck => healthCheck.CheckedAt)
            .IsRequired();

        builder.HasIndex(healthCheck => new
        {
            healthCheck.ComputeNodeId,
            healthCheck.CheckedAt
        });

        builder.HasOne<ComputeNode>()
            .WithMany()
            .HasForeignKey(healthCheck => healthCheck.ComputeNodeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}