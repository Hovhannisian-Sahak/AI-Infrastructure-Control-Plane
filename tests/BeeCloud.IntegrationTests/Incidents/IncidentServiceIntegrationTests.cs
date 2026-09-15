using BeeCloud.Application.DTOs.Incidents;
using BeeCloud.Application.Services;
using BeeCloud.Domain.Entities;
using BeeCloud.Domain.Enums;
using BeeCloud.Infrastructure.Persistence;
using BeeCloud.Infrastructure.Persistence.Repositories;
using BeeCloud.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace BeeCloud.IntegrationTests.Incidents;

[TestFixture]
public class IncidentServiceIntegrationTests
{
    private PostgreSqlTestContainer _postgres = null!;
    private ApplicationDbContext _dbContext = null!;
    private IncidentService _service = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _postgres = new PostgreSqlTestContainer();

        await _postgres.StartAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_postgres.ConnectionString)
            .Options;

        _dbContext = new ApplicationDbContext(options);

        await _dbContext.Database.MigrateAsync();

        var incidentRepository =
            new IncidentRepository(_dbContext);

        var computeNodeRepository =
            new ComputeNodeRepository(_dbContext);

        _service = new IncidentService(
            incidentRepository,
            computeNodeRepository);
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        await _dbContext.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    [Test]
    public async Task CreateAsync_ShouldPersistIncident()
    {
        // Arrange
        var node = await CreateTestNodeAsync();

        var request = new CreateIncidentRequest
        {
            ComputeNodeId = node.Id,
            Severity = IncidentSeverity.Critical,
            Title = "GPU failure",
            Description = "GPU stopped responding."
        };

        // Act
        var result = await _service.CreateAsync(request);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.ComputeNodeId, Is.EqualTo(node.Id));
        Assert.That(result.Severity, Is.EqualTo(IncidentSeverity.Critical));
        Assert.That(result.Status, Is.EqualTo(IncidentStatus.Open));
        Assert.That(result.Title, Is.EqualTo("GPU failure"));

        var persistedIncident =
            await _dbContext.Incidents
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == result.Id);

        Assert.That(persistedIncident, Is.Not.Null);
        Assert.That(
            persistedIncident!.ComputeNodeId,
            Is.EqualTo(node.Id));
    }

    [Test]
    public async Task CreateAsync_WithNonExistingNode_ShouldThrow()
    {
        // Arrange
        var request = new CreateIncidentRequest
        {
            ComputeNodeId = Guid.NewGuid(),
            Severity = IncidentSeverity.High,
            Title = "GPU failure",
            Description = "Test incident."
        };

        // Act & Assert
        var exception = Assert.ThrowsAsync<KeyNotFoundException>(
            async () => await _service.CreateAsync(request));

        Assert.That(
            exception!.Message,
            Does.Contain("was not found"));
    }

    [Test]
    public async Task GetByIdAsync_ShouldReturnPersistedIncident()
    {
        // Arrange
        var node = await CreateTestNodeAsync();

        var request = new CreateIncidentRequest
        {
            ComputeNodeId = node.Id,
            Severity = IncidentSeverity.Medium,
            Title = "Health check failed",
            Description = "Test health failure."
        };

        var created = await _service.CreateAsync(request);

        // Act
        var result = await _service.GetByIdAsync(created.Id);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Id, Is.EqualTo(created.Id));
        Assert.That(result.ComputeNodeId, Is.EqualTo(node.Id));
        Assert.That(result.Status, Is.EqualTo(IncidentStatus.Open));
    }

    [Test]
    public async Task GetByIdAsync_WithNonExistingIncident_ShouldReturnNull()
    {
        // Act
        var result = await _service.GetByIdAsync(Guid.NewGuid());

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetAllAsync_ShouldFilterBySeverityAndStatus()
    {
        // Arrange
        var node = await CreateTestNodeAsync();

        await _service.CreateAsync(
            new CreateIncidentRequest
            {
                ComputeNodeId = node.Id,
                Severity = IncidentSeverity.Critical,
                Title = "Critical GPU failure"
            });

        // Act
        var result = await _service.GetAllAsync(
            IncidentSeverity.Critical,
            IncidentStatus.Open);

        // Assert
        Assert.That(result, Is.Not.Empty);
        Assert.That(
            result,
            Has.All.Matches<IncidentResponse>(
                incident =>
                    incident.Severity == IncidentSeverity.Critical &&
                    incident.Status == IncidentStatus.Open));
    }

    [Test]
    public async Task StartInvestigationAsync_ShouldPersistInvestigatingStatus()
    {
        // Arrange
        var node = await CreateTestNodeAsync();

        var created = await _service.CreateAsync(
            new CreateIncidentRequest
            {
                ComputeNodeId = node.Id,
                Severity = IncidentSeverity.High,
                Title = "GPU failure"
            });

        // Act
        var result =
            await _service.StartInvestigationAsync(created.Id);

        // Assert
        Assert.That(
            result.Status,
            Is.EqualTo(IncidentStatus.Investigating));

        var persistedIncident =
            await _dbContext.Incidents
                .AsNoTracking()
                .FirstAsync(i => i.Id == created.Id);

        Assert.That(
            persistedIncident.Status,
            Is.EqualTo(IncidentStatus.Investigating));
    }

    [Test]
    public async Task ResolveAsync_ShouldPersistResolvedStatus()
    {
        // Arrange
        var node = await CreateTestNodeAsync();

        var created = await _service.CreateAsync(
            new CreateIncidentRequest
            {
                ComputeNodeId = node.Id,
                Severity = IncidentSeverity.High,
                Title = "GPU failure"
            });

        // Act
        var result =
            await _service.ResolveAsync(created.Id);

        // Assert
        Assert.That(
            result.Status,
            Is.EqualTo(IncidentStatus.Resolved));

        Assert.That(
            result.ResolvedAt,
            Is.Not.Null);

        var persistedIncident =
            await _dbContext.Incidents
                .AsNoTracking()
                .FirstAsync(i => i.Id == created.Id);

        Assert.That(
            persistedIncident.Status,
            Is.EqualTo(IncidentStatus.Resolved));

        Assert.That(
            persistedIncident.ResolvedAt,
            Is.Not.Null);
    }

    [Test]
    public async Task CreateForUnhealthyNodeAsync_ShouldCreateIncident()
    {
        // Arrange
        var node = await CreateTestNodeAsync();

        var healthCheck = new HealthCheck(
            node.Id,
            false,
            cpuUsagePercent: 65,
            gpuUsagePercent: 50,
            gpuTemperatureCelsius: 105);

        // Act
        var result =
            await _service.CreateForUnhealthyNodeAsync(
                node,
                healthCheck);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.ComputeNodeId, Is.EqualTo(node.Id));
        Assert.That(
            result.Severity,
            Is.EqualTo(IncidentSeverity.Critical));
        Assert.That(
            result.Status,
            Is.EqualTo(IncidentStatus.Open));

        var persistedIncident =
            await _dbContext.Incidents
                .AsNoTracking()
                .FirstAsync(i => i.Id == result.Id);

        Assert.That(
            persistedIncident.Severity,
            Is.EqualTo(IncidentSeverity.Critical));
    }

    [Test]
    public async Task CreateForUnhealthyNodeAsync_WhenActiveIncidentExists_ShouldNotCreateDuplicate()
    {
        // Arrange
        var node = await CreateTestNodeAsync();

        var firstHealthCheck = new HealthCheck(
            node.Id,
            false,
            cpuUsagePercent: 60,
            gpuUsagePercent: 50,
            gpuTemperatureCelsius: 105);

        var firstIncident =
            await _service.CreateForUnhealthyNodeAsync(
                node,
                firstHealthCheck);

        var secondHealthCheck = new HealthCheck(
            node.Id,
            false,
            cpuUsagePercent: 70,
            gpuUsagePercent: 1,
            gpuTemperatureCelsius: 95);

        // Act
        var secondIncident =
            await _service.CreateForUnhealthyNodeAsync(
                node,
                secondHealthCheck);

        // Assert
        Assert.That(firstIncident, Is.Not.Null);
        Assert.That(secondIncident, Is.Null);

        var incidents =
            await _dbContext.Incidents
                .AsNoTracking()
                .Where(i => i.ComputeNodeId == node.Id)
                .ToListAsync();

        Assert.That(incidents, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task CreateForUnhealthyNodeAsync_WhenIncidentIsInvestigating_ShouldNotCreateDuplicate()
    {
        // Arrange
        var node = await CreateTestNodeAsync();

        var healthCheck = new HealthCheck(
            node.Id,
            false,
            cpuUsagePercent: 60,
            gpuUsagePercent: 50,
            gpuTemperatureCelsius: 95);

        var incident =
            await _service.CreateForUnhealthyNodeAsync(
                node,
                healthCheck);

        Assert.That(incident, Is.Not.Null);

        await _service.StartInvestigationAsync(
            incident!.Id);

        // Act
        var secondIncident =
            await _service.CreateForUnhealthyNodeAsync(
                node,
                healthCheck);

        // Assert
        Assert.That(secondIncident, Is.Null);

        var incidents =
            await _dbContext.Incidents
                .AsNoTracking()
                .Where(i => i.ComputeNodeId == node.Id)
                .ToListAsync();

        Assert.That(incidents, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task CreateForUnhealthyNodeAsync_WhenPreviousIncidentIsResolved_ShouldCreateNewIncident()
    {
        // Arrange
        var node = await CreateTestNodeAsync();

        var healthCheck = new HealthCheck(
            node.Id,
            false,
            cpuUsagePercent: 60,
            gpuUsagePercent: 50,
            gpuTemperatureCelsius: 95);

        var firstIncident =
            await _service.CreateForUnhealthyNodeAsync(
                node,
                healthCheck);

        Assert.That(firstIncident, Is.Not.Null);

        await _service.ResolveAsync(firstIncident!.Id);

        // Act
        var secondIncident =
            await _service.CreateForUnhealthyNodeAsync(
                node,
                healthCheck);

        // Assert
        Assert.That(secondIncident, Is.Not.Null);
        Assert.That(
            secondIncident!.Id,
            Is.Not.EqualTo(firstIncident.Id));

        var incidents =
            await _dbContext.Incidents
                .AsNoTracking()
                .Where(i => i.ComputeNodeId == node.Id)
                .OrderBy(i => i.CreatedAt)
                .ToListAsync();

        Assert.That(incidents, Has.Count.EqualTo(2));
        Assert.That(
            incidents[0].Status,
            Is.EqualTo(IncidentStatus.Resolved));
        Assert.That(
            incidents[1].Status,
            Is.EqualTo(IncidentStatus.Open));
    }

    [Test]
    public async Task ResolveForNodeAsync_WhenActiveIncidentExists_ShouldResolveIt()
    {
        // Arrange
        var node = await CreateTestNodeAsync();

        var healthCheck = new HealthCheck(
            node.Id,
            false,
            cpuUsagePercent: 60,
            gpuUsagePercent: 50,
            gpuTemperatureCelsius: 95);

        var incident =
            await _service.CreateForUnhealthyNodeAsync(
                node,
                healthCheck);

        Assert.That(incident, Is.Not.Null);

        // Act
        await _service.ResolveForNodeAsync(node.Id);

        // Assert
        var persistedIncident =
            await _dbContext.Incidents
                .AsNoTracking()
                .FirstAsync(i => i.Id == incident!.Id);

        Assert.That(
            persistedIncident.Status,
            Is.EqualTo(IncidentStatus.Resolved));

        Assert.That(
            persistedIncident.ResolvedAt,
            Is.Not.Null);
    }

    [Test]
    public async Task ResolveForNodeAsync_WhenNoActiveIncidentExists_ShouldDoNothing()
    {
        // Arrange
        var node = await CreateTestNodeAsync();

        // Act & Assert
        Assert.DoesNotThrowAsync(
            async () =>
                await _service.ResolveForNodeAsync(node.Id));
    }

    private async Task<ComputeNode> CreateTestNodeAsync()
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