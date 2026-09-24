using BeeCloud.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BeeCloud.Infrastructure.Persistence.Configurations;

public class OperationalMetricConfiguration
    : IEntityTypeConfiguration<OperationalMetric>
{
    public void Configure(
        EntityTypeBuilder<OperationalMetric> builder)
    {
        builder.ToTable("operational_metrics");

        builder.HasKey(metric => metric.Id);

        builder.Property(metric => metric.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(metric => metric.Name)
            .IsUnique();

        builder.Property(metric => metric.Value)
            .IsRequired();

        builder.Property(metric => metric.UpdatedAt)
            .IsRequired();
    }
}