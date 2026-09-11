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

        builder.HasKey(attachment => attachment.Id);

        builder.Property(attachment => attachment.ComputeNodeId)
            .IsRequired();

        builder.Property(attachment => attachment.NetworkId)
            .IsRequired();

        builder.Property(attachment => attachment.AttachedAt)
            .IsRequired();

        builder.HasIndex(attachment =>
                new
                {
                    attachment.ComputeNodeId,
                    attachment.NetworkId
                })
            .IsUnique();

        builder.HasOne<ComputeNode>()
            .WithMany()
            .HasForeignKey(attachment => attachment.ComputeNodeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Network>()
            .WithMany()
            .HasForeignKey(attachment => attachment.NetworkId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}