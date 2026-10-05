using BeeCloud.Application.Services;
using BeeCloud.Domain.Entities;
using BeeCloud.Domain.Enums;
using BeeCloud.Infrastructure.Persistence;
using BeeCloud.Infrastructure.Persistence.Repositories;
using BeeCloud.IntegrationTests.Fakes;
using BeeCloud.IntegrationTests.Infrastructure;
using BeeCloud.Worker.Processors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BeeCloud.IntegrationTests.ComputeNodes;

[TestFixture]
public class ComputeNodeServiceIntegrationTests
{
    private PostgreSqlTestContainer _postgres = null!;
    private ApplicationDbContext _dbContext = null!;
    private ComputeNodeService _service = null!;
    private StoppingProcessor _stoppingProcessor = null!;
    private RestartProcessor _restartProcessor = null!;
    private FakeStoppingQueue _stoppingQueue = null!;
    private FakeRestartQueue _restartQueue = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _postgres = new PostgreSqlTestContainer();

        await _postgres.StartAsync();

        var options =
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(_postgres.ConnectionString)
                .Options;

        var stoppingLogger =
            LoggerFactory
                .Create(builder => builder.AddConsole())
                .CreateLogger<StoppingProcessor>();

        var restartLogger =
            LoggerFactory
                .Create(builder => builder.AddConsole())
                .CreateLogger<RestartProcessor>();

        _dbContext =
            new ApplicationDbContext(options);

        await _dbContext.Database.EnsureCreatedAsync();

        var repository =
            new ComputeNodeRepository(_dbContext);

        var provisioningQueue =
            new FakeProvisioningQueue();

        _stoppingQueue =
            new FakeStoppingQueue();

        _restartQueue =
            new FakeRestartQueue();

        _service =
            new ComputeNodeService(
                repository,
                provisioningQueue,
                _stoppingQueue,
                _restartQueue);

        _stoppingProcessor =
            new StoppingProcessor(
                repository,
                _stoppingQueue,
                stoppingLogger);
        
