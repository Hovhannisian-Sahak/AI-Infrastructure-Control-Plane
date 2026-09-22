using BeeCloud.Application.Interfaces;
using BeeCloud.Application.Services;
using BeeCloud.Domain.Entities;
using BeeCloud.Domain.Enums;
using BeeCloud.Infrastructure.Persistence;
using BeeCloud.Infrastructure.Persistence.Repositories;
using BeeCloud.IntegrationTests.Infrastructure;
using BeeCloud.Worker.Processors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace BeeCloud.IntegrationTests.Health;

[TestFixture]
public class HealthMonitoringProcessorIntegrationTests
{
    private PostgreSqlTestContainer _postgres = null!;
    private ApplicationDbContext _dbContext = null!;
    private HealthMonitoringProcessor _processor = null!;
    private NodeSimulationService _simulationService = null!;
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

        IComputeNodeRepository nodeRepository =
            new ComputeNodeRepository(_dbContext);

        IHealthCheckRepository healthCheckRepository =
            new HealthCheckRepository(_dbContext);

        IIncidentRepository incidentRepository =
            new IncidentRepository(_dbContext);

        IIncidentService incidentService =
            new IncidentService(
                incidentRepository,
                nodeRepository);
        
        _simulationService =
            new NodeSimulationService(
                nodeRepository);

        _processor = new HealthMonitoringProcessor(
            nodeRepository,
            healthCheckRepository,
            incidentService,
            NullLogger<HealthMonitoringProcessor>.Instance);
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        await _dbContext.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    [Test]
    public async Task ProcessAsync_WithHealthyNode_ShouldCreateHealthyHealthCheck()
    {
        // Arrange
        var node = await CreateRunningNodeAsync();

        // We don't want to rely on Random.Shared for this test.
        // Therefore, this test only verifies that a health check is persisted
        // and associated with the running node.

        // Act
        await _processor.ProcessAsync();

        // Assert
        var healthCheck = await _dbContext.HealthChecks
            .AsNoTracking()
            .Where(h => h.ComputeNodeId == node.Id)
            .OrderByDescending(h => h.CheckedAt)
            .FirstOrDefaultAsync();

        Assert.That(healthCheck, Is.Not.Null);
        Assert.That(
            healthCheck!.ComputeNodeId,
            Is.EqualTo(node.Id));

        Assert.That(
            healthCheck.CpuUsagePercent,
            Is.Not.Null);

        Assert.That(
            healthCheck.GpuUsagePercent,
            Is.Not.Null);

        Assert.That(
            healthCheck.GpuTemperatureCelsius,
            Is.Not.Null);
    }

    [Test]
    public async Task ProcessAsync_WithGpuOverheat_ShouldMarkNodeUnhealthy()
    {
        // Arrange
        var node = await CreateRunningNodeAsync();

        node.SimulateFault(NodeFault.GpuOverheat);

        await _dbContext.SaveChangesAsync();

        // Act
        await _processor.ProcessAsync();

        // Assert
        var persistedNode = await _dbContext.ComputeNodes
            .AsNoTracking()
            .FirstAsync(n => n.Id == node.Id);

        Assert.That(
            persistedNode.Status,
            Is.EqualTo(NodeStatus.Unhealthy));
    }

    [Test]
    public async Task ProcessAsync_WithGpuOverheat_ShouldPersistHealthCheck()
    {
        // Arrange
        var node = await CreateRunningNodeAsync();

        node.SimulateFault(NodeFault.GpuOverheat);

        await _dbContext.SaveChangesAsync();

        // Act
        await _processor.ProcessAsync();

        // Assert
        var healthCheck = await _dbContext.HealthChecks
            .AsNoTracking()
            .Where(h => h.ComputeNodeId == node.Id)
            .OrderByDescending(h => h.CheckedAt)
            .FirstOrDefaultAsync();

        Assert.That(healthCheck, Is.Not.Null);
        Assert.That(
            healthCheck!.IsHealthy,
            Is.False);

        Assert.That(
            healthCheck.GpuTemperatureCelsius,
            Is.EqualTo(105));
    }

