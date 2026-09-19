using BeeCloud.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BeeCloud.Infrastructure.Persistence.Configurations;

public class NetworkAttachmentConfiguration
    : IEntityTypeConfiguration<NetworkAttachment>
{
    public void Configure(
        EntityTypeBuilder<NetworkAttachment> builder)
    {
        builder.ToTable("network_attachments");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ComputeNodeId)
            .IsRequired();

        builder.Property(x => x.NetworkId)
            .IsRequired();

        builder.Property(x => x.AttachedAt)
            .IsRequired();

        builder.HasIndex(x => new
            {
                x.ComputeNodeId,
                x.NetworkId
            })
            .IsUnique();

        builder.HasOne<ComputeNode>()
            .WithMany()
            .HasForeignKey(x => x.ComputeNodeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Network>()
            .WithMany()
            .HasForeignKey(x => x.NetworkId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}