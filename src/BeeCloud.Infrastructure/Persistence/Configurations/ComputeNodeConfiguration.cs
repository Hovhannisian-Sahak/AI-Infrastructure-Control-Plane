using BeeCloud.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BeeCloud.Infrastructure.Persistence.Configurations;

public class ComputeNodeConfiguration
    : IEntityTypeConfiguration<ComputeNode>
{
    public void Configure(
        EntityTypeBuilder<ComputeNode> builder)
    {
        builder.ToTable("compute_nodes");

        builder.HasKey(node => node.Id);

        builder.Property(node => node.Id)
            .ValueGeneratedNever();

        builder.Property(node => node.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(node => node.GpuModel)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(node => node.GpuCount)
            .IsRequired();

        builder.Property(node => node.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(node => node.CreatedAt)
            .IsRequired();

        builder.Property(node => node.UpdatedAt)
            .IsRequired();

        builder.Property(node => node.LastHealthCheck)
            .IsRequired(false);

        builder.HasIndex(node => node.Name)
            .IsUnique();
    }
}