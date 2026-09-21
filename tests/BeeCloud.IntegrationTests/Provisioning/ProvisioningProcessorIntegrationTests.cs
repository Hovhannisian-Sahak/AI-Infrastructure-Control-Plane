using BeeCloud.Domain.Entities;
using BeeCloud.Domain.Enums;
using BeeCloud.Infrastructure.Persistence;
using BeeCloud.Infrastructure.Persistence.Repositories;
using BeeCloud.Infrastructure.Redis;
using BeeCloud.Worker.Processors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace BeeCloud.IntegrationTests.Provisioning;

[TestFixture]
public class ProvisioningProcessorIntegrationTests
{
    private PostgreSqlContainer _postgres = null!;
    private RedisContainer _redis = null!;

    private ApplicationDbContext _dbContext = null!;
    private IConnectionMultiplexer _redisConnection = null!;
    private RedisProvisioningQueue _queue = null!;
    private ProvisioningProcessor _processor = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _postgres = new PostgreSqlBuilder()
            .WithImage("postgres:16")
            .WithDatabase("beecloud_test")
            .WithUsername("beecloud_test")
            .WithPassword("beecloud_test_password")
            .Build();

        _redis = new RedisBuilder()
            .WithImage("redis:7")
            .Build();

        await _postgres.StartAsync();
        await _redis.StartAsync();

        var dbOptions =
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(_postgres.GetConnectionString())
                .Options;

        _dbContext =
            new ApplicationDbContext(dbOptions);

        await _dbContext.Database.EnsureCreatedAsync();

        _redisConnection =
            await ConnectionMultiplexer.ConnectAsync(
                _redis.GetConnectionString());

        _queue =
            new RedisProvisioningQueue(
                _redisConnection);

        var logger =
            LoggerFactory
                .Create(builder => builder.AddConsole())
                .CreateLogger<ProvisioningProcessor>();

        var repository =
            new ComputeNodeRepository(_dbContext);

        _processor =
            new ProvisioningProcessor(
                repository,
                _queue,
                logger);
    }

    [SetUp]
    public async Task SetUp()
    {
        await _dbContext.NodeMetrics.ExecuteDeleteAsync();
        await _dbContext.ComputeNodes.ExecuteDeleteAsync();

        var database =
            _redisConnection.GetDatabase();

        await database.KeyDeleteAsync(
            "beecloud:provisioning:queue");
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        await _dbContext.DisposeAsync();

        await _redisConnection.DisposeAsync();

        await _redis.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    [Test]
    public async Task ProcessAsync_WhenNodeIsQueued_ShouldProvisionNode()
    {
        var node = new ComputeNode(
            $"integration-node-{Guid.NewGuid():N}",
            "NVIDIA RTX 4090",
            1);

        await _dbContext.ComputeNodes.AddAsync(node);
        await _dbContext.SaveChangesAsync();

        await _queue.EnqueueAsync(node.Id);

        await _processor.ProcessAsync();

        var updatedNode =
            await _dbContext.ComputeNodes
                .AsNoTracking()
                .SingleAsync(x => x.Id == node.Id);

        Assert.That(
            updatedNode.Status,
            Is.EqualTo(NodeStatus.Available));
    }

    [Test]
    public async Task ProcessAsync_WhenNodeIsNotInQueue_ShouldNotChangeNode()
    {
        var node = new ComputeNode(
            $"integration-node-{Guid.NewGuid():N}",
            "NVIDIA RTX 4090",
            1);

        await _dbContext.ComputeNodes.AddAsync(node);
        await _dbContext.SaveChangesAsync();

        await _processor.ProcessAsync();

        var updatedNode =
            await _dbContext.ComputeNodes
                .AsNoTracking()
                .SingleAsync(x => x.Id == node.Id);

        Assert.That(
            updatedNode.Status,
            Is.EqualTo(NodeStatus.Provisioning));
    }

    [Test]
    public async Task ProcessAsync_WhenMultipleNodesAreQueued_ShouldProvisionAllNodes()
    {
        var firstNode = new ComputeNode(
            $"integration-node-{Guid.NewGuid():N}",
            "NVIDIA RTX 4090",
            1);

        var secondNode = new ComputeNode(
            $"integration-node-{Guid.NewGuid():N}",
            "NVIDIA RTX 4090",
            2);

        await _dbContext.ComputeNodes.AddRangeAsync(
            firstNode,
            secondNode);

        await _dbContext.SaveChangesAsync();

        await _queue.EnqueueAsync(firstNode.Id);
        await _queue.EnqueueAsync(secondNode.Id);

        await _processor.ProcessAsync();

        var nodes =
            await _dbContext.ComputeNodes
                .AsNoTracking()
                .Where(x =>
                    x.Id == firstNode.Id ||
                    x.Id == secondNode.Id)
                .ToListAsync();

        Assert.Multiple(() =>
        {
            Assert.That(
                nodes.Single(x => x.Id == firstNode.Id).Status,
                Is.EqualTo(NodeStatus.Available));

            Assert.That(
                nodes.Single(x => x.Id == secondNode.Id).Status,
                Is.EqualTo(NodeStatus.Available));
        });
    }

    [Test]
    public async Task ProcessAsync_WhenQueuedNodeDoesNotExist_ShouldNotThrow()
    {
        var missingNodeId = Guid.NewGuid();

        await _queue.EnqueueAsync(missingNodeId);

        Assert.DoesNotThrowAsync(
            async () => await _processor.ProcessAsync());
    }

    [Test]
    public async Task ProcessAsync_WhenQueuedNodeIsAlreadyAvailable_ShouldNotChangeIt()
    {
        var node = new ComputeNode(
            $"integration-node-{Guid.NewGuid():N}",
            "NVIDIA RTX 4090",
            1);

        node.MarkAvailable();

        await _dbContext.ComputeNodes.AddAsync(node);
        await _dbContext.SaveChangesAsync();

        await _queue.EnqueueAsync(node.Id);

        await _processor.ProcessAsync();

        var updatedNode =
            await _dbContext.ComputeNodes
                .AsNoTracking()
                .SingleAsync(x => x.Id == node.Id);

        Assert.That(
            updatedNode.Status,
            Is.EqualTo(NodeStatus.Available));
    }
}