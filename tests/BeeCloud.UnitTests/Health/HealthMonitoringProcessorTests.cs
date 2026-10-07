using BeeCloud.Application.Interfaces;
using BeeCloud.Domain.Entities;
using BeeCloud.Domain.Enums;
using BeeCloud.Worker.Processors;
using Microsoft.Extensions.Logging;
using Moq;

namespace BeeCloud.UnitTests.Health;

[TestFixture]
public class HealthMonitoringProcessorTests
{
    private Mock<IComputeNodeRepository> _nodeRepository = null!;
    private Mock<IHealthCheckRepository> _healthCheckRepository = null!;
    private Mock<IIncidentService> _incidentService = null!;
    private Mock<ILogger<HealthMonitoringProcessor>> _logger = null!;
    private HealthMonitoringProcessor _processor = null!;

    [SetUp]
    public void SetUp()
    {
        _nodeRepository = new Mock<IComputeNodeRepository>();
        _healthCheckRepository = new Mock<IHealthCheckRepository>();
        _incidentService = new Mock<IIncidentService>();
        _logger = new Mock<ILogger<HealthMonitoringProcessor>>();

        _healthCheckRepository
            .Setup(repository => repository.AddAsync(
                It.IsAny<HealthCheck>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _healthCheckRepository
            .Setup(repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _nodeRepository
            .Setup(repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _incidentService
            .Setup(service => service.CreateForUnhealthyNodeAsync(
                It.IsAny<ComputeNode>(),
                It.IsAny<HealthCheck>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((BeeCloud.Application.DTOs.Incidents.IncidentResponse?)null);

        _processor = new HealthMonitoringProcessor(
            _nodeRepository.Object,
            _healthCheckRepository.Object,
            _incidentService.Object,
            _logger.Object);
    }

    [Test]
    public async Task ProcessAsync_WithGpuOverheat_PersistsUnhealthyCheckAndCreatesIncident()
    {
        var node = CreateRunningNode();
        node.SimulateFault(NodeFault.GpuOverheat);
        HealthCheck? savedCheck = null;

        _nodeRepository
            .Setup(repository => repository.GetByStatusAsync(
                NodeStatus.Running,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { node });
        _healthCheckRepository
            .Setup(repository => repository.AddAsync(
                It.IsAny<HealthCheck>(),
                It.IsAny<CancellationToken>()))
            .Callback<HealthCheck, CancellationToken>((check, _) => savedCheck = check)
            .Returns(Task.CompletedTask);

        await _processor.ProcessAsync();

        Assert.That(savedCheck, Is.Not.Null);
        Assert.That(savedCheck!.ComputeNodeId, Is.EqualTo(node.Id));
        Assert.That(savedCheck.IsHealthy, Is.False);
        Assert.That(savedCheck.GpuTemperatureCelsius, Is.EqualTo(105));
        Assert.That(node.Status, Is.EqualTo(NodeStatus.Unhealthy));
        Assert.That(node.LastHealthCheck, Is.EqualTo(savedCheck.CheckedAt));
        _incidentService.Verify(service =>
            service.CreateForUnhealthyNodeAsync(
                node,
                savedCheck,
                It.IsAny<CancellationToken>()),
            Times.Once);
        _healthCheckRepository.Verify(repository =>
            repository.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
        _nodeRepository.Verify(repository =>
            repository.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task ProcessAsync_WhenNodePersistenceFails_ContinuesToMonitorNextNode()
    {
        var failedNode = CreateRunningNode();
        failedNode.SimulateFault(NodeFault.GpuFailure);
        var nextNode = CreateRunningNode();
        nextNode.SimulateFault(NodeFault.NetworkFailure);
        var addedNodeIds = new List<Guid>();

        _nodeRepository
            .Setup(repository => repository.GetByStatusAsync(
                NodeStatus.Running,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { failedNode, nextNode });
        _healthCheckRepository
            .Setup(repository => repository.AddAsync(
                It.IsAny<HealthCheck>(),
                It.IsAny<CancellationToken>()))
            .Callback<HealthCheck, CancellationToken>((check, _) =>
                addedNodeIds.Add(check.ComputeNodeId))
            .Returns<HealthCheck, CancellationToken>((check, _) =>
                check.ComputeNodeId == failedNode.Id
                    ? Task.FromException(new InvalidOperationException("Database failure"))
                    : Task.CompletedTask);

        await _processor.ProcessAsync();

        Assert.That(addedNodeIds, Is.EqualTo(new[] { failedNode.Id, nextNode.Id }));
        Assert.That(nextNode.Status, Is.EqualTo(NodeStatus.Unhealthy));
        _healthCheckRepository.Verify(repository =>
            repository.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
        _nodeRepository.Verify(repository =>
            repository.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
        _logger.Verify(logger =>
            logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) =>
                    state.ToString()!.Contains("Failed to monitor node")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    private static ComputeNode CreateRunningNode()
    {
        var node = new ComputeNode(
            $"health-monitor-test-{Guid.NewGuid():N}",
            "NVIDIA A100",
            1);
        node.MarkAvailable();
        node.Start();
        return node;
    }
}
