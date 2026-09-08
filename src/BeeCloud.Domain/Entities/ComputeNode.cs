using BeeCloud.Domain.Enums;
using BeeCloud.Domain.Exceptions;
using BeeCloud.Domain.Enums;
using BeeCloud.Domain.Exceptions;

namespace BeeCloud.Domain.Entities;

public class ComputeNode
{
    public Guid Id { get; private set; }

    public string Name { get; private set; }

    public string GpuModel { get; private set; }

    public int GpuCount { get; private set; }

    public NodeStatus Status { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    public DateTime? LastHealthCheck { get; private set; }

    private ComputeNode()
    {
        // Required by EF Core later.
    }

    public ComputeNode(
        string name,
        string gpuModel,
        int gpuCount)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException(
                "Node name cannot be empty.",
                nameof(name));

        if (string.IsNullOrWhiteSpace(gpuModel))
            throw new ArgumentException(
                "GPU model cannot be empty.",
                nameof(gpuModel));

        if (gpuCount <= 0)
            throw new ArgumentException(
                "GPU count must be greater than zero.",
                nameof(gpuCount));

        Id = Guid.NewGuid();
        Name = name;
        GpuModel = gpuModel;
        GpuCount = gpuCount;

        Status = NodeStatus.Provisioning;

        CreatedAt = DateTime.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public void MarkAvailable()
    {
        TransitionTo(NodeStatus.Available);
    }

    public void Start()
    {
        TransitionTo(NodeStatus.Running);
    }

    public void Stop()
    {
        TransitionTo(NodeStatus.Stopping);
    }

    public void CompleteStopping()
    {
        TransitionTo(NodeStatus.Stopped);
    }

    public void MarkUnhealthy()
    {
        TransitionTo(NodeStatus.Unhealthy);
    }

    public void Quarantine()
    {
        TransitionTo(NodeStatus.Quarantined);
    }

    public void StartRemediation()
    {
        TransitionTo(NodeStatus.Remediating);
    }

    public void Recover()
    {
        TransitionTo(NodeStatus.Available);
    }

    public void MarkFailed()
    {
        TransitionTo(NodeStatus.Failed);
    }

    public void RecordHealthCheck(DateTime timestamp)
    {
        LastHealthCheck = timestamp;
        UpdatedAt = DateTime.UtcNow;
    }

    private void TransitionTo(NodeStatus newStatus)
    {
        if (!IsValidTransition(Status, newStatus))
        {
            throw new InvalidNodeStateTransitionException(
                Status,
                newStatus);
        }

        Status = newStatus;
        UpdatedAt = DateTime.UtcNow;
    }

    private static bool IsValidTransition(
        NodeStatus current,
        NodeStatus next)
    {
        return (current, next) switch
        {
            (NodeStatus.Provisioning, NodeStatus.Available)
                => true,

            (NodeStatus.Available, NodeStatus.Running)
                => true,

            (NodeStatus.Running, NodeStatus.Stopping)
                => true,

            (NodeStatus.Stopping, NodeStatus.Stopped)
                => true,

            (NodeStatus.Running, NodeStatus.Unhealthy)
                => true,

            (NodeStatus.Unhealthy, NodeStatus.Quarantined)
                => true,

            (NodeStatus.Quarantined, NodeStatus.Remediating)
                => true,

            (NodeStatus.Remediating, NodeStatus.Available)
                => true,

            (NodeStatus.Quarantined, NodeStatus.Failed)
                => true,

            _ => false
        };
    }
}