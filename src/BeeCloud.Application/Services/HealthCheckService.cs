using BeeCloud.Application.DTOs.Health;
using BeeCloud.Application.Interfaces;
using BeeCloud.Domain.Entities;
using BeeCloud.Application.DTOs.Pagination;
using BeeCloud.Application.Pagination;

namespace BeeCloud.Application.Services;

public class HealthCheckService : IHealthCheckService
{
    private readonly IComputeNodeRepository _computeNodeRepository;
    private readonly IHealthCheckRepository _healthCheckRepository;

    public HealthCheckService(
        IComputeNodeRepository computeNodeRepository,
        IHealthCheckRepository healthCheckRepository)
    {
        _computeNodeRepository = computeNodeRepository;
        _healthCheckRepository = healthCheckRepository;
    }

    public async Task<HealthCheckResponse> CreateAsync(
        Guid computeNodeId,
        HealthCheckRequest request,
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

        var healthCheck = new HealthCheck(
            computeNodeId,
            request.IsHealthy,
            request.CpuUsagePercent,
            request.GpuUsagePercent,
            request.GpuTemperatureCelsius);

        await _healthCheckRepository.AddAsync(
            healthCheck,
            cancellationToken);

        if (!request.IsHealthy)
        {
            node.MarkUnhealthy();
        }

        node.RecordHealthCheck(healthCheck.CheckedAt);

        await _healthCheckRepository.SaveChangesAsync(
            cancellationToken);

        await _computeNodeRepository.SaveChangesAsync(
            cancellationToken);

        return MapToResponse(healthCheck);
    }
    public async Task<HealthCheckResponse?> GetLatestAsync(
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

        var healthCheck =
            await _healthCheckRepository.GetLatestAsync(
                computeNodeId,
                cancellationToken);

        return healthCheck is null
            ? null
            : MapToResponse(healthCheck);
    }

    public async Task<IReadOnlyList<HealthCheckResponse>> GetHistoryAsync(
        Guid computeNodeId,
        DateTime? from = null,
        DateTime? to = null,
        int limit = 100,
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

        if (limit <= 0)
        {
            throw new ArgumentException(
                "Limit must be greater than zero.",
                nameof(limit));
        }

        if (limit > 1000)
        {
            throw new ArgumentException(
                "Limit cannot be greater than 1000.",
                nameof(limit));
        }

        if (from.HasValue && to.HasValue && from > to)
        {
            throw new ArgumentException(
                "'from' must be earlier than or equal to 'to'.");
        }

        var healthChecks =
            await _healthCheckRepository.GetHistoryAsync(
                computeNodeId,
                from,
                to,
                limit,
                cancellationToken);

        return healthChecks
            .Select(MapToResponse)
            .ToList();
    }

    public async Task<CursorPageResponse<HealthCheckResponse>> GetHistoryPageAsync(
        Guid computeNodeId,
        DateTime? from,
        DateTime? to,
        string? cursor,
        bool previous,
        int limit,
        CancellationToken cancellationToken = default)
    {
        ValidateHistoryQuery(from, to, limit);
        await EnsureNodeExistsAsync(computeNodeId, cancellationToken);

        HistoryCursor? position = cursor is null
            ? null
            : HistoryCursorCodec.Decode(cursor);
        var records = await _healthCheckRepository.GetHistoryPageAsync(
            computeNodeId,
            from,
            to,
            position,
            previous,
            limit + 1,
            cancellationToken);
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
                nextCursor = HistoryCursorCodec.Encode(page[^1].CheckedAt, page[^1].Id);
                if (hasMore)
                    previousCursor = HistoryCursorCodec.Encode(page[0].CheckedAt, page[0].Id);
            }
            else
            {
                if (hasMore)
                    nextCursor = HistoryCursorCodec.Encode(page[^1].CheckedAt, page[^1].Id);
                if (cursor is not null)
                    previousCursor = HistoryCursorCodec.Encode(page[0].CheckedAt, page[0].Id);
            }
        }

        return new CursorPageResponse<HealthCheckResponse>
        {
            Items = page.Select(MapToResponse).ToList(),
            NextCursor = nextCursor,
            PreviousCursor = previousCursor
        };
    }

    private async Task EnsureNodeExistsAsync(
        Guid computeNodeId,
        CancellationToken cancellationToken)
    {
        if (await _computeNodeRepository.GetByIdAsync(computeNodeId, cancellationToken) is null)
            throw new KeyNotFoundException($"Compute node with id '{computeNodeId}' was not found.");
    }

    private static void ValidateHistoryQuery(DateTime? from, DateTime? to, int limit)
    {
        if (limit <= 0 || limit > 1000)
            throw new ArgumentOutOfRangeException(nameof(limit), "Limit must be between 1 and 1000.");
        if (from.HasValue && to.HasValue && from > to)
            throw new ArgumentException("'from' must be earlier than or equal to 'to'.");
    }

    private static HealthCheckResponse MapToResponse(
        HealthCheck healthCheck)
    {
        return new HealthCheckResponse
        {
            Id = healthCheck.Id,
            ComputeNodeId = healthCheck.ComputeNodeId,
            IsHealthy = healthCheck.IsHealthy,
            CpuUsagePercent = healthCheck.CpuUsagePercent,
            GpuUsagePercent = healthCheck.GpuUsagePercent,
            GpuTemperatureCelsius =
                healthCheck.GpuTemperatureCelsius,
            CheckedAt = healthCheck.CheckedAt
        };
    }
}