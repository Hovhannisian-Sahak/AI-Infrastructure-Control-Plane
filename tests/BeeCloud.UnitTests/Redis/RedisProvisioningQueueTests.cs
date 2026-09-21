using BeeCloud.Infrastructure.Redis;
using Moq;
using StackExchange.Redis;

namespace BeeCloud.UnitTests.Redis;

[TestFixture]
public class RedisProvisioningQueueTests
{
    private const string QueueKey =
        "beecloud:provisioning:queue";

    private Mock<IConnectionMultiplexer> _redis = null!;
    private Mock<IDatabase> _database = null!;
    private RedisProvisioningQueue _queue = null!;

    [SetUp]
    public void SetUp()
    {
        _redis =
            new Mock<IConnectionMultiplexer>();

        _database =
            new Mock<IDatabase>();

        _redis
            .Setup(redis =>
                redis.GetDatabase(
                    It.IsAny<int>(),
                    It.IsAny<object?>()))
            .Returns(_database.Object);

        _queue =
            new RedisProvisioningQueue(
                _redis.Object);
    }

    [Test]
    public async Task EnqueueAsync_ShouldPushNodeIdToRedisQueue()
    {
        // Arrange
        var nodeId = Guid.NewGuid();

        _database
            .Setup(database =>
                database.ListRightPushAsync(
                    QueueKey,
                    nodeId.ToString(),
                    It.IsAny<When>(),
                    It.IsAny<CommandFlags>()))
            .ReturnsAsync(1);

        // Act
        await _queue.EnqueueAsync(nodeId);

        // Assert
        _database.Verify(
            database =>
                database.ListRightPushAsync(
                    QueueKey,
                    nodeId.ToString(),
                    It.IsAny<When>(),
                    It.IsAny<CommandFlags>()),
            Times.Once);
    }

    [Test]
    public async Task EnqueueAsync_ShouldUseExpectedRedisKey()
    {
        // Arrange
        var nodeId = Guid.NewGuid();

        _database
            .Setup(database =>
                database.ListRightPushAsync(
                    It.IsAny<RedisKey>(),
                    It.IsAny<RedisValue>(),
                    It.IsAny<When>(),
                    It.IsAny<CommandFlags>()))
            .ReturnsAsync(1);

        // Act
        await _queue.EnqueueAsync(nodeId);

        // Assert
        _database.Verify(
            database =>
                database.ListRightPushAsync(
                    QueueKey,
                    nodeId.ToString(),
                    It.IsAny<When>(),
                    It.IsAny<CommandFlags>()),
            Times.Once);
    }

    [Test]
    public async Task DequeueAsync_WhenQueueIsEmpty_ShouldReturnNull()
    {
        // Arrange
        _database
            .Setup(database =>
                database.ListLeftPopAsync(
                    QueueKey,
                    It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);

        // Act
        var result =
            await _queue.DequeueAsync();

        // Assert
        Assert.That(result, Is.Null);

        _database.Verify(
            database =>
                database.ListLeftPopAsync(
                    QueueKey,
                    It.IsAny<CommandFlags>()),
            Times.Once);
    }

    [Test]
    public async Task DequeueAsync_WhenQueueContainsNode_ShouldReturnNode()
    {
        // Arrange
        var nodeId = Guid.NewGuid();

        _database
            .Setup(database =>
                database.ListLeftPopAsync(
                    QueueKey,
                    It.IsAny<CommandFlags>()))
            .ReturnsAsync(
                new RedisValue(nodeId.ToString()));

        // Act
        var result =
            await _queue.DequeueAsync();

        // Assert
        Assert.That(
            result,
            Is.EqualTo(nodeId));
    }

    [Test]
    public async Task DequeueAsync_ShouldUseExpectedRedisKey()
    {
        // Arrange
        _database
            .Setup(database =>
                database.ListLeftPopAsync(
                    It.IsAny<RedisKey>(),
                    It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);

        // Act
        await _queue.DequeueAsync();

        // Assert
        _database.Verify(
            database =>
                database.ListLeftPopAsync(
                    QueueKey,
                    It.IsAny<CommandFlags>()),
            Times.Once);
    }

    [Test]
    public async Task DequeueAsync_WhenRedisContainsInvalidGuid_ShouldThrow()
    {
        // Arrange
        _database
            .Setup(database =>
                database.ListLeftPopAsync(
                    QueueKey,
                    It.IsAny<CommandFlags>()))
            .ReturnsAsync(
                new RedisValue("invalid-node-id"));

        // Act & Assert
        var exception =
            Assert.ThrowsAsync<InvalidOperationException>(
                async () =>
                    await _queue.DequeueAsync());

        Assert.That(
            exception!.Message,
            Does.Contain("Invalid node ID"));
    }

    [Test]
    public async Task EnqueueAsync_WhenCancellationRequested_ShouldThrow()
    {
        // Arrange
        var nodeId = Guid.NewGuid();

        using var cancellationTokenSource =
            new CancellationTokenSource();

        cancellationTokenSource.Cancel();

        // Act & Assert
        Assert.ThrowsAsync<OperationCanceledException>(
            async () =>
                await _queue.EnqueueAsync(
                    nodeId,
                    cancellationTokenSource.Token));

        _database.Verify(
            database =>
                database.ListRightPushAsync(
                    It.IsAny<RedisKey>(),
                    It.IsAny<RedisValue>(),
                    It.IsAny<When>(),
                    It.IsAny<CommandFlags>()),
            Times.Never);
    }

    [Test]
    public async Task DequeueAsync_WhenCancellationRequested_ShouldThrow()
    {
        // Arrange
        using var cancellationTokenSource =
            new CancellationTokenSource();

        cancellationTokenSource.Cancel();

        // Act & Assert
        Assert.ThrowsAsync<OperationCanceledException>(
            async () =>
                await _queue.DequeueAsync(
                    cancellationTokenSource.Token));

        _database.Verify(
            database =>
                database.ListLeftPopAsync(
                    It.IsAny<RedisKey>(),
                    It.IsAny<CommandFlags>()),
            Times.Never);
    }
}