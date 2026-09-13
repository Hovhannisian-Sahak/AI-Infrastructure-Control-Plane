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

                await ProcessQuarantinedNodesAsync(
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
                    "Quarantining unhealthy node {NodeId} ({NodeName}).",
                    node.Id,
                    node.Name);

                node.Quarantine();

                await repository.SaveChangesAsync(
                    cancellationToken);

                _logger.LogWarning(
                    "Node {NodeId} ({NodeName}) is now quarantined.",
                    node.Id,
                    node.Name);
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Failed to quarantine node {NodeId}.",
                    node.Id);
            }
        }
    }

   private async Task ProcessQuarantinedNodesAsync(
    CancellationToken cancellationToken)
{
    using var scope = _scopeFactory.CreateScope();

    var repository =
        scope.ServiceProvider
            .GetRequiredService<IComputeNodeRepository>();

    var nodes = await repository.GetByStatusAsync(
        NodeStatus.Quarantined,
        cancellationToken);

    foreach (var node in nodes)
    {
        try
        {
            _logger.LogInformation(
                "Starting remediation for node {NodeId} ({NodeName}).",
                node.Id,
                node.Name);

            node.StartRemediation();

            await repository.SaveChangesAsync(
                cancellationToken);

            _logger.LogInformation(
                "Node {NodeId} ({NodeName}) is now being remediated.",
                node.Id,
                node.Name);

            await SimulateRemediationAsync(
                node,
                cancellationToken);

            node.ClearFault();
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
                "Remediation failed for node {NodeId} ({NodeName}).",
                node.Id,
                node.Name);

            try
            {
                node.MarkFailed();

                await repository.SaveChangesAsync(
                    cancellationToken);

                _logger.LogError(
                    "Node {NodeId} ({NodeName}) has been marked as Failed.",
                    node.Id,
                    node.Name);
            }
            catch (Exception failureException)
            {
                _logger.LogError(
                    failureException,
                    "Failed to mark node {NodeId} as Failed.",
                    node.Id);
            }
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

    if (node.ActiveFault == NodeFault.ServiceCrash)
    {
        throw new InvalidOperationException(
            "Simulated remediation failure: service crash could not be recovered.");
    }
}
}