        _restartProcessor =
            new RestartProcessor(
                repository,
                _restartQueue,
                restartLogger);
    }

    [SetUp]
    public async Task SetUp()
    {
        _dbContext.ChangeTracker.Clear();

        await _dbContext.ComputeNodes.ExecuteDeleteAsync();

        _dbContext.ChangeTracker.Clear();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        await _dbContext.DisposeAsync();
        await _postgres.DisposeAsync();
    }
    [Test]
    public async Task DeleteAsync_WhenNodeExists_ShouldPersistSoftDelete()
    {
        // Arrange
        var node = new ComputeNode(
            "delete-integration-test-node",
            "NVIDIA A100",
            2);

        await _dbContext.ComputeNodes.AddAsync(node);
        await _dbContext.SaveChangesAsync();

        // Act
        await _service.DeleteAsync(node.Id);

        // Assert
        _dbContext.ChangeTracker.Clear();

        var persistedNode =
            await _dbContext.ComputeNodes
                .SingleAsync(x => x.Id == node.Id);

        Assert.That(
            persistedNode.IsActive,
            Is.False);

        Assert.That(
            persistedNode.DeletedAt,
            Is.Not.Null);
    }
    [Test]
    public async Task GetByIdAsync_WhenNodeIsSoftDeleted_ShouldReturnNull()
    {
        // Arrange
        var node = new ComputeNode(
            "deleted-node-integration-test-node",
            "NVIDIA A100",
            2);

        await _dbContext.ComputeNodes.AddAsync(node);
        await _dbContext.SaveChangesAsync();

        await _service.DeleteAsync(node.Id);

        // Act
        var result = await _service.GetByIdAsync(node.Id);

        // Assert
        Assert.That(result, Is.Null);
    }
    [Test]
    public async Task SimulateFaultAsync_WhenGpuFailureIsSimulated_ShouldPersistFault()
    {
        // Arrange
        var node = new ComputeNode(
            "integration-test-node",
            "NVIDIA A100",
            2);

        await _dbContext.ComputeNodes.AddAsync(node);
        await _dbContext.SaveChangesAsync();

        // Act
        await _service.SimulateFaultAsync(
            node.Id,
            NodeFault.GpuFailure);

        // Assert
        _dbContext.ChangeTracker.Clear();

        var persistedNode =
            await _dbContext.ComputeNodes
                .SingleAsync(x => x.Id == node.Id);

        Assert.That(
            persistedNode.ActiveFault,
            Is.EqualTo(NodeFault.GpuFailure));
    }

    [Test]
    public async Task StopAsync_WhenRunningNode_ShouldPersistStoppingAndEnqueueNode()
    {
        // Arrange
        var node = new ComputeNode(
            "stop-integration-test-node",
            "NVIDIA A100",
            2);

        node.MarkAvailable();
        node.Start();

        await _dbContext.ComputeNodes.AddAsync(node);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _service.StopAsync(node.Id);

        // Assert
        Assert.That(
            result.Status,
            Is.EqualTo(NodeStatus.Stopping.ToString()));

        _dbContext.ChangeTracker.Clear();

        var persistedNode =
            await _dbContext.ComputeNodes
                .SingleAsync(x => x.Id == node.Id);

        Assert.That(
            persistedNode.Status,
            Is.EqualTo(NodeStatus.Stopping));
    }

    [Test]
    public async Task StoppingProcessor_WhenNodeIsStopping_ShouldPersistStopped()
    {
        // Arrange
        var node = new ComputeNode(
            "processor-stop-integration-test-node",
            "NVIDIA A100",
            2);

        node.MarkAvailable();
        node.Start();
        node.Stop();

        await _dbContext.ComputeNodes.AddAsync(node);
        await _dbContext.SaveChangesAsync();

        await _stoppingQueue.EnqueueAsync(node.Id);

        // Act
        await _stoppingProcessor.ProcessAsync();

        // Assert
        _dbContext.ChangeTracker.Clear();

        var persistedNode =
            await _dbContext.ComputeNodes
                .SingleAsync(x => x.Id == node.Id);

        Assert.That(
            persistedNode.Status,
            Is.EqualTo(NodeStatus.Stopped));
    }
    [Test]
    public async Task RestartAsync_WhenRunningNode_ShouldPersistStoppingAndEnqueueNode()
    {
        // Arrange
        var node = new ComputeNode(
            "restart-integration-test-node",
            "NVIDIA A100",
            2);

        node.MarkAvailable();
        node.Start();

        await _dbContext.ComputeNodes.AddAsync(node);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _service.RestartAsync(node.Id);

        // Assert
        Assert.That(
            result.Status,
            Is.EqualTo(NodeStatus.Stopping.ToString()));

        _dbContext.ChangeTracker.Clear();

        var persistedNode =
            await _dbContext.ComputeNodes
                .SingleAsync(x => x.Id == node.Id);

        Assert.That(
            persistedNode.Status,
            Is.EqualTo(NodeStatus.Stopping));

        var queuedNodeId =
            await _restartQueue.DequeueAsync();

        Assert.That(
            queuedNodeId,
            Is.EqualTo(node.Id));
    }
    [Test]
    public async Task RestartProcessor_WhenNodeIsStopping_ShouldPersistRunning()
    {
        // Arrange
        var node = new ComputeNode(
            "restart-processor-integration-test-node",
            "NVIDIA A100",
            2);

        node.MarkAvailable();
        node.Start();

        await _dbContext.ComputeNodes.AddAsync(node);
        await _dbContext.SaveChangesAsync();

        await _service.RestartAsync(node.Id);

        _dbContext.ChangeTracker.Clear();

        // Act
        await _restartProcessor.ProcessAsync();

        // Assert
        _dbContext.ChangeTracker.Clear();

        var persistedNode =
            await _dbContext.ComputeNodes
                .SingleAsync(x => x.Id == node.Id);

        Assert.That(
            persistedNode.Status,
            Is.EqualTo(NodeStatus.Running));
    }
}