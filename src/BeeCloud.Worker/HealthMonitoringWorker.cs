using BeeCloud.Application.Interfaces;
using BeeCloud.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BeeCloud.Worker;

public class HealthMonitoringWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<HealthMonitoringWorker> _logger;

    public HealthMonitoringWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<HealthMonitoringWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Health monitoring worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await MonitorNodesAsync(stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Error occurred while monitoring node health.");
            }

            await Task.Delay(
                TimeSpan.FromSeconds(10),
                stoppingToken);
        }

        _logger.LogInformation(
            "Health monitoring worker stopped.");
    }

    private async Task MonitorNodesAsync(
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();

        var nodeRepository =
            scope.ServiceProvider
                .GetRequiredService<IComputeNodeRepository>();

        var healthCheckRepository =
            scope.ServiceProvider
                .GetRequiredService<IHealthCheckRepository>();

        var nodes = await nodeRepository.GetByStatusAsync(
            NodeStatus.Running,
            cancellationToken);

        foreach (var node in nodes)
        {
            try
            {
                var healthCheck = CreateSimulatedHealthCheck(node);

                await healthCheckRepository.AddAsync(
                    healthCheck,
                    cancellationToken);

                if (!healthCheck.IsHealthy)
                {
                    node.MarkUnhealthy();

                    _logger.LogWarning(
                        "Node {NodeId} ({NodeName}) is unhealthy.",
                        node.Id,
                        node.Name);
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

                await healthCheckRepository.SaveChangesAsync(
                    cancellationToken);

                await nodeRepository.SaveChangesAsync(
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

    private static BeeCloud.Domain.Entities.HealthCheck
        CreateSimulatedHealthCheck(
            BeeCloud.Domain.Entities.ComputeNode node)
    {
        if (node.ActiveFault == NodeFault.GpuOverheat)
        {
            return new BeeCloud.Domain.Entities.HealthCheck(
                node.Id,
                isHealthy: false,
                cpuUsagePercent: 65,
                gpuUsagePercent: 95,
                gpuTemperatureCelsius: 105);
        }

        if (node.ActiveFault == NodeFault.GpuFailure)
        {
            return new BeeCloud.Domain.Entities.HealthCheck(
                node.Id,
                isHealthy: false,
                cpuUsagePercent: 40,
                gpuUsagePercent: 0,
                gpuTemperatureCelsius: 45);
        }

        if (node.ActiveFault == NodeFault.NetworkFailure)
        {
            return new BeeCloud.Domain.Entities.HealthCheck(
                node.Id,
                isHealthy: false,
                cpuUsagePercent: 40,
                gpuUsagePercent: 50,
                gpuTemperatureCelsius: 60);
        }

        if (node.ActiveFault == NodeFault.ServiceCrash)
        {
            return new BeeCloud.Domain.Entities.HealthCheck(
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

        return new BeeCloud.Domain.Entities.HealthCheck(
            node.Id,
            isHealthy,
            cpuUsage,
            gpuUsage,
            gpuTemperature);
    }
}