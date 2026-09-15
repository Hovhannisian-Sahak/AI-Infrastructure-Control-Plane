using BeeCloud.Domain.Entities;
using BeeCloud.Domain.Enums;
using BeeCloud.Infrastructure.Persistence;
using BeeCloud.Infrastructure.Persistence.Repositories;
using BeeCloud.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace BeeCloud.IntegrationTests.Incidents;

[TestFixture]
public class IncidentRepositoryTests
{
    private PostgreSqlTestContainer _postgres = null!;
    private ApplicationDbContext _dbContext = null!;
    private IncidentRepository _repository = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _postgres = new PostgreSqlTestContainer();

        await _postgres.StartAsync();

        var options =
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(_postgres.ConnectionString)
                .Options;

        _dbContext = new ApplicationDbContext(options);

        await _dbContext.Database.MigrateAsync();

        _repository = new IncidentRepository(_dbContext);
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        await _dbContext.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    [Test]
    public async Task AddAsync_ShouldPersistIncident()
    {
        var node = await CreateNodeAsync();

        var incident = new Incident(
            node.Id,
            IncidentSeverity.Critical,
            "GPU failure",
            "Test incident");

        await _repository.AddAsync(incident);
        await _repository.SaveChangesAsync();

        var result = await _repository.GetByIdAsync(incident.Id);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.ComputeNodeId, Is.EqualTo(node.Id));
        Assert.That(result.Severity, Is.EqualTo(IncidentSeverity.Critical));
        Assert.That(result.Status, Is.EqualTo(IncidentStatus.Open));
    }

    [Test]
    public async Task GetActiveForNodeAsync_WhenOpenIncidentExists_ShouldReturnIncident()
    {
        var node = await CreateNodeAsync();
        
        var incident = new Incident(
            node.Id,
            IncidentSeverity.High,
            "GPU failure");

        await _repository.AddAsync(incident);
        await _repository.SaveChangesAsync();

        var result =
            await _repository.GetActiveForNodeAsync(node.Id);

        Assert.That(result, Is.Not.Null);
        Assert.That(
            result!.Id,
            Is.EqualTo(incident.Id));
    }

    [Test]
    public async Task GetActiveForNodeAsync_WhenIncidentIsResolved_ShouldReturnNull()
    {
        var node = await CreateNodeAsync();

        var incident = new Incident(
            node.Id,
            IncidentSeverity.High,
            "GPU failure");

        incident.Resolve();

        await _repository.AddAsync(incident);
        await _repository.SaveChangesAsync();

        var result =
            await _repository.GetActiveForNodeAsync(node.Id);

        Assert.That(result, Is.Null);
    }
    private async Task<ComputeNode> CreateNodeAsync()
    {
        var node = new ComputeNode(
            $"integration-test-node-{Guid.NewGuid():N}",
            "NVIDIA A100",
            1);

        await _dbContext.ComputeNodes.AddAsync(node);
        await _dbContext.SaveChangesAsync();

        return node;
    }
}