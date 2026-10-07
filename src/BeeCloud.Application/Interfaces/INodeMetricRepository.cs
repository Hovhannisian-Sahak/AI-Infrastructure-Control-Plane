using BeeCloud.Domain.Entities;
using BeeCloud.Application.Pagination;

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

    Task<IReadOnlyList<NodeMetric>> GetHistoryPageAsync(
        Guid computeNodeId,
        DateTime? from,
        DateTime? to,
        HistoryCursor? cursor,
        bool previous,
        int limit,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        NodeMetric metric,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);
}