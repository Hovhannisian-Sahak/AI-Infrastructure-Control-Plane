using BeeCloud.Application.Pagination;
using BeeCloud.Domain.Entities;
using BeeCloud.Infrastructure.Persistence;
using BeeCloud.Infrastructure.Persistence.Repositories;
using BeeCloud.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace BeeCloud.IntegrationTests.Health;

[TestFixture]
public class HealthCheckRepositoryIntegrationTests
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
    public async Task GetHistoryPageAsync_UsesStableKeysetForOlderAndNewerPages()
    {
        await using var dbContext = CreateDbContext();
        var node = CreateNode();
        dbContext.ComputeNodes.Add(node);
        await dbContext.SaveChangesAsync();

        var checks = new List<HealthCheck>();
        for (var index = 0; index < 5; index++)
        {
            var check = new HealthCheck(
                node.Id,
                isHealthy: true,
                cpuUsagePercent: 10 + index,
                gpuUsagePercent: 20 + index,
                gpuTemperatureCelsius: 50 + index);
            checks.Add(check);
            dbContext.HealthChecks.Add(check);
            await dbContext.SaveChangesAsync();
            await Task.Delay(15);
        }

        var repository = new HealthCheckRepository(dbContext);

        var firstPage = await repository.GetHistoryPageAsync(
            node.Id,
            null,
            null,
            null,
            previous: false,
            limit: 3);

        Assert.That(firstPage.Select(check => check.Id), Is.EqualTo(new[]
        {
            checks[4].Id,
            checks[3].Id,
            checks[2].Id
        }));

        var olderPage = await repository.GetHistoryPageAsync(
            node.Id,
            null,
            null,
            new HistoryCursor(firstPage[^1].CheckedAt, firstPage[^1].Id),
            previous: false,
            limit: 3);

        Assert.That(olderPage.Select(check => check.Id), Is.EqualTo(new[]
        {
            checks[1].Id,
            checks[0].Id
        }));

        var newerPage = await repository.GetHistoryPageAsync(
            node.Id,
            null,
            null,
            new HistoryCursor(olderPage[0].CheckedAt, olderPage[0].Id),
            previous: true,
            limit: 3);

        Assert.That(newerPage.Select(check => check.Id), Is.EqualTo(new[]
        {
            checks[2].Id,
            checks[3].Id,
            checks[4].Id
        }));
    }

    [Test]
    public async Task GetHistoryPageAsync_AppliesTimeBoundsAndNodeFilter()
    {
        await using var dbContext = CreateDbContext();
        var node = CreateNode();
        var otherNode = CreateNode();
        dbContext.ComputeNodes.AddRange(node, otherNode);

        var inRange = new HealthCheck(node.Id, true, 35, 45, 60);
        var beforeRange = new HealthCheck(node.Id, true, 15, 25, 50);
        var otherNodeCheck = new HealthCheck(otherNode.Id, true, 55, 65, 70);
        dbContext.HealthChecks.AddRange(inRange, beforeRange, otherNodeCheck);
        await dbContext.SaveChangesAsync();

        var repository = new HealthCheckRepository(dbContext);
        var result = await repository.GetHistoryPageAsync(
            node.Id,
            inRange.CheckedAt.AddTicks(-1),
            inRange.CheckedAt.AddTicks(1),
            null,
            previous: false,
            limit: 10);

        Assert.That(result.Select(check => check.Id), Is.EqualTo(new[] { inRange.Id }));
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
            $"health-history-test-{Guid.NewGuid():N}",
            "NVIDIA A100",
            1);
    }
}
