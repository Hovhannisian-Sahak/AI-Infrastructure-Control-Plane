using BeeCloud.Application.Interfaces;
using BeeCloud.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace BeeCloud.Worker.Processors;

public class RestartProcessor : IRestartProcessor
{
    private readonly IComputeNodeRepository _nodeRepository;
    private readonly IRestartQueue _restartQueue;
    private readonly ILogger<RestartProcessor> _logger;

    public RestartProcessor(
        IComputeNodeRepository nodeRepository,
        IRestartQueue restartQueue,
        ILogger<RestartProcessor> logger)
    {
        _nodeRepository = nodeRepository;
        _restartQueue = restartQueue;
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
                nodeId = await _restartQueue.DequeueAsync(
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
                await RestartNodeAsync(
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
                    "Failed to restart node {NodeId}.",
                    nodeId.Value);
            }
        }
    }

    private async Task RestartNodeAsync(
        Guid nodeId,
        CancellationToken cancellationToken)
    {
        var node = await _nodeRepository.GetByIdAsync(
            nodeId,
            cancellationToken);

        if (node is null)
        {
            _logger.LogWarning(
                "Restart job references node {NodeId}, but the node was not found.",
                nodeId);

            return;
        }

        if (node.Status != NodeStatus.Stopping)
        {
            _logger.LogInformation(
                "Skipping restart for node {NodeId} because its current status is {Status}.",
                node.Id,
                node.Status);

            return;
        }

        _logger.LogInformation(
            "Restarting node {NodeId} ({NodeName}).",
            node.Id,
            node.Name);

        node.CompleteStopping();

        await _nodeRepository.SaveChangesAsync(
            cancellationToken);

        node.Start();

        await _nodeRepository.SaveChangesAsync(
            cancellationToken);

        _logger.LogInformation(
            "Node {NodeId} ({NodeName}) successfully restarted.",
            node.Id,
            node.Name);
    }
}