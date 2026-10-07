using BeeCloud.Application.DTOs.NodeMetrics;
using BeeCloud.Application.Interfaces;
using BeeCloud.Application.DTOs.Pagination;
using BeeCloud.Application.Pagination;

namespace BeeCloud.Application.Services;

public class NodeMetricService : INodeMetricService
{
    private readonly INodeMetricRepository _nodeMetricRepository;
    private readonly IComputeNodeRepository _computeNodeRepository;

    public NodeMetricService(
        INodeMetricRepository nodeMetricRepository,
        IComputeNodeRepository computeNodeRepository)
    {
        _nodeMetricRepository = nodeMetricRepository;
        _computeNodeRepository = computeNodeRepository;
    }

    public async Task<NodeMetricResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var metric = await _nodeMetricRepository.GetByIdAsync(
            id,
            cancellationToken);

        if (metric is null)
            return null;

        return MapToResponse(metric);
    }

    public async Task<IReadOnlyList<NodeMetricResponse>> GetByNodeIdAsync(
        Guid computeNodeId,
        CancellationToken cancellationToken = default)
    {
        var node = await _computeNodeRepository.GetByIdAsync(
            computeNodeId,
            cancellationToken);

        if (node is null)
        {
            throw new KeyNotFoundException(
                $"Compute node with id '{computeNodeId}' was not found.");
        }

        var metrics = await _nodeMetricRepository.GetByNodeIdAsync(
            computeNodeId,
            cancellationToken);

        return metrics.Select(MapToResponse).ToList();
    }

    public async Task<IReadOnlyList<NodeMetricResponse>> GetHistoryAsync(
        Guid computeNodeId,
        DateTime? from = null,
        DateTime? to = null,
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        if (limit <= 0 || limit > 1000)
        {
            throw new ArgumentOutOfRangeException(
                nameof(limit),
                "Limit must be between 1 and 1000.");
        }

        if (from.HasValue && to.HasValue && from > to)
        {
            throw new ArgumentException(
                "'from' must be earlier than or equal to 'to'.");
        }

        var node = await _computeNodeRepository.GetByIdAsync(
            computeNodeId,
            cancellationToken);

        if (node is null)
        {
            throw new KeyNotFoundException(
                $"Compute node with id '{computeNodeId}' was not found.");
        }

        var metrics = await _nodeMetricRepository.GetHistoryAsync(
            computeNodeId,
            from,
            to,
            limit,
            cancellationToken);

        return metrics
            .Select(MapToResponse)
            .ToList();
    }

    public async Task<CursorPageResponse<NodeMetricResponse>> GetHistoryPageAsync(
        Guid computeNodeId,
        DateTime? from,
        DateTime? to,
        string? cursor,
        bool previous,
        int limit,
        CancellationToken cancellationToken = default)
    {
        if (limit <= 0 || limit > 1000)
            throw new ArgumentOutOfRangeException(nameof(limit), "Limit must be between 1 and 1000.");
        if (from.HasValue && to.HasValue && from > to)
            throw new ArgumentException("'from' must be earlier than or equal to 'to'.");

        var node = await _computeNodeRepository.GetByIdAsync(computeNodeId, cancellationToken);
        if (node is null)
            throw new KeyNotFoundException($"Compute node with id '{computeNodeId}' was not found.");

        HistoryCursor? position = cursor is null ? null : HistoryCursorCodec.Decode(cursor);
        var records = await _nodeMetricRepository.GetHistoryPageAsync(
            computeNodeId, from, to, position, previous, limit + 1, cancellationToken);
        var hasMore = records.Count > limit;
        var page = records.Take(limit).ToList();
        if (previous)
            page.Reverse();

        string? nextCursor = null;
        string? previousCursor = null;
        if (page.Count > 0)
        {
            if (previous)
            {
                nextCursor = HistoryCursorCodec.Encode(page[^1].RecordedAt, page[^1].Id);
                if (hasMore)
                    previousCursor = HistoryCursorCodec.Encode(page[0].RecordedAt, page[0].Id);
            }
            else
            {
                if (hasMore)
                    nextCursor = HistoryCursorCodec.Encode(page[^1].RecordedAt, page[^1].Id);
                if (cursor is not null)
                    previousCursor = HistoryCursorCodec.Encode(page[0].RecordedAt, page[0].Id);
            }
        }

        return new CursorPageResponse<NodeMetricResponse>
        {
            Items = page.Select(MapToResponse).ToList(),
            NextCursor = nextCursor,
            PreviousCursor = previousCursor
        };
    }

    private static NodeMetricResponse MapToResponse(
        Domain.Entities.NodeMetric metric)
    {
        return new NodeMetricResponse
        {
            Id = metric.Id,
            ComputeNodeId = metric.ComputeNodeId,
            CpuUsagePercent = metric.CpuUsagePercent,
            GpuUsagePercent = metric.GpuUsagePercent,
            GpuTemperatureCelsius = metric.GpuTemperatureCelsius,
            RecordedAt = metric.RecordedAt
        };
    }
}