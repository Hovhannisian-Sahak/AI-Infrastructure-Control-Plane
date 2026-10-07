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

    Task<IReadOnlyList<NodeMetricResponse>> GetHistoryAsync(
        Guid computeNodeId,
        DateTime? from = null,
        DateTime? to = null,
        int limit = 100,
        CancellationToken cancellationToken = default);
}