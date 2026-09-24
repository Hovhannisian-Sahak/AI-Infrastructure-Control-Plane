using BeeCloud.Application.Interfaces;
using BeeCloud.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace BeeCloud.Worker.Processors;

public class ProvisioningProcessor : IProvisioningProcessor
{
    private readonly IComputeNodeRepository _nodeRepository;
    private readonly IProvisioningQueue _provisioningQueue;
    private readonly IOperationalMetricsService _operationalMetricsService;
    private readonly ILogger<ProvisioningProcessor> _logger;

    public ProvisioningProcessor(
        IComputeNodeRepository nodeRepository,
        IProvisioningQueue provisioningQueue,
        IOperationalMetricsService operationalMetricsService,
        ILogger<ProvisioningProcessor> logger)
    {
        _nodeRepository = nodeRepository;
        _provisioningQueue = provisioningQueue;
        _operationalMetricsService = operationalMetricsService;
        _logger = logger;
    }

    public async Task ProcessAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        while (!cancellationToken.IsCancellationRequested)
        {
            Guid? nodeId;

            try
            {
                nodeId = await _provisioningQueue.DequeueAsync(
                    cancellationToken);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }

            if (nodeId is null)
            {
                break;
            }

            try
            {
                await ProvisionNodeAsync(
                    nodeId.Value,
                    cancellationToken);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                await RecordProvisioningFailureAsync(
                    cancellationToken);

                _logger.LogError(
                    exception,
                    "Failed to provision node {NodeId}.",
                    nodeId.Value);
            }
        }
    }

    private async Task ProvisionNodeAsync(
        Guid nodeId,
        CancellationToken cancellationToken)
    {
        var node = await _nodeRepository.GetByIdAsync(
            nodeId,
            cancellationToken);

        if (node is null)
        {
            _logger.LogWarning(
                "Provisioning job references node {NodeId}, but the node was not found.",
                nodeId);

            return;
        }

        if (node.Status != NodeStatus.Provisioning)
        {
            _logger.LogInformation(
                "Skipping provisioning for node {NodeId} because its current status is {Status}.",
                node.Id,
                node.Status);

            return;
        }

        _logger.LogInformation(
            "Starting provisioning for node {NodeId} ({NodeName}).",
            node.Id,
            node.Name);

        await SimulateProvisioningAsync(
            cancellationToken);

        node.MarkAvailable();

        await _nodeRepository.SaveChangesAsync(
            cancellationToken);

        await _operationalMetricsService
            .IncrementProvisioningAsync(
                cancellationToken);

        _logger.LogInformation(
            "Node {NodeId} ({NodeName}) successfully provisioned.",
            node.Id,
            node.Name);
    }

    private async Task RecordProvisioningFailureAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            await _operationalMetricsService
                .IncrementProvisioningFailureAsync(
                    cancellationToken);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception metricsException)
        {
            _logger.LogError(
                metricsException,
                "Failed to record provisioning failure metric.");
        }
    }

    private static async Task SimulateProvisioningAsync(
        CancellationToken cancellationToken)
    {
        await Task.Delay(
            TimeSpan.FromSeconds(2),
            cancellationToken);
    }
}