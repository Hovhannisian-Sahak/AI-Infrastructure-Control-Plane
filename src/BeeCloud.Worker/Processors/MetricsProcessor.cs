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
    private readonly IIncidentRepository _incidentRepository;
    private readonly IBeeCloudMetrics _beeCloudMetrics;
    public MetricsProcessor(
        IComputeNodeRepository nodeRepository,
        INodeMetricRepository metricRepository,
        IIncidentRepository incidentRepository,
        IBeeCloudMetrics beeCloudMetrics,
        ILogger<MetricsProcessor> logger)
    {
        _nodeRepository = nodeRepository;
        _metricRepository = metricRepository;
        _incidentRepository = incidentRepository;
        _beeCloudMetrics = beeCloudMetrics;
        _logger = logger;
    }

    public async Task ProcessAsync(
        CancellationToken cancellationToken = default)
    {
        var nodes = await _nodeRepository.GetAllAsync(
            cancellationToken);

        var availableNodes = nodes.Count(
            node => node.Status == NodeStatus.Available);

        var unhealthyNodes = nodes.Count(
            node => node.Status == NodeStatus.Unhealthy);

        _beeCloudMetrics.SetNodeCounts(
            nodes.Count,
            availableNodes,
            unhealthyNodes);

        var openIncidents = await _incidentRepository.GetAllAsync(
            status: IncidentStatus.Open,
            cancellationToken: cancellationToken);

        _beeCloudMetrics.SetOpenIncidentCount(
            openIncidents.Count);

        var runningNodes = nodes
            .Where(node => node.Status == NodeStatus.Running)
            .ToList();

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
        var (cpuUsage, gpuUsage, gpuTemperature) =
            GenerateMetrics(node);

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

        _logger.LogInformation(
            "Recorded metrics for node {NodeId}: CPU {CpuUsage}%, GPU {GpuUsage}%, temperature {GpuTemperature}°C.",
            node.Id,
            cpuUsage,
            gpuUsage,
            gpuTemperature);
    }

    private static (
        double CpuUsage,
        double GpuUsage,
        double GpuTemperature)
        GenerateMetrics(ComputeNode node)
    {
        var random = Random.Shared;

        var cpuUsage = random.NextDouble() * 100;
        var gpuUsage = random.NextDouble() * 100;

        var gpuTemperature = 40 + random.NextDouble() * 60;

        return (
            Math.Round(cpuUsage, 2),
            Math.Round(gpuUsage, 2),
            Math.Round(gpuTemperature, 2));
    }
}