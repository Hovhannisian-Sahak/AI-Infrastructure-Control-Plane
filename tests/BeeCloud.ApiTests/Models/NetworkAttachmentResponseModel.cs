namespace BeeCloud.ApiTests.Models;

public class NetworkAttachmentResponseModel
{
    public Guid Id { get; set; }

    public Guid ComputeNodeId { get; set; }

    public Guid NetworkId { get; set; }

    public DateTime AttachedAt { get; set; }
}