namespace BeeCloud.Application.DTOs.Networks;

public class NetworkAttachmentResponse
{
    public Guid Id { get; set; }

    public Guid ComputeNodeId { get; set; }

    public Guid NetworkId { get; set; }

    public DateTime AttachedAt { get; set; }
}