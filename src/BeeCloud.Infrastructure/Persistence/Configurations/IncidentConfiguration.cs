using BeeCloud.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BeeCloud.Infrastructure.Persistence.Configurations;

public class IncidentConfiguration
    : IEntityTypeConfiguration<Incident>
{
    public void Configure(EntityTypeBuilder<Incident> builder)
    {
        builder.ToTable("incidents");

        builder.HasKey(incident => incident.Id);

        builder.Property(incident => incident.ComputeNodeId)
            .IsRequired();

        builder.Property(incident => incident.Severity)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(incident => incident.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(incident => incident.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(incident => incident.Description)
            .HasMaxLength(2000);

        builder.Property(incident => incident.CreatedAt)
            .IsRequired();

        builder.Property(incident => incident.UpdatedAt)
            .IsRequired();

        builder.HasIndex(incident => new
        {
            incident.ComputeNodeId,
            incident.Status
        });

        builder.HasIndex(incident => new
        {
            incident.Severity,
            incident.Status
        });

        builder.HasOne<ComputeNode>()
            .WithMany()
            .HasForeignKey(incident => incident.ComputeNodeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}