using BeeCloud.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BeeCloud.Infrastructure.Persistence.Configurations;

public class NetworkConfiguration
    : IEntityTypeConfiguration<Network>
{
    public void Configure(
        EntityTypeBuilder<Network> builder)
    {
        builder.ToTable("networks");

        builder.HasKey(network => network.Id);

        builder.Property(network => network.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(network => network.Description)
            .HasMaxLength(500);

        builder.Property(network => network.CreatedAt)
            .IsRequired();

        builder.HasIndex(network => network.Name)
            .IsUnique();
    }
}