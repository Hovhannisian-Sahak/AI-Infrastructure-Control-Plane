using BeeCloud.Infrastructure.Redis;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using StackExchange.Redis;
using Testcontainers.Redis;

namespace BeeCloud.IntegrationTests.Redis;

[TestFixture]
public class RedisProvisioningQueueIntegrationTests
{
    private RedisContainer _container = null!;
    private RedisProvisioningQueue _queue = null!;
    private IDistributedCache _cache = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _container = new RedisBuilder()
            .WithImage("redis:7")
            .Build();

        await _container.StartAsync();

        var options = new RedisCacheOptions
        {
            Configuration = _container.GetConnectionString()
        };

        _cache = new RedisCache(options);

        _queue = new RedisProvisioningQueue(_cache);
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        if (_cache is IDisposable disposable)
        {
            disposable.Dispose();
        }

        await _container.DisposeAsync();
    }

    [SetUp]
    public async Task SetUp()
    {
        var connection = await ConnectionMultiplexer.ConnectAsync(
            _container.GetConnectionString());

        var database = connection.GetDatabase();

        await database.KeyDeleteAsync(
            "beecloud:provisioning:queue");

        await connection.DisposeAsync();
    }

    [Test]
    public async Task EnqueueAsync_ShouldPersistNodeInRedis()
    {
        // Arrange
        var nodeId = Guid.NewGuid();

        // Act
        await _queue.EnqueueAsync(nodeId);

        // Assert
        var result = await _queue.DequeueAsync();

        Assert.That(result, Is.EqualTo(nodeId));
    }

    [Test]
    public async Task DequeueAsync_ShouldReturnNodesInFifoOrder()
    {
        // Arrange
        var firstNodeId = Guid.NewGuid();
        var secondNodeId = Guid.NewGuid();
        var thirdNodeId = Guid.NewGuid();

        await _queue.EnqueueAsync(firstNodeId);
        await _queue.EnqueueAsync(secondNodeId);
        await _queue.EnqueueAsync(thirdNodeId);

        // Act
        var first = await _queue.DequeueAsync();
        var second = await _queue.DequeueAsync();
        var third = await _queue.DequeueAsync();

        // Assert
        Assert.That(first, Is.EqualTo(firstNodeId));
        Assert.That(second, Is.EqualTo(secondNodeId));
        Assert.That(third, Is.EqualTo(thirdNodeId));
    }

    [Test]
    public async Task DequeueAsync_WhenQueueIsEmpty_ShouldReturnNull()
    {
        // Act
        var result = await _queue.DequeueAsync();

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task DequeueAsync_ShouldRemoveNodeFromQueue()
    {
        // Arrange
        var nodeId = Guid.NewGuid();

        await _queue.EnqueueAsync(nodeId);

        // Act
        var firstResult = await _queue.DequeueAsync();
        var secondResult = await _queue.DequeueAsync();

        // Assert
        Assert.That(firstResult, Is.EqualTo(nodeId));
        Assert.That(secondResult, Is.Null);
    }

    [Test]
    public async Task EnqueueAsync_MultipleTimes_ShouldPreserveAllNodes()
    {
        // Arrange
        var nodeIds = new[]
        {
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid()
        };

        // Act
        foreach (var nodeId in nodeIds)
        {
            await _queue.EnqueueAsync(nodeId);
        }

        // Assert
        foreach (var expectedNodeId in nodeIds)
        {
            var actualNodeId = await _queue.DequeueAsync();

            Assert.That(
                actualNodeId,
                Is.EqualTo(expectedNodeId));
        }
    }
}