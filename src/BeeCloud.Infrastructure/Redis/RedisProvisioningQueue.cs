using BeeCloud.Application.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace BeeCloud.Infrastructure.Redis;

public class RedisProvisioningQueue : IProvisioningQueue
{
    private const string QueueKey = "beecloud:provisioning:queue";

    private readonly IDistributedCache _cache;

    public RedisProvisioningQueue(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task EnqueueAsync(
        Guid nodeId,
        CancellationToken cancellationToken = default)
    {
        var existing = await GetQueueAsync(cancellationToken);

        existing.Add(nodeId);

        await SaveQueueAsync(
            existing,
            cancellationToken);
    }

    public async Task<Guid?> DequeueAsync(
        CancellationToken cancellationToken = default)
    {
        var queue = await GetQueueAsync(cancellationToken);

        if (queue.Count == 0)
            return null;

        var nodeId = queue[0];

        queue.RemoveAt(0);

        await SaveQueueAsync(
            queue,
            cancellationToken);

        return nodeId;
    }

    private async Task<List<Guid>> GetQueueAsync(
        CancellationToken cancellationToken)
    {
        var data = await _cache.GetStringAsync(
            QueueKey,
            cancellationToken);

        if (string.IsNullOrWhiteSpace(data))
            return [];

        return JsonSerializer.Deserialize<List<Guid>>(data)
               ?? [];
    }

    private async Task SaveQueueAsync(
        List<Guid> queue,
        CancellationToken cancellationToken)
    {
        var data = JsonSerializer.Serialize(queue);

        await _cache.SetStringAsync(
            QueueKey,
            data,
            new DistributedCacheEntryOptions(),
            cancellationToken);
    }
}