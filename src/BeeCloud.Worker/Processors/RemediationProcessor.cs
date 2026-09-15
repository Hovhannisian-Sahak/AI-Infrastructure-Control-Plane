using BeeCloud.Application.Interfaces;
using BeeCloud.Domain.Entities;
using BeeCloud.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace BeeCloud.Worker.Processors;

public class RemediationProcessor : IRemediationProcessor
{
    private readonly IComputeNodeRepository _nodeRepository;
    private readonly IIncidentService _incidentService;
    private readonly ILogger<RemediationProcessor> _logger;

    public RemediationProcessor(
        IComputeNodeRepository nodeRepository,
        IIncidentService incidentService,
        ILogger<RemediationProcessor> logger)
    {
        _nodeRepository = nodeRepository;
        _incidentService = incidentService;
        _logger = logger;
    }

    public async Task ProcessAsync(
        CancellationToken cancellationToken = default)
    {
        await ProcessUnhealthyNodesAsync(cancellationToken);
        await ProcessQuarantinedNodesAsync(cancellationToken);
    }

    private async Task ProcessUnhealthyNodesAsync(
        CancellationToken cancellationToken)
    {
        var nodes = await _nodeRepository.GetByStatusAsync(
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

                await _nodeRepository.SaveChangesAsync(
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
        var nodes = await _nodeRepository.GetByStatusAsync(
            NodeStatus.Quarantined,
            cancellationToken);

        foreach (var node in nodes)
        {
            try
            {
                await RemediateNodeAsync(
                    node,
                    cancellationToken);
            }
            catch (Exception exception)
            {
                await HandleRemediationFailureAsync(
                    node,
                    exception,
                    cancellationToken);
            }
        }
    }

    private async Task RemediateNodeAsync(
        ComputeNode node,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Starting remediation for node {NodeId} ({NodeName}).",
            node.Id,
            node.Name);

        node.StartRemediation();

        await _nodeRepository.SaveChangesAsync(
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

        await _nodeRepository.SaveChangesAsync(
            cancellationToken);

        await _incidentService.ResolveForNodeAsync(
            node.Id,
            cancellationToken);

        _logger.LogInformation(
            "Node {NodeId} ({NodeName}) successfully recovered.",
            node.Id,
            node.Name);
    }

    private async Task HandleRemediationFailureAsync(
        ComputeNode node,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(
            exception,
            "Remediation failed for node {NodeId} ({NodeName}).",
            node.Id,
            node.Name);

        try
        {
            node.MarkFailed();

            await _nodeRepository.SaveChangesAsync(
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

    private static async Task SimulateRemediationAsync(
        ComputeNode node,
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