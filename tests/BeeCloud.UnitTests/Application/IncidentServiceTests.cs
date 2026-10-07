using BeeCloud.Application.Interfaces;
using BeeCloud.Application.Services;
using BeeCloud.Domain.Entities;
using BeeCloud.Domain.Enums;
using Moq;
using NUnit.Framework;

namespace BeeCloud.UnitTests.Application;

[TestFixture]
public class IncidentServiceTests
{
    private Mock<IIncidentRepository> _incidentRepository = null!;
    private Mock<IComputeNodeRepository> _computeNodeRepository = null!;
    private IncidentService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _incidentRepository = new Mock<IIncidentRepository>();
        _computeNodeRepository = new Mock<IComputeNodeRepository>();

        _service = new IncidentService(
            _incidentRepository.Object,
            _computeNodeRepository.Object);
    }

    [Test]
    public async Task CreateForUnhealthyNodeAsync_WhenNoActiveIncidentExists_ShouldCreateIncident()
    {
        var node = CreateNode();
        var healthCheck = CreateUnhealthyHealthCheck(node.Id);

        _incidentRepository
            .Setup(repository => repository.GetActiveForNodeAsync(
                node.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Incident?)null);

        var result = await _service.CreateForUnhealthyNodeAsync(
            node,
            healthCheck);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.ComputeNodeId, Is.EqualTo(node.Id));
        Assert.That(result.Status, Is.EqualTo(IncidentStatus.Open));
        Assert.That(result.Severity, Is.EqualTo(IncidentSeverity.Critical));
        Assert.That(result.Title, Is.EqualTo("GPU overheat detected"));

        _incidentRepository.Verify(
            repository => repository.AddAsync(
                It.Is<Incident>(incident =>
                    incident.ComputeNodeId == node.Id &&
                    incident.Status == IncidentStatus.Open &&
                    incident.Severity == IncidentSeverity.Critical),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _incidentRepository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task CreateForUnhealthyNodeAsync_WhenActiveIncidentExists_ShouldNotCreateDuplicate()
    {
        var node = CreateNode();
        var healthCheck = CreateUnhealthyHealthCheck(node.Id);
        var existingIncident = CreateIncident(
            node.Id,
            IncidentStatus.Open);

        _incidentRepository
            .Setup(repository => repository.GetActiveForNodeAsync(
                node.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingIncident);

        var result = await _service.CreateForUnhealthyNodeAsync(
            node,
            healthCheck);

        Assert.That(result, Is.Null);

        _incidentRepository.Verify(
            repository => repository.AddAsync(
                It.IsAny<Incident>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _incidentRepository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task CreateForUnhealthyNodeAsync_WhenInvestigatingIncidentExists_ShouldNotCreateDuplicate()
    {
        var node = CreateNode();
        var healthCheck = CreateUnhealthyHealthCheck(node.Id);
        var existingIncident = CreateIncident(
            node.Id,
            IncidentStatus.Investigating);

        _incidentRepository
            .Setup(repository => repository.GetActiveForNodeAsync(
                node.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingIncident);

        var result = await _service.CreateForUnhealthyNodeAsync(
            node,
            healthCheck);

        Assert.That(result, Is.Null);

        _incidentRepository.Verify(
            repository => repository.AddAsync(
                It.IsAny<Incident>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task CreateForUnhealthyNodeAsync_WhenOnlyResolvedIncidentExists_ShouldCreateNewIncident()
    {
        var node = CreateNode();
        var healthCheck = CreateUnhealthyHealthCheck(node.Id);
        var resolvedIncident = CreateIncident(
            node.Id,
            IncidentStatus.Resolved);

        _incidentRepository
            .Setup(repository => repository.GetActiveForNodeAsync(
                node.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Incident?)null);

        var result = await _service.CreateForUnhealthyNodeAsync(
            node,
            healthCheck);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Status, Is.EqualTo(IncidentStatus.Open));

        _incidentRepository.Verify(
            repository => repository.AddAsync(
                It.IsAny<Incident>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task CreateForUnhealthyNodeAsync_WhenGpuTemperatureIsHigh_ShouldCreateHighSeverityIncident()
    {
        var node = CreateNode();

        var healthCheck = new HealthCheck(
            node.Id,
            isHealthy: false,
            cpuUsagePercent: 70,
            gpuUsagePercent: 90,
            gpuTemperatureCelsius: 95);

        _incidentRepository
            .Setup(repository => repository.GetActiveForNodeAsync(
                node.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Incident?)null);

        var result = await _service.CreateForUnhealthyNodeAsync(
            node,
            healthCheck);

        Assert.That(result, Is.Not.Null);
        Assert.That(
            result!.Severity,
            Is.EqualTo(IncidentSeverity.High));
    }

    [Test]
    public async Task CreateForUnhealthyNodeAsync_WhenGpuFailureIsDetected_ShouldCreateHighSeverityIncident()
    {
        var node = CreateNode();

        var healthCheck = new HealthCheck(
            node.Id,
            isHealthy: false,
            cpuUsagePercent: 40,
            gpuUsagePercent: 0,
            gpuTemperatureCelsius: 45);

        _incidentRepository
            .Setup(repository => repository.GetActiveForNodeAsync(
                node.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Incident?)null);

        var result = await _service.CreateForUnhealthyNodeAsync(
            node,
            healthCheck);

        Assert.That(result, Is.Not.Null);
        Assert.That(
            result!.Severity,
            Is.EqualTo(IncidentSeverity.High));

        Assert.That(
            result.Title,
            Is.EqualTo("GPU failure detected"));
    }

    [Test]
    public async Task ResolveForNodeAsync_WhenActiveIncidentExists_ShouldResolveIncident()
    {
        var node = CreateNode();
        var incident = CreateIncident(
            node.Id,
            IncidentStatus.Open);

        _incidentRepository
            .Setup(repository => repository.GetActiveForNodeAsync(
                node.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(incident);

        await _service.ResolveForNodeAsync(node.Id);

        Assert.That(
            incident.Status,
            Is.EqualTo(IncidentStatus.Resolved));

        Assert.That(
            incident.ResolvedAt,
            Is.Not.Null);

        _incidentRepository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task ResolveForNodeAsync_WhenNoActiveIncidentExists_ShouldDoNothing()
    {
        var nodeId = Guid.NewGuid();

        _incidentRepository
            .Setup(repository => repository.GetActiveForNodeAsync(
                nodeId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Incident?)null);

        await _service.ResolveForNodeAsync(nodeId);

        _incidentRepository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task GetPageAsync_ShouldApplyAllServerFiltersAndReturnTotalCount()
    {
        var nodeId = Guid.NewGuid();
        var incident = CreateIncident(nodeId, IncidentStatus.Open);
        _incidentRepository
            .Setup(repository => repository.GetPageAsync(
                IncidentSeverity.High,
                IncidentStatus.Open,
                nodeId,
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                2,
                15,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(((IReadOnlyList<Incident>)new[] { incident }, 21));
        var from = DateTime.UtcNow.AddDays(-2);
        var to = DateTime.UtcNow;

        var result = await _service.GetPageAsync(
            IncidentSeverity.High,
            IncidentStatus.Open,
            nodeId,
            from,
            to,
            2,
            15);

        Assert.That(result.Items, Has.Count.EqualTo(1));
        Assert.That(result.TotalCount, Is.EqualTo(21));
        _incidentRepository.Verify(repository => repository.GetPageAsync(
            IncidentSeverity.High,
            IncidentStatus.Open,
            nodeId,
            from,
            to,
            2,
            15,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private static ComputeNode CreateNode()
    {
        return new ComputeNode(
            "test-node",
            "NVIDIA H100",
            8);
    }

    private static HealthCheck CreateUnhealthyHealthCheck(
        Guid nodeId)
    {
        return new HealthCheck(
            nodeId,
            isHealthy: false,
            cpuUsagePercent: 65,
            gpuUsagePercent: 95,
            gpuTemperatureCelsius: 105);
    }

    private static Incident CreateIncident(
        Guid nodeId,
        IncidentStatus status)
    {
        var incident = new Incident(
            nodeId,
            IncidentSeverity.High,
            "GPU failure",
            "GPU became unavailable.");

        if (status == IncidentStatus.Investigating)
        {
            incident.StartInvestigation();
        }
        else if (status == IncidentStatus.Resolved)
        {
            incident.Resolve();
        }

        return incident;
    }
}