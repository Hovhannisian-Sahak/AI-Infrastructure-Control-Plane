using BeeCloud.Domain.Enums;

namespace BeeCloud.Domain.Exceptions;

public class InvalidNodeStateTransitionException : Exception
{
    public NodeStatus CurrentStatus { get; }
    public NodeStatus RequestedStatus { get; }

    public InvalidNodeStateTransitionException(
        NodeStatus currentStatus,
        NodeStatus requestedStatus)
        : base(
            $"Invalid node state transition: " +
            $"{currentStatus} -> {requestedStatus}.")
    {
        CurrentStatus = currentStatus;
        RequestedStatus = requestedStatus;
    }
}