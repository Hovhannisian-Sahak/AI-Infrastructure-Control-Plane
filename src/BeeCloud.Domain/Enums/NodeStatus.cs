namespace BeeCloud.Domain.Enums;

public enum NodeStatus
{
    Provisioning,
    Available,
    Running,
    Stopping,
    Stopped,
    Unhealthy,
    Quarantined,
    Remediating,
    Failed
}