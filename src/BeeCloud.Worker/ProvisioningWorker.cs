using BeeCloud.Application.Interfaces;
using BeeCloud.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BeeCloud.Worker;

public class ProvisioningWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ProvisioningWorker> _logger;

    public ProvisioningWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<ProvisioningWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Provisioning worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessProvisioningNodesAsync(
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
                    "Error occurred while processing provisioning nodes.");
            }

            await Task.Delay(
                TimeSpan.FromSeconds(5),
                stoppingToken);
        }

        _logger.LogInformation(
            "Provisioning worker stopped.");
    }

    private async Task ProcessProvisioningNodesAsync(
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();

        var repository =
            scope.ServiceProvider
                .GetRequiredService<IComputeNodeRepository>();

        var nodes = await repository.GetByStatusAsync(
            NodeStatus.Provisioning,
            cancellationToken);

        foreach (var node in nodes)
        {
            try
            {
                _logger.LogInformation(
                    "Provisioning node {NodeId} ({NodeName}).",
                    node.Id,
                    node.Name);

                await ProvisionNodeAsync(
                    node,
                    cancellationToken);

                node.MarkAvailable();

                await repository.SaveChangesAsync(
                    cancellationToken);

                _logger.LogInformation(
                    "Node {NodeId} ({NodeName}) is now available.",
                    node.Id,
                    node.Name);
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Failed to provision node {NodeId}.",
                    node.Id);
            }
        }
    }

    private static async Task ProvisionNodeAsync(
        BeeCloud.Domain.Entities.ComputeNode node,
        CancellationToken cancellationToken)
    {
        // Simulate an asynchronous infrastructure operation.
        await Task.Delay(
            TimeSpan.FromSeconds(2),
            cancellationToken);
    }
}