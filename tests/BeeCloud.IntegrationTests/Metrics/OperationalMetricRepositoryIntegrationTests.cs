using BeeCloud.Domain.Entities;
using BeeCloud.Infrastructure.Persistence;
using BeeCloud.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace BeeCloud.IntegrationTests.Metrics;

[TestFixture]
public class OperationalMetricRepositoryIntegrationTests
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

        await dbContext.OperationalMetrics.ExecuteDeleteAsync();
    }

    [Test]
    public async Task GetByNameAsync_WithExistingMetric_ShouldReturnMetric()
    {
        await using var dbContext = CreateDbContext();

        var metric = new OperationalMetric(
            "provisioning_total",
            5);

        await dbContext.OperationalMetrics.AddAsync(metric);
        await dbContext.SaveChangesAsync();

        var repository =
            new OperationalMetricRepository(dbContext);

        var result = await repository.GetByNameAsync(
            "provisioning_total");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Id, Is.EqualTo(metric.Id));
        Assert.That(
            result.Name,
            Is.EqualTo("provisioning_total"));
        Assert.That(result.Value, Is.EqualTo(5));
    }

    [Test]
    public async Task GetByNameAsync_WithMissingMetric_ShouldReturnNull()
    {
        await using var dbContext = CreateDbContext();

        var repository =
            new OperationalMetricRepository(dbContext);

        var result = await repository.GetByNameAsync(
            "does_not_exist");

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task AddAsync_ShouldPersistMetric()
    {
        await using var dbContext = CreateDbContext();

        var repository =
            new OperationalMetricRepository(dbContext);

        var metric = new OperationalMetric(
            "remediation_total");

        await repository.AddAsync(metric);
        await repository.SaveChangesAsync();

        await using var verificationContext =
            CreateDbContext();

        var persistedMetric =
            await verificationContext.OperationalMetrics
                .SingleOrDefaultAsync(
                    item => item.Name == "remediation_total");

        Assert.That(persistedMetric, Is.Not.Null);
        Assert.That(
            persistedMetric!.Value,
            Is.EqualTo(0));
    }

    [Test]
    public async Task SaveChangesAsync_AfterIncrement_ShouldPersistUpdatedValue()
    {
        await using var dbContext = CreateDbContext();

        var metric = new OperationalMetric(
            "provisioning_total");

        await dbContext.OperationalMetrics.AddAsync(metric);
        await dbContext.SaveChangesAsync();

        var repository =
            new OperationalMetricRepository(dbContext);

        metric.Increment(3);

        await repository.SaveChangesAsync();

        await using var verificationContext =
            CreateDbContext();

        var persistedMetric =
            await verificationContext.OperationalMetrics
                .SingleAsync(
                    item => item.Name == "provisioning_total");

        Assert.That(
            persistedMetric.Value,
            Is.EqualTo(3));
    }

    [Test]
    public async Task MetricName_ShouldBeUnique()
    {
        await using var dbContext = CreateDbContext();

        var firstMetric = new OperationalMetric(
            "provisioning_total");

        var secondMetric = new OperationalMetric(
            "provisioning_total");

        await dbContext.OperationalMetrics.AddAsync(firstMetric);
        await dbContext.SaveChangesAsync();

        await dbContext.OperationalMetrics.AddAsync(secondMetric);

        Assert.ThrowsAsync<DbUpdateException>(
            async () => await dbContext.SaveChangesAsync());
    }

    private ApplicationDbContext CreateDbContext()
    {
        var options =
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(_container.GetConnectionString())
                .Options;

        return new ApplicationDbContext(options);
    }
}