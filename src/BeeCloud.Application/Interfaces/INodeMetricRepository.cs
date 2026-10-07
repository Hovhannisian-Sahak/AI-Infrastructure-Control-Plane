using BeeCloud.Domain.Entities;

namespace BeeCloud.Application.Interfaces;

public interface INodeMetricRepository
{
    Task<NodeMetric?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NodeMetric>> GetByNodeIdAsync(
        Guid computeNodeId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NodeMetric>> GetHistoryAsync(
        Guid computeNodeId,
        DateTime? from = null,
        DateTime? to = null,
        int limit = 100,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        NodeMetric metric,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);
}