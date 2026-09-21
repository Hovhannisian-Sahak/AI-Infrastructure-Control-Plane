using BeeCloud.Infrastructure.Redis;
using BeeCloud.IntegrationTests.Infrastructure;
using StackExchange.Redis;

namespace BeeCloud.IntegrationTests.Redis;

[TestFixture]
public class RedisProvisioningQueueIntegrationTests
{
    private RedisTestContainer _redisContainer = null!;
    private IConnectionMultiplexer _redis = null!;
    private RedisProvisioningQueue _queue = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _redisContainer =
            new RedisTestContainer();

        await _redisContainer.StartAsync();

        _redis =
            await ConnectionMultiplexer.ConnectAsync(
                _redisContainer.ConnectionString);

        _queue =
            new RedisProvisioningQueue(_redis);
    }

    [SetUp]
    public async Task SetUp()
    {
        var database =
            _redis.GetDatabase();

        await database.KeyDeleteAsync(
            "beecloud:provisioning:queue");
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        await _redis.CloseAsync();
        _redis.Dispose();

        await _redisContainer.DisposeAsync();
    }

    [Test]
    public async Task EnqueueAndDequeueAsync_ShouldPreserveFifoOrder()
    {
        // Arrange
        var firstNodeId = Guid.NewGuid();
        var secondNodeId = Guid.NewGuid();
        var thirdNodeId = Guid.NewGuid();

        // Act
        await _queue.EnqueueAsync(firstNodeId);
        await _queue.EnqueueAsync(secondNodeId);
        await _queue.EnqueueAsync(thirdNodeId);

        var first =
            await _queue.DequeueAsync();

        var second =
            await _queue.DequeueAsync();

        var third =
            await _queue.DequeueAsync();

        var empty =
            await _queue.DequeueAsync();

        // Assert
        Assert.That(first, Is.EqualTo(firstNodeId));
        Assert.That(second, Is.EqualTo(secondNodeId));
        Assert.That(third, Is.EqualTo(thirdNodeId));
        Assert.That(empty, Is.Null);
    }

    [Test]
    public async Task EnqueueAsync_WhenCalledConcurrently_ShouldPreserveAllNodeIds()
    {
        // Arrange
        const int nodeCount = 50;

        var nodeIds =
            Enumerable
                .Range(0, nodeCount)
                .Select(_ => Guid.NewGuid())
                .ToList();

        // Act
        await Task.WhenAll(
            nodeIds.Select(
                nodeId => _queue.EnqueueAsync(nodeId)));

        var dequeuedNodeIds =
            new List<Guid>();

        for (var i = 0; i < nodeCount; i++)
        {
            var nodeId =
                await _queue.DequeueAsync();

            Assert.That(nodeId, Is.Not.Null);

            dequeuedNodeIds.Add(
                nodeId!.Value);
        }

        // Assert
        Assert.That(
            dequeuedNodeIds.Count,
            Is.EqualTo(nodeCount));

        Assert.That(
            dequeuedNodeIds.ToHashSet(),
            Is.EquivalentTo(nodeIds));
    }

    [Test]
    public async Task DequeueAsync_WhenCalledConcurrently_ShouldReturnEachNodeOnlyOnce()
    {
        // Arrange
        const int nodeCount = 50;

        var nodeIds =
            Enumerable
                .Range(0, nodeCount)
                .Select(_ => Guid.NewGuid())
                .ToList();

        foreach (var nodeId in nodeIds)
        {
            await _queue.EnqueueAsync(nodeId);
        }

        // Act
        var results =
            await Task.WhenAll(
                Enumerable
                    .Range(0, nodeCount)
                    .Select(_ => _queue.DequeueAsync()));

        // Assert
        var dequeuedNodeIds =
            results
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .ToList();

        Assert.That(
            dequeuedNodeIds.Count,
            Is.EqualTo(nodeCount));

        Assert.That(
            dequeuedNodeIds.Distinct().Count(),
            Is.EqualTo(nodeCount));

        Assert.That(
            dequeuedNodeIds.ToHashSet(),
            Is.EquivalentTo(nodeIds));
    }
}