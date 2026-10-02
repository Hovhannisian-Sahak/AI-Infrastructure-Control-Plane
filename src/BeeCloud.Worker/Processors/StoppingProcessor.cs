using BeeCloud.Application.Interfaces;
using BeeCloud.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace BeeCloud.Worker.Processors;

public class StoppingProcessor : IStoppingProcessor
{
    private readonly IComputeNodeRepository _nodeRepository;
    private readonly IStoppingQueue _stoppingQueue;
    private readonly ILogger<StoppingProcessor> _logger;

    public StoppingProcessor(
        IComputeNodeRepository nodeRepository,
        IStoppingQueue stoppingQueue,
        ILogger<StoppingProcessor> logger)
    {
        _nodeRepository = nodeRepository;
        _stoppingQueue = stoppingQueue;
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
                nodeId = await _stoppingQueue.DequeueAsync(
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
                await StopNodeAsync(
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
                _logger.LogError(
                    exception,
                    "Failed to complete stopping for node {NodeId}.",
                    nodeId.Value);
            }
        }
    }

    private async Task StopNodeAsync(
        Guid nodeId,
        CancellationToken cancellationToken)
    {
        var node = await _nodeRepository.GetByIdAsync(
            nodeId,
            cancellationToken);

        if (node is null)
        {
            _logger.LogWarning(
                "Stopping job references node {NodeId}, but the node was not found.",
                nodeId);

            return;
        }

        if (node.Status != NodeStatus.Stopping)
        {
            _logger.LogInformation(
                "Skipping stopping for node {NodeId} because its current status is {Status}.",
                node.Id,
                node.Status);

            return;
        }

        _logger.LogInformation(
            "Completing stopping for node {NodeId} ({NodeName}).",
            node.Id,
            node.Name);

        node.CompleteStopping();

        await _nodeRepository.SaveChangesAsync(
            cancellationToken);

        _logger.LogInformation(
            "Node {NodeId} ({NodeName}) successfully stopped.",
            node.Id,
            node.Name);
    }
}