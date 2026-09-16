using BeeCloud.Domain.Entities;
using BeeCloud.Infrastructure.Persistence;
using BeeCloud.Infrastructure.Persistence.Repositories;
using BeeCloud.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace BeeCloud.IntegrationTests.NodeMetrics;

[TestFixture]
public class NodeMetricRepositoryIntegrationTests
{
    private PostgreSqlTestContainer _container = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _container = new PostgreSqlTestContainer();

        await _container.StartAsync();

        await using var dbContext = CreateDbContext();

        await dbContext.Database.MigrateAsync();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        await _container.DisposeAsync();
    }

    [Test]
    public async Task AddAsync_AndSaveChangesAsync_ShouldPersistMetric()
    {
        // Arrange
        await using var dbContext = CreateDbContext();

        var node = CreateNode();
        dbContext.ComputeNodes.Add(node);

        await dbContext.SaveChangesAsync();

        var repository = new NodeMetricRepository(dbContext);

        var metric = new NodeMetric(
            node.Id,
            45.5,
            72.3,
            68.0);

        // Act
        await repository.AddAsync(metric);
        await repository.SaveChangesAsync();

        // Assert
        var savedMetric = await dbContext.NodeMetrics
            .FirstOrDefaultAsync(x => x.Id == metric.Id);

        Assert.That(savedMetric, Is.Not.Null);
        Assert.That(savedMetric!.ComputeNodeId, Is.EqualTo(node.Id));
        Assert.That(savedMetric.CpuUsagePercent, Is.EqualTo(45.5));
        Assert.That(savedMetric.GpuUsagePercent, Is.EqualTo(72.3));
        Assert.That(savedMetric.GpuTemperatureCelsius, Is.EqualTo(68.0));
    }

    [Test]
    public async Task GetByIdAsync_WhenMetricExists_ShouldReturnMetric()
    {
        // Arrange
        await using var dbContext = CreateDbContext();

        var node = CreateNode();
        dbContext.ComputeNodes.Add(node);

        var metric = new NodeMetric(
            node.Id,
            40,
            65,
            70);

        dbContext.NodeMetrics.Add(metric);

        await dbContext.SaveChangesAsync();

        var repository = new NodeMetricRepository(dbContext);

        // Act
        var result = await repository.GetByIdAsync(metric.Id);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Id, Is.EqualTo(metric.Id));
        Assert.That(result.ComputeNodeId, Is.EqualTo(node.Id));
        Assert.That(result.CpuUsagePercent, Is.EqualTo(40));
        Assert.That(result.GpuUsagePercent, Is.EqualTo(65));
        Assert.That(result.GpuTemperatureCelsius, Is.EqualTo(70));
    }

    [Test]
    public async Task GetByIdAsync_WhenMetricDoesNotExist_ShouldReturnNull()
    {
        // Arrange
        await using var dbContext = CreateDbContext();

        var repository = new NodeMetricRepository(dbContext);

        var metricId = Guid.NewGuid();

        // Act
        var result = await repository.GetByIdAsync(metricId);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetByNodeIdAsync_ShouldReturnOnlyMetricsForRequestedNode()
    {
        // Arrange
        await using var dbContext = CreateDbContext();

        var node1 = CreateNode();
        var node2 = CreateNode();

        dbContext.ComputeNodes.AddRange(node1, node2);

        var node1Metric1 = new NodeMetric(
            node1.Id,
            30,
            50,
            60);

        var node1Metric2 = new NodeMetric(
            node1.Id,
            40,
            70,
            65);

        var node2Metric = new NodeMetric(
            node2.Id,
            80,
            90,
            75);

        dbContext.NodeMetrics.AddRange(
            node1Metric1,
            node1Metric2,
            node2Metric);

        await dbContext.SaveChangesAsync();

        var repository = new NodeMetricRepository(dbContext);

        // Act
        var result = await repository.GetByNodeIdAsync(node1.Id);

        // Assert
        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(
            result.All(metric => metric.ComputeNodeId == node1.Id),
            Is.True);
    }

    [Test]
    public async Task GetByNodeIdAsync_ShouldReturnMetricsOrderedByRecordedAtDescending()
    {
        // Arrange
        await using var dbContext = CreateDbContext();

        var node = CreateNode();

        dbContext.ComputeNodes.Add(node);

        var olderMetric = new NodeMetric(
            node.Id,
            30,
            50,
            60);

        await dbContext.NodeMetrics.AddAsync(olderMetric);
        await dbContext.SaveChangesAsync();

        // Small delay so RecordedAt values are different.
        await Task.Delay(20);

        var newerMetric = new NodeMetric(
            node.Id,
            70,
            80,
            75);

        await dbContext.NodeMetrics.AddAsync(newerMetric);
        await dbContext.SaveChangesAsync();

        var repository = new NodeMetricRepository(dbContext);

        // Act
        var result = await repository.GetByNodeIdAsync(node.Id);

        // Assert
        Assert.That(result, Has.Count.EqualTo(2));

        Assert.That(result[0].Id, Is.EqualTo(newerMetric.Id));
        Assert.That(result[1].Id, Is.EqualTo(olderMetric.Id));

        Assert.That(
            result[0].RecordedAt,
            Is.GreaterThan(result[1].RecordedAt));
    }

    [Test]
    public async Task AddAsync_WithNonExistingNode_ShouldFailWithForeignKeyViolation()
    {
        // Arrange
        await using var dbContext = CreateDbContext();

        var repository = new NodeMetricRepository(dbContext);

        var metric = new NodeMetric(
            Guid.NewGuid(),
            50,
            60,
            65);

        // Act
        await repository.AddAsync(metric);

        // Assert
        Assert.ThrowsAsync<DbUpdateException>(
            async () => await repository.SaveChangesAsync());
    }

    [Test]
    public async Task GetByNodeIdAsync_WhenNoMetricsExist_ShouldReturnEmptyList()
    {
        // Arrange
        await using var dbContext = CreateDbContext();

        var node = CreateNode();

        dbContext.ComputeNodes.Add(node);

        await dbContext.SaveChangesAsync();

        var repository = new NodeMetricRepository(dbContext);

        // Act
        var result = await repository.GetByNodeIdAsync(node.Id);

        // Assert
        Assert.That(result, Is.Empty);
    }

    private ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_container.ConnectionString)
            .Options;

        return new ApplicationDbContext(options);
    }

    private static ComputeNode CreateNode()
    {
        return new ComputeNode(
            $"integration-test-node-{Guid.NewGuid():N}",
            "NVIDIA A100",
            4);
    }
}