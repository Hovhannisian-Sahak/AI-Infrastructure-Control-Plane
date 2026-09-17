using System.Text.Json;
using BeeCloud.Infrastructure.Redis;
using Microsoft.Extensions.Caching.Distributed;
using Moq;

namespace BeeCloud.UnitTests.Redis;

[TestFixture]
public class RedisProvisioningQueueTests
{
    private Mock<IDistributedCache> _cache = null!;
    private RedisProvisioningQueue _queue = null!;

    [SetUp]
    public void SetUp()
    {
        _cache = new Mock<IDistributedCache>();

        _queue = new RedisProvisioningQueue(
            _cache.Object);
    }

    [Test]
    public async Task EnqueueAsync_WhenQueueDoesNotExist_ShouldStoreNodeId()
    {
        // Arrange
        var nodeId = Guid.NewGuid();

        _cache
            .Setup(cache => cache.GetStringAsync(
                "beecloud:provisioning:queue",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        // Act
        await _queue.EnqueueAsync(nodeId);

        // Assert
        _cache.Verify(
            cache => cache.SetStringAsync(
                "beecloud:provisioning:queue",
                It.Is<string>(value =>
                    ContainsNodeId(value, nodeId)),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task EnqueueAsync_WhenQueueExists_ShouldPreserveExistingNodes()
    {
        // Arrange
        var existingNodeId = Guid.NewGuid();
        var newNodeId = Guid.NewGuid();

        var existingQueue = JsonSerializer.Serialize(
            new List<Guid>
            {
                existingNodeId
            });

        _cache
            .Setup(cache => cache.GetStringAsync(
                "beecloud:provisioning:queue",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingQueue);

        // Act
        await _queue.EnqueueAsync(newNodeId);

        // Assert
        _cache.Verify(
            cache => cache.SetStringAsync(
                "beecloud:provisioning:queue",
                It.Is<string>(value =>
                    ContainsNodeIds(
                        value,
                        existingNodeId,
                        newNodeId)),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task DequeueAsync_WhenQueueDoesNotExist_ShouldReturnNull()
    {
        // Arrange
        _cache
            .Setup(cache => cache.GetStringAsync(
                "beecloud:provisioning:queue",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        // Act
        var result = await _queue.DequeueAsync();

        // Assert
        Assert.That(result, Is.Null);

        _cache.Verify(
            cache => cache.SetStringAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task DequeueAsync_WhenQueueIsEmpty_ShouldReturnNull()
    {
        // Arrange
        var emptyQueue = JsonSerializer.Serialize(
            new List<Guid>());

        _cache
            .Setup(cache => cache.GetStringAsync(
                "beecloud:provisioning:queue",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyQueue);

        // Act
        var result = await _queue.DequeueAsync();

        // Assert
        Assert.That(result, Is.Null);

        _cache.Verify(
            cache => cache.SetStringAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task DequeueAsync_WhenQueueContainsNodes_ShouldReturnFirstNode()
    {
        // Arrange
        var firstNodeId = Guid.NewGuid();
        var secondNodeId = Guid.NewGuid();

        var queue = JsonSerializer.Serialize(
            new List<Guid>
            {
                firstNodeId,
                secondNodeId
            });

        _cache
            .Setup(cache => cache.GetStringAsync(
                "beecloud:provisioning:queue",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(queue);

        // Act
        var result = await _queue.DequeueAsync();

        // Assert
        Assert.That(result, Is.EqualTo(firstNodeId));

        _cache.Verify(
            cache => cache.SetStringAsync(
                "beecloud:provisioning:queue",
                It.Is<string>(value =>
                    ContainsOnlyNode(value, secondNodeId)),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task DequeueAsync_WhenQueueContainsMultipleNodes_ShouldPreserveRemainingOrder()
    {
        // Arrange
        var firstNodeId = Guid.NewGuid();
        var secondNodeId = Guid.NewGuid();
        var thirdNodeId = Guid.NewGuid();

        var queue = JsonSerializer.Serialize(
            new List<Guid>
            {
                firstNodeId,
                secondNodeId,
                thirdNodeId
            });

        _cache
            .Setup(cache => cache.GetStringAsync(
                "beecloud:provisioning:queue",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(queue);

        // Act
        var result = await _queue.DequeueAsync();

        // Assert
        Assert.That(result, Is.EqualTo(firstNodeId));

        _cache.Verify(
            cache => cache.SetStringAsync(
                "beecloud:provisioning:queue",
                It.Is<string>(value =>
                    HasRemainingNodesInOrder(
                        value,
                        secondNodeId,
                        thirdNodeId)),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task EnqueueAsync_ShouldUseExpectedRedisKey()
    {
        // Arrange
        var nodeId = Guid.NewGuid();

        _cache
            .Setup(cache => cache.GetStringAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        // Act
        await _queue.EnqueueAsync(nodeId);

        // Assert
        _cache.Verify(
            cache => cache.GetStringAsync(
                "beecloud:provisioning:queue",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task DequeueAsync_ShouldUseExpectedRedisKey()
    {
        // Arrange
        _cache
            .Setup(cache => cache.GetStringAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        // Act
        await _queue.DequeueAsync();

        // Assert
        _cache.Verify(
            cache => cache.GetStringAsync(
                "beecloud:provisioning:queue",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static bool ContainsNodeId(
        string json,
        Guid expectedNodeId)
    {
        var nodes = JsonSerializer.Deserialize<List<Guid>>(json);

        return nodes is not null &&
               nodes.Contains(expectedNodeId);
    }

    private static bool ContainsNodeIds(
        string json,
        Guid firstExpectedNodeId,
        Guid secondExpectedNodeId)
    {
        var nodes = JsonSerializer.Deserialize<List<Guid>>(json);

        return nodes is not null &&
               nodes.Count == 2 &&
               nodes[0] == firstExpectedNodeId &&
               nodes[1] == secondExpectedNodeId;
    }

    private static bool ContainsOnlyNode(
        string json,
        Guid expectedNodeId)
    {
        var nodes = JsonSerializer.Deserialize<List<Guid>>(json);

        return nodes is not null &&
               nodes.Count == 1 &&
               nodes[0] == expectedNodeId;
    }

    private static bool HasRemainingNodesInOrder(
        string json,
        Guid firstExpectedNodeId,
        Guid secondExpectedNodeId)
    {
        var nodes = JsonSerializer.Deserialize<List<Guid>>(json);

        return nodes is not null &&
               nodes.Count == 2 &&
               nodes[0] == firstExpectedNodeId &&
               nodes[1] == secondExpectedNodeId;
    }
}