using BeeCloud.Application.Interfaces;
using BeeCloud.Domain.Entities;
using BeeCloud.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace BeeCloud.Worker.Processors;

public class HealthMonitoringProcessor : IHealthMonitoringProcessor
{
    private readonly IComputeNodeRepository _nodeRepository;
    private readonly IHealthCheckRepository _healthCheckRepository;
    private readonly IIncidentService _incidentService;
    private readonly ILogger<HealthMonitoringProcessor> _logger;

    public HealthMonitoringProcessor(
        IComputeNodeRepository nodeRepository,
        IHealthCheckRepository healthCheckRepository,
        IIncidentService incidentService,
        ILogger<HealthMonitoringProcessor> logger)
    {
        _nodeRepository = nodeRepository;
        _healthCheckRepository = healthCheckRepository;
        _incidentService = incidentService;
        _logger = logger;
    }

    public async Task ProcessAsync(
        CancellationToken cancellationToken = default)
    {
        var nodes = await _nodeRepository.GetByStatusAsync(
            NodeStatus.Running,
            cancellationToken);

        foreach (var node in nodes)
        {
            try
            {
                await ProcessNodeAsync(
                    node,
                    cancellationToken);
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Failed to monitor node {NodeId}.",
                    node.Id);
            }
        }
    }

    private async Task ProcessNodeAsync(
        ComputeNode node,
        CancellationToken cancellationToken)
    {
        var healthCheck = CreateSimulatedHealthCheck(node);

        await _healthCheckRepository.AddAsync(
            healthCheck,
            cancellationToken);

        if (!healthCheck.IsHealthy)
        {
            node.MarkUnhealthy();

            _logger.LogWarning(
                "Node {NodeId} ({NodeName}) is unhealthy.",
                node.Id,
                node.Name);

            await _incidentService.CreateForUnhealthyNodeAsync(
                node,
                healthCheck,
                cancellationToken);
        }
        else
        {
            _logger.LogInformation(
                "Node {NodeId} ({NodeName}) is healthy.",
                node.Id,
                node.Name);
        }

        node.RecordHealthCheck(
            healthCheck.CheckedAt);

        await _healthCheckRepository.SaveChangesAsync(
            cancellationToken);

        await _nodeRepository.SaveChangesAsync(
            cancellationToken);
    }

    private static HealthCheck CreateSimulatedHealthCheck(
        ComputeNode node)
    {
        if (node.ActiveFault == NodeFault.GpuOverheat)
        {
            return new HealthCheck(
                node.Id,
                isHealthy: false,
                cpuUsagePercent: 65,
                gpuUsagePercent: 95,
                gpuTemperatureCelsius: 105);
        }

        if (node.ActiveFault == NodeFault.GpuFailure)
        {
            return new HealthCheck(
                node.Id,
                isHealthy: false,
                cpuUsagePercent: 40,
                gpuUsagePercent: 0,
                gpuTemperatureCelsius: 45);
        }

        if (node.ActiveFault == NodeFault.NetworkFailure)
        {
            return new HealthCheck(
                node.Id,
                isHealthy: false,
                cpuUsagePercent: 40,
                gpuUsagePercent: 50,
                gpuTemperatureCelsius: 60);
        }

        if (node.ActiveFault == NodeFault.ServiceCrash)
        {
            return new HealthCheck(
                node.Id,
                isHealthy: false,
                cpuUsagePercent: 0,
                gpuUsagePercent: 0,
                gpuTemperatureCelsius: 40);
        }

        var random = Random.Shared;

        var cpuUsage = random.NextDouble() * 100;
        var gpuUsage = random.NextDouble() * 100;
        var gpuTemperature = 50 + random.NextDouble() * 50;

        var isHealthy =
            cpuUsage < 90 &&
            gpuTemperature < 90;

        return new HealthCheck(
            node.Id,
            isHealthy,
            cpuUsage,
            gpuUsage,
            gpuTemperature);
    }
}