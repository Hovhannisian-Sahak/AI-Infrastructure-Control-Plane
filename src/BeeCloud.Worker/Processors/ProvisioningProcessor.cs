using BeeCloud.Application.Interfaces;
using BeeCloud.Domain.Entities;
using BeeCloud.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace BeeCloud.Worker.Processors;

public class ProvisioningProcessor : IProvisioningProcessor
{
    private readonly IComputeNodeRepository _nodeRepository;
    private readonly ILogger<ProvisioningProcessor> _logger;

    public ProvisioningProcessor(
        IComputeNodeRepository nodeRepository,
        ILogger<ProvisioningProcessor> logger)
    {
        _nodeRepository = nodeRepository;
        _logger = logger;
    }

    public async Task ProcessAsync(
        CancellationToken cancellationToken = default)
    {
        var nodes = await _nodeRepository.GetByStatusAsync(
            NodeStatus.Provisioning,
            cancellationToken);

        foreach (var node in nodes)
        {
            try
            {
                await ProvisionNodeAsync(
                    node,
                    cancellationToken);
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

    private async Task ProvisionNodeAsync(
        ComputeNode node,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Starting provisioning for node {NodeId} ({NodeName}).",
            node.Id,
            node.Name);

        await SimulateProvisioningAsync(
            cancellationToken);

        node.MarkAvailable();

        await _nodeRepository.SaveChangesAsync(
            cancellationToken);

        _logger.LogInformation(
            "Node {NodeId} ({NodeName}) successfully provisioned.",
            node.Id,
            node.Name);
    }

    private static async Task SimulateProvisioningAsync(
        CancellationToken cancellationToken)
    {
        await Task.Delay(
            TimeSpan.FromSeconds(2),
            cancellationToken);
    }
}