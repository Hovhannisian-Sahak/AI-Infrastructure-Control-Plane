namespace BeeCloud.Domain.Entities;

public class NetworkAttachment
{
    public Guid Id { get; private set; }

    public Guid ComputeNodeId { get; private set; }

    public Guid NetworkId { get; private set; }

    public DateTime AttachedAt { get; private set; }

    private NetworkAttachment()
    {
        // Required by EF Core.
    }

    public NetworkAttachment(
        Guid computeNodeId,
        Guid networkId)
    {
        if (computeNodeId == Guid.Empty)
        {
            throw new ArgumentException(
                "Compute node ID cannot be empty.",
                nameof(computeNodeId));
        }

        if (networkId == Guid.Empty)
        {
            throw new ArgumentException(
                "Network ID cannot be empty.",
                nameof(networkId));
        }

        Id = Guid.NewGuid();

        ComputeNodeId = computeNodeId;
        NetworkId = networkId;

        AttachedAt = DateTime.UtcNow;
    }
}