    [Test]
    public async Task ProcessAsync_WithGpuOverheat_ShouldCreateCriticalIncident()
    {
        // Arrange
        var node = await CreateRunningNodeAsync();

        node.SimulateFault(NodeFault.GpuOverheat);

        await _dbContext.SaveChangesAsync();

        // Act
        await _processor.ProcessAsync();

        // Assert
        var incident = await _dbContext.Incidents
            .AsNoTracking()
            .Where(i => i.ComputeNodeId == node.Id)
            .OrderByDescending(i => i.CreatedAt)
            .FirstOrDefaultAsync();

        Assert.That(incident, Is.Not.Null);

        Assert.That(
            incident!.Severity,
            Is.EqualTo(IncidentSeverity.Critical));

        Assert.That(
            incident.Status,
            Is.EqualTo(IncidentStatus.Open));

        Assert.That(
            incident.Title,
            Is.EqualTo("GPU overheat detected"));
    }

    [Test]
    public async Task ProcessAsync_WithGpuFailure_ShouldCreateHighSeverityIncident()
    {
        // Arrange
        var node = await CreateRunningNodeAsync();

        node.SimulateFault(NodeFault.GpuFailure);

        await _dbContext.SaveChangesAsync();

        // Act
        await _processor.ProcessAsync();

        // Assert
        var incident = await _dbContext.Incidents
            .AsNoTracking()
            .Where(i => i.ComputeNodeId == node.Id)
            .FirstOrDefaultAsync();

        Assert.That(incident, Is.Not.Null);

        Assert.That(
            incident!.Severity,
            Is.EqualTo(IncidentSeverity.High));

        Assert.That(
            incident.Title,
            Is.EqualTo("GPU failure detected"));
    }

    [Test]
    public async Task ProcessAsync_WithNetworkFailure_ShouldCreateMediumIncident()
    {
        // Arrange
        var node = await CreateRunningNodeAsync();

        node.SimulateFault(NodeFault.NetworkFailure);

        await _dbContext.SaveChangesAsync();

        // Act
        await _processor.ProcessAsync();

        // Assert
        var incident = await _dbContext.Incidents
            .AsNoTracking()
            .Where(i => i.ComputeNodeId == node.Id)
            .FirstOrDefaultAsync();

        Assert.That(incident, Is.Not.Null);

        Assert.That(
            incident!.Severity,
            Is.EqualTo(IncidentSeverity.Medium));

        Assert.That(
            incident.Title,
            Is.EqualTo("Compute node health check failed"));
    }

