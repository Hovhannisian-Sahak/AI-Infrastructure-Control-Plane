using BeeCloud.Application.Interfaces;
using BeeCloud.Domain.Enums;

namespace BeeCloud.Api.Observability;

public class BeeCloudMetricsCollector : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IBeeCloudMetrics _beeCloudMetrics;
    private readonly ILogger<BeeCloudMetricsCollector> _logger;

    public BeeCloudMetricsCollector(
        IServiceScopeFactory scopeFactory,
        IBeeCloudMetrics beeCloudMetrics,
        ILogger<BeeCloudMetricsCollector> logger)
    {
        _scopeFactory = scopeFactory;
        _beeCloudMetrics = beeCloudMetrics;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CollectAsync(stoppingToken);
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
                    "Failed to collect BeeCloud metrics.");
            }

            await Task.Delay(
                TimeSpan.FromSeconds(5),
                stoppingToken);
        }
    }

    private async Task CollectAsync(
        CancellationToken cancellationToken)
    {
        using var scope =
            _scopeFactory.CreateScope();

        var nodeRepository =
            scope.ServiceProvider
                .GetRequiredService<IComputeNodeRepository>();

        var incidentRepository =
            scope.ServiceProvider
                .GetRequiredService<IIncidentRepository>();

        var nodes = await nodeRepository.GetAllAsync(
            cancellationToken);

        var availableNodes = nodes.Count(
            node => node.Status == NodeStatus.Available);

        var unhealthyNodes = nodes.Count(
            node => node.Status == NodeStatus.Unhealthy);

        _beeCloudMetrics.SetNodeCounts(
            nodes.Count,
            availableNodes,
            unhealthyNodes);

        var openIncidents =
            await incidentRepository.GetAllAsync(
                status: IncidentStatus.Open,
                cancellationToken: cancellationToken);

        _beeCloudMetrics.SetOpenIncidentCount(
            openIncidents.Count);
    }
}