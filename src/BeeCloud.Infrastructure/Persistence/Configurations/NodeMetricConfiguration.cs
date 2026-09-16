using BeeCloud.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BeeCloud.Infrastructure.Persistence.Configurations;

public class NodeMetricConfiguration : IEntityTypeConfiguration<NodeMetric>
{
    public void Configure(
        EntityTypeBuilder<NodeMetric> builder)
    {
        builder.ToTable("node_metrics");

        builder.HasKey(metric => metric.Id);

        builder.Property(metric => metric.Id)
            .ValueGeneratedNever();

        builder.Property(metric => metric.ComputeNodeId)
            .IsRequired();

        builder.Property(metric => metric.CpuUsagePercent)
            .IsRequired();

        builder.Property(metric => metric.GpuUsagePercent)
            .IsRequired();

        builder.Property(metric => metric.GpuTemperatureCelsius)
            .IsRequired();

        builder.Property(metric => metric.RecordedAt)
            .IsRequired();

        builder.HasOne<ComputeNode>()
            .WithMany()
            .HasForeignKey(metric => metric.ComputeNodeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(metric => new
        {
            metric.ComputeNodeId,
            metric.RecordedAt
        });
    }
}