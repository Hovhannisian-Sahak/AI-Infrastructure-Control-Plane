using BeeCloud.Application.DTOs.NodeMetrics;

namespace BeeCloud.Application.Interfaces;

public interface INodeMetricService
{
    Task<NodeMetricResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NodeMetricResponse>> GetByNodeIdAsync(
        Guid computeNodeId,
        CancellationToken cancellationToken = default);
}