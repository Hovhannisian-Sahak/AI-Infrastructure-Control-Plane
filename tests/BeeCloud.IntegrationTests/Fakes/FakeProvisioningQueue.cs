using BeeCloud.Application.Interfaces;

namespace BeeCloud.IntegrationTests.Fakes;

public class FakeProvisioningQueue : IProvisioningQueue
{
    private readonly Queue<Guid> _queue = new();

    public Task EnqueueAsync(
        Guid nodeId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _queue.Enqueue(nodeId);

        return Task.CompletedTask;
    }

    public Task<Guid?> DequeueAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (_queue.Count == 0)
        {
            return Task.FromResult<Guid?>(null);
        }

        return Task.FromResult<Guid?>(_queue.Dequeue());
    }
}