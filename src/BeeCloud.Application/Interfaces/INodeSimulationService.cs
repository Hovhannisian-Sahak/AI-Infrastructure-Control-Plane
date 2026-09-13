using BeeCloud.Application.DTOs.ComputeNodes;
using BeeCloud.Domain.Enums;

namespace BeeCloud.Application.Interfaces;

public interface INodeSimulationService
{
    Task<ComputeNodeResponse> SimulateFaultAsync(
        Guid nodeId,
        NodeFault fault,
        CancellationToken cancellationToken = default);

    Task<ComputeNodeResponse> ClearFaultAsync(
        Guid nodeId,
        CancellationToken cancellationToken = default);
}