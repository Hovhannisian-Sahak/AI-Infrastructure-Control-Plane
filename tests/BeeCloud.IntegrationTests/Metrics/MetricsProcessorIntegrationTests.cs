using BeeCloud.Domain.Entities;
using BeeCloud.Infrastructure.Persistence;
using BeeCloud.Infrastructure.Persistence.Repositories;
using BeeCloud.Worker.Processors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Testcontainers.PostgreSql;

namespace BeeCloud.IntegrationTests.Metrics;

[TestFixture]
public class MetricsProcessorIntegrationTests
{
    private PostgreSqlContainer _container = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:16")
            .WithDatabase("beecloud_test")
            .WithUsername("beecloud_test")
            .WithPassword("beecloud_test_password")
            .Build();

        await _container.StartAsync();

        await using var dbContext = CreateDbContext();

        await dbContext.Database.MigrateAsync();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        await _container.DisposeAsync();
    }

    [SetUp]
    public async Task SetUp()
    {
        await using var dbContext = CreateDbContext();

        await dbContext.NodeMetrics.ExecuteDeleteAsync();
        await dbContext.ComputeNodes.ExecuteDeleteAsync();
    }

    [Test]
    public async Task ProcessAsync_WithRunningNode_ShouldPersistMetric()
    {
        await using var dbContext = CreateDbContext();

        var node = CreateRunningNode();

        await dbContext.ComputeNodes.AddAsync(node);
        await dbContext.SaveChangesAsync();

        var nodeRepository =
            new ComputeNodeRepository(dbContext);

        var metricRepository =
            new NodeMetricRepository(dbContext);

        var processor = new MetricsProcessor(
            nodeRepository,
            metricRepository,
            NullLogger<MetricsProcessor>.Instance);

        await processor.ProcessAsync();

        var metrics = await dbContext.NodeMetrics
            .Where(metric =>
                metric.ComputeNodeId == node.Id)
            .ToListAsync();

        Assert.That(
            metrics,
            Has.Count.EqualTo(1));

        var metric = metrics.Single();

        Assert.That(
            metric.ComputeNodeId,
            Is.EqualTo(node.Id));

        Assert.That(
            metric.CpuUsagePercent,
            Is.InRange(0, 100));

        Assert.That(
            metric.GpuUsagePercent,
            Is.InRange(0, 100));

        Assert.That(
            metric.GpuTemperatureCelsius,
            Is.InRange(40, 100));
    }

    [Test]
    public async Task ProcessAsync_WithMultipleRunningNodes_ShouldPersistMetricForEachNode()
    {
        await using var dbContext = CreateDbContext();

        var nodes = new[]
        {
            CreateRunningNode(),
            CreateRunningNode(),
            CreateRunningNode()
        };

        await dbContext.ComputeNodes.AddRangeAsync(nodes);
        await dbContext.SaveChangesAsync();

        var nodeRepository =
            new ComputeNodeRepository(dbContext);

        var metricRepository =
            new NodeMetricRepository(dbContext);

        var processor = new MetricsProcessor(
            nodeRepository,
            metricRepository,
            NullLogger<MetricsProcessor>.Instance);

        await processor.ProcessAsync();

        var nodeIds = nodes
            .Select(node => node.Id)
            .ToList();

        var metrics = await dbContext.NodeMetrics
            .Where(metric =>
                nodeIds.Contains(metric.ComputeNodeId))
            .ToListAsync();

        Assert.That(
            metrics,
            Has.Count.EqualTo(3));

        foreach (var node in nodes)
        {
            Assert.That(
                metrics.Count(metric =>
                    metric.ComputeNodeId == node.Id),
                Is.EqualTo(1));
        }
    }

    [Test]
    public async Task ProcessAsync_WithNonRunningNode_ShouldNotPersistMetric()
    {
        await using var dbContext = CreateDbContext();

        var node = new ComputeNode(
            $"metrics-integration-node-{Guid.NewGuid():N}",
            "NVIDIA A100",
            4);

        node.MarkAvailable();

        await dbContext.ComputeNodes.AddAsync(node);
        await dbContext.SaveChangesAsync();

        var nodeRepository =
            new ComputeNodeRepository(dbContext);

        var metricRepository =
            new NodeMetricRepository(dbContext);

        var processor = new MetricsProcessor(
            nodeRepository,
            metricRepository,
            NullLogger<MetricsProcessor>.Instance);

        await processor.ProcessAsync();

        var metrics = await dbContext.NodeMetrics
            .Where(metric =>
                metric.ComputeNodeId == node.Id)
            .ToListAsync();

        Assert.That(
            metrics,
            Is.Empty);
    }

    [Test]
    public async Task ProcessAsync_WithNoRunningNodes_ShouldCompleteWithoutCreatingMetrics()
    {
        await using var dbContext = CreateDbContext();

        var nodeRepository =
            new ComputeNodeRepository(dbContext);

        var metricRepository =
            new NodeMetricRepository(dbContext);

        var processor = new MetricsProcessor(
            nodeRepository,
            metricRepository,
            NullLogger<MetricsProcessor>.Instance);

        await processor.ProcessAsync();

        var metricsCount =
            await dbContext.NodeMetrics.CountAsync();

        Assert.That(
            metricsCount,
            Is.EqualTo(0));
    }

    private ApplicationDbContext CreateDbContext()
    {
        var options =
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(
                    _container.GetConnectionString())
                .Options;

        return new ApplicationDbContext(options);
    }

    private static ComputeNode CreateAvailableNode()
    {
        var node = new ComputeNode(
            $"metrics-integration-node-{Guid.NewGuid():N}",
            "NVIDIA A100",
            4);

        node.MarkAvailable();

        return node;
    }

    private static ComputeNode CreateRunningNode()
    {
        var node = CreateAvailableNode();

        node.Start();

        return node;
    }
}