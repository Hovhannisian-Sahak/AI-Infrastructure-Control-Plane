using BeeCloud.Application.Interfaces;
using StackExchange.Redis;

namespace BeeCloud.Infrastructure.Redis;

public class RedisStoppingQueue : IStoppingQueue
{
    private const string QueueKey =
        "beecloud:stopping:queue";

    private readonly IConnectionMultiplexer _redis;

    public RedisStoppingQueue(
        IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task EnqueueAsync(
        Guid nodeId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var database =
            _redis.GetDatabase();

        await database.ListRightPushAsync(
            QueueKey,
            nodeId.ToString());
    }

    public async Task<Guid?> DequeueAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var database =
            _redis.GetDatabase();

        var value =
            await database.ListLeftPopAsync(
                QueueKey);

        if (value.IsNullOrEmpty)
        {
            return null;
        }

        if (!Guid.TryParse(
                value.ToString(),
                out var nodeId))
        {
            throw new InvalidOperationException(
                $"Invalid node ID '{value}' found in stopping queue.");
        }

        return nodeId;
    }
}