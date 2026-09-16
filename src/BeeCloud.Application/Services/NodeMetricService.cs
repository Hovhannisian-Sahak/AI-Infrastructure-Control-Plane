using BeeCloud.Application.DTOs.NodeMetrics;
using BeeCloud.Application.Interfaces;

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

        return metrics
            .Select(MapToResponse)
            .ToList();
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