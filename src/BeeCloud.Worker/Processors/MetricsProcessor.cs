using BeeCloud.Application.Interfaces;
using BeeCloud.Domain.Entities;
using BeeCloud.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace BeeCloud.Worker.Processors;

public class MetricsProcessor : IMetricsProcessor
{
    private readonly IComputeNodeRepository _nodeRepository;
    private readonly INodeMetricRepository _metricRepository;
    private readonly ILogger<MetricsProcessor> _logger;

    public MetricsProcessor(
        IComputeNodeRepository nodeRepository,
        INodeMetricRepository metricRepository,
        ILogger<MetricsProcessor> logger)
    {
        _nodeRepository = nodeRepository;
        _metricRepository = metricRepository;
        _logger = logger;
    }

    public async Task ProcessAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var runningNodes =
            await _nodeRepository.GetByStatusAsync(
                NodeStatus.Running,
                cancellationToken);

        foreach (var node in runningNodes)
        {
            try
            {
                await RecordMetricAsync(
                    node,
                    cancellationToken);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Failed to record metrics for node {NodeId}.",
                    node.Id);
            }
        }
    }

    private async Task RecordMetricAsync(
        ComputeNode node,
        CancellationToken cancellationToken)
    {
        var cpuUsage =
            Math.Round(
                Random.Shared.NextDouble() * 100,
                2);

        var gpuUsage =
            Math.Round(
                Random.Shared.NextDouble() * 100,
                2);

        var gpuTemperature =
            Math.Round(
                40 + Random.Shared.NextDouble() * 60,
                2);

        var metric = new NodeMetric(
            node.Id,
            cpuUsage,
            gpuUsage,
            gpuTemperature);

        await _metricRepository.AddAsync(
            metric,
            cancellationToken);

        await _metricRepository.SaveChangesAsync(
            cancellationToken);
    }
}