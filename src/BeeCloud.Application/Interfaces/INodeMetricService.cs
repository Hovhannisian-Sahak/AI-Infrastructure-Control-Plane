using BeeCloud.Application.DTOs.NodeMetrics;
using BeeCloud.Application.DTOs.Pagination;

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

    Task<CursorPageResponse<NodeMetricResponse>> GetHistoryPageAsync(
        Guid computeNodeId,
        DateTime? from,
        DateTime? to,
        string? cursor,
        bool previous,
        int limit,
        CancellationToken cancellationToken = default);
}