    [Test]
    public async Task ProcessAsync_WithServiceCrash_ShouldCreateHighSeverityIncident()
    {
        // Arrange
        var node = await CreateRunningNodeAsync();

        node.SimulateFault(NodeFault.ServiceCrash);

        await _dbContext.SaveChangesAsync();

        // Act
        await _processor.ProcessAsync();

        // Assert
        var healthCheck = await _dbContext.HealthChecks
            .AsNoTracking()
            .Where(h => h.ComputeNodeId == node.Id)
            .OrderByDescending(h => h.CheckedAt)
            .FirstAsync();

        var incident = await _dbContext.Incidents
            .AsNoTracking()
            .Where(i => i.ComputeNodeId == node.Id)
            .FirstOrDefaultAsync();

        Assert.That(healthCheck.IsHealthy, Is.False);
        Assert.That(incident, Is.Not.Null);

        Assert.That(
            incident!.Severity,
            Is.EqualTo(IncidentSeverity.High));
    }
    [Test]
    public async Task ProcessAsync_WithRepeatedGpuOverheat_ShouldNotCreateDuplicateIncident()
    {
        // Arrange
        var node = await CreateRunningNodeAsync();

        node.SimulateFault(NodeFault.GpuOverheat);

        await _dbContext.SaveChangesAsync();

        // Act
        await _processor.ProcessAsync();
        await _processor.ProcessAsync();

        // Assert
        var incidents = await _dbContext.Incidents
            .AsNoTracking()
            .Where(i => i.ComputeNodeId == node.Id)
            .ToListAsync();

        Assert.That(incidents, Has.Count.EqualTo(1));
    }
    [Test]
    public async Task SimulateGpuFailure_ThenProcessHealthMonitoring_ShouldCreateHighSeverityIncident()
    {
        // Arrange
        var node = await CreateRunningNodeAsync();

        // Simulate the fault through the application service
        await _simulationService.SimulateFaultAsync(
            node.Id,
            NodeFault.GpuFailure);

        // Act
        await _processor.ProcessAsync();

        // Assert - health check
        var healthCheck = await _dbContext.HealthChecks
            .AsNoTracking()
            .Where(h => h.ComputeNodeId == node.Id)
            .OrderByDescending(h => h.CheckedAt)
            .FirstOrDefaultAsync();

        Assert.That(healthCheck, Is.Not.Null);

        Assert.That(
            healthCheck!.IsHealthy,
            Is.False);

        Assert.That(
            healthCheck.GpuUsagePercent,
            Is.EqualTo(0));

        // Assert - node status
        var persistedNode = await _dbContext.ComputeNodes
            .AsNoTracking()
            .FirstAsync(n => n.Id == node.Id);

        Assert.That(
            persistedNode.Status,
            Is.EqualTo(NodeStatus.Unhealthy));

        // Assert - incident
        var incident = await _dbContext.Incidents
            .AsNoTracking()
            .Where(i => i.ComputeNodeId == node.Id)
            .OrderByDescending(i => i.CreatedAt)
            .FirstOrDefaultAsync();

        Assert.That(incident, Is.Not.Null);

        Assert.That(
            incident!.Severity,
            Is.EqualTo(IncidentSeverity.High));

        Assert.That(
            incident.Status,
            Is.EqualTo(IncidentStatus.Open));

        Assert.That(
            incident.Title,
            Is.EqualTo("GPU failure detected"));
    }
    [Test]
    public async Task SimulateGpuOverheat_ThenProcessHealthMonitoring_ShouldCreateCriticalIncident()
    {
        // Arrange
        var node = await CreateRunningNodeAsync();

        await _simulationService.SimulateFaultAsync(
            node.Id,
            NodeFault.GpuOverheat);

        // Act
        await _processor.ProcessAsync();

        // Assert - health check
        var healthCheck = await _dbContext.HealthChecks
            .AsNoTracking()
            .Where(h => h.ComputeNodeId == node.Id)
            .OrderByDescending(h => h.CheckedAt)
            .FirstOrDefaultAsync();

        Assert.That(healthCheck, Is.Not.Null);

        Assert.That(
            healthCheck!.IsHealthy,
            Is.False);

        Assert.That(
            healthCheck.GpuTemperatureCelsius,
            Is.EqualTo(105));

        // Assert - node status
        var persistedNode = await _dbContext.ComputeNodes
            .AsNoTracking()
            .FirstAsync(n => n.Id == node.Id);

        Assert.That(
            persistedNode.Status,
            Is.EqualTo(NodeStatus.Unhealthy));

        // Assert - incident
        var incident = await _dbContext.Incidents
            .AsNoTracking()
            .Where(i => i.ComputeNodeId == node.Id)
            .OrderByDescending(i => i.CreatedAt)
            .FirstOrDefaultAsync();

        Assert.That(incident, Is.Not.Null);

        Assert.That(
            incident!.Severity,
            Is.EqualTo(IncidentSeverity.Critical));

        Assert.That(
            incident.Status,
            Is.EqualTo(IncidentStatus.Open));

        Assert.That(
            incident.Title,
            Is.EqualTo("GPU overheat detected"));
    }
    [Test]
    public async Task SimulateNetworkFailure_ThenProcessHealthMonitoring_ShouldCreateMediumIncident()
    {
        // Arrange
        var node = await CreateRunningNodeAsync();

        await _simulationService.SimulateFaultAsync(
            node.Id,
            NodeFault.NetworkFailure);

        // Act
        await _processor.ProcessAsync();

        // Assert - health check
        var healthCheck = await _dbContext.HealthChecks
            .AsNoTracking()
            .Where(h => h.ComputeNodeId == node.Id)
            .OrderByDescending(h => h.CheckedAt)
            .FirstOrDefaultAsync();

        Assert.That(healthCheck, Is.Not.Null);

        Assert.That(
            healthCheck!.IsHealthy,
            Is.False);

        // Assert - node status
        var persistedNode = await _dbContext.ComputeNodes
            .AsNoTracking()
            .FirstAsync(n => n.Id == node.Id);

        Assert.That(
            persistedNode.Status,
            Is.EqualTo(NodeStatus.Unhealthy));

        // Assert - incident
        var incident = await _dbContext.Incidents
            .AsNoTracking()
            .Where(i => i.ComputeNodeId == node.Id)
            .OrderByDescending(i => i.CreatedAt)
            .FirstOrDefaultAsync();

        Assert.That(incident, Is.Not.Null);

        Assert.That(
            incident!.Severity,
            Is.EqualTo(IncidentSeverity.Medium));

        Assert.That(
            incident.Status,
            Is.EqualTo(IncidentStatus.Open));

        Assert.That(
            incident.Title,
            Is.EqualTo("Compute node health check failed"));
    }
    [Test]
    public async Task SimulateServiceCrash_ThenProcessHealthMonitoring_ShouldCreateHighSeverityIncident()
    {
        // Arrange
        var node = await CreateRunningNodeAsync();

        await _simulationService.SimulateFaultAsync(
            node.Id,
            NodeFault.ServiceCrash);

        // Act
        await _processor.ProcessAsync();

        // Assert - health check
        var healthCheck = await _dbContext.HealthChecks
            .AsNoTracking()
            .Where(h => h.ComputeNodeId == node.Id)
            .OrderByDescending(h => h.CheckedAt)
            .FirstOrDefaultAsync();

        Assert.That(healthCheck, Is.Not.Null);

        Assert.That(
            healthCheck!.IsHealthy,
            Is.False);

        Assert.That(
            healthCheck.GpuUsagePercent,
            Is.EqualTo(0));

        // Assert - node status
        var persistedNode = await _dbContext.ComputeNodes
            .AsNoTracking()
            .FirstAsync(n => n.Id == node.Id);

        Assert.That(
            persistedNode.Status,
            Is.EqualTo(NodeStatus.Unhealthy));

        // Assert - incident
        var incident = await _dbContext.Incidents
            .AsNoTracking()
            .Where(i => i.ComputeNodeId == node.Id)
            .OrderByDescending(i => i.CreatedAt)
            .FirstOrDefaultAsync();

        Assert.That(incident, Is.Not.Null);

        Assert.That(
            incident!.Severity,
            Is.EqualTo(IncidentSeverity.High));

        Assert.That(
            incident.Status,
            Is.EqualTo(IncidentStatus.Open));

        Assert.That(
            incident.Title,
            Is.EqualTo("GPU failure detected"));
    }
    [Test]
    public async Task ProcessAsync_ShouldUpdateLastHealthCheck()
    {
        // Arrange
        var node = await CreateRunningNodeAsync();

        // Act
        await _processor.ProcessAsync();

        // Assert
        var persistedNode = await _dbContext.ComputeNodes
            .AsNoTracking()
            .FirstAsync(n => n.Id == node.Id);

        Assert.That(
            persistedNode.LastHealthCheck,
            Is.Not.Null);
    }

    private async Task<ComputeNode> CreateRunningNodeAsync()
    {
        var node = new ComputeNode(
            $"health-test-node-{Guid.NewGuid():N}",
            "NVIDIA A100",
            1);

        node.MarkAvailable();
        node.Start();

        await _dbContext.ComputeNodes.AddAsync(node);
        await _dbContext.SaveChangesAsync();

        return node;
    }
}