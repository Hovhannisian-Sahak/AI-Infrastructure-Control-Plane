using BeeCloud.Application.Interfaces;
using BeeCloud.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BeeCloud.Worker;

public class RemediationWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RemediationWorker> _logger;

    public RemediationWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<RemediationWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Remediation worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessUnhealthyNodesAsync(
                    stoppingToken);
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
                    "Error occurred while processing node remediation.");
            }

            await Task.Delay(
                TimeSpan.FromSeconds(10),
                stoppingToken);
        }

        _logger.LogInformation(
            "Remediation worker stopped.");
    }

    private async Task ProcessUnhealthyNodesAsync(
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();

        var repository =
            scope.ServiceProvider
                .GetRequiredService<IComputeNodeRepository>();

        var nodes = await repository.GetByStatusAsync(
            NodeStatus.Unhealthy,
            cancellationToken);

        foreach (var node in nodes)
        {
            try
            {
                _logger.LogWarning(
                    "Starting remediation for node {NodeId} ({NodeName}).",
                    node.Id,
                    node.Name);

                node.Quarantine();

                await repository.SaveChangesAsync(
                    cancellationToken);

                _logger.LogWarning(
                    "Node {NodeId} ({NodeName}) has been quarantined.",
                    node.Id,
                    node.Name);

                node.StartRemediation();

                await repository.SaveChangesAsync(
                    cancellationToken);

                _logger.LogInformation(
                    "Node {NodeId} ({NodeName}) is being remediated.",
                    node.Id,
                    node.Name);

                await SimulateRemediationAsync(
                    node,
                    cancellationToken);

                node.Recover();

                await repository.SaveChangesAsync(
                    cancellationToken);

                _logger.LogInformation(
                    "Node {NodeId} ({NodeName}) successfully recovered.",
                    node.Id,
                    node.Name);
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Failed to remediate node {NodeId}.",
                    node.Id);
            }
        }
    }

    private static async Task SimulateRemediationAsync(
        BeeCloud.Domain.Entities.ComputeNode node,
        CancellationToken cancellationToken)
    {
        await Task.Delay(
            TimeSpan.FromSeconds(3),
            cancellationToken);
    }
}