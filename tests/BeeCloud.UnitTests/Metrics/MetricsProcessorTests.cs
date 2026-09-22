using BeeCloud.Application.Interfaces;
using BeeCloud.Domain.Entities;
using BeeCloud.Domain.Enums;
using BeeCloud.Worker.Processors;
using Microsoft.Extensions.Logging;
using Moq;

namespace BeeCloud.UnitTests.Metrics;

[TestFixture]
public class MetricsProcessorTests
{
    private Mock<IComputeNodeRepository> _nodeRepository = null!;
    private Mock<INodeMetricRepository> _metricRepository = null!;
    private Mock<IIncidentRepository> _incidentRepository = null!;
    private Mock<IBeeCloudMetrics> _beeCloudMetrics = null!;
    private Mock<ILogger<MetricsProcessor>> _logger = null!;
    private MetricsProcessor _processor = null!;

    [SetUp]
    public void SetUp()
    {
        _nodeRepository = new Mock<IComputeNodeRepository>();
        _metricRepository = new Mock<INodeMetricRepository>();
        _incidentRepository = new Mock<IIncidentRepository>();
        _beeCloudMetrics = new Mock<IBeeCloudMetrics>();
        _logger = new Mock<ILogger<MetricsProcessor>>();

        _nodeRepository
            .Setup(repository => repository.GetAllAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ComputeNode>());

        _incidentRepository
            .Setup(repository => repository.GetAllAsync(
                null,
                IncidentStatus.Open,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Incident>());

        _processor = new MetricsProcessor(
            _nodeRepository.Object,
            _metricRepository.Object,
            _incidentRepository.Object,
            _beeCloudMetrics.Object,
            _logger.Object);
    }

    [Test]
    public async Task ProcessAsync_ShouldRequestAllNodes()
    {
        // Act
        await _processor.ProcessAsync();

        // Assert
        _nodeRepository.Verify(
            repository => repository.GetAllAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task ProcessAsync_ShouldUpdateNodeCounts()
    {
        // Arrange
        var availableNode = CreateAvailableNode();
        var runningNode = CreateRunningNode();
        var unhealthyNode = CreateRunningNode();

        unhealthyNode.MarkUnhealthy();

        _nodeRepository
            .Setup(repository => repository.GetAllAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ComputeNode>
            {
                availableNode,
                runningNode,
                unhealthyNode
            });

        // Act
        await _processor.ProcessAsync();

        // Assert
        _beeCloudMetrics.Verify(
            metrics => metrics.SetNodeCounts(
                3,
                1,
                1),
            Times.Once);
    }

    [Test]
    public async Task ProcessAsync_ShouldUpdateOpenIncidentCount()
    {
        // Arrange
        var incidents = new List<Incident>
        {
            CreateIncident(),
            CreateIncident()
        };

        _incidentRepository
            .Setup(repository => repository.GetAllAsync(
                null,
                IncidentStatus.Open,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(incidents);

        // Act
        await _processor.ProcessAsync();

        // Assert
        _beeCloudMetrics.Verify(
            metrics => metrics.SetOpenIncidentCount(2),
            Times.Once);
    }

    [Test]
    public async Task ProcessAsync_WithRunningNode_ShouldSaveMetric()
    {
        // Arrange
        var node = CreateRunningNode();

        _nodeRepository
            .Setup(repository => repository.GetAllAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ComputeNode>
            {
                node
            });

        // Act
        await _processor.ProcessAsync();

        // Assert
        _metricRepository.Verify(
            repository => repository.AddAsync(
                It.Is<NodeMetric>(metric =>
                    metric.ComputeNodeId == node.Id &&
                    metric.CpuUsagePercent >= 0 &&
                    metric.CpuUsagePercent <= 100 &&
                    metric.GpuUsagePercent >= 0 &&
                    metric.GpuUsagePercent <= 100 &&
                    metric.GpuTemperatureCelsius >= -100 &&
                    metric.GpuTemperatureCelsius <= 200),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _metricRepository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task ProcessAsync_WithMultipleRunningNodes_ShouldSaveMetricForEachNode()
    {
        // Arrange
        var node1 = CreateRunningNode();
        var node2 = CreateRunningNode();
        var node3 = CreateRunningNode();

        _nodeRepository
            .Setup(repository => repository.GetAllAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ComputeNode>
            {
                node1,
                node2,
                node3
            });

        // Act
        await _processor.ProcessAsync();

        // Assert
        _metricRepository.Verify(
            repository => repository.AddAsync(
                It.IsAny<NodeMetric>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(3));

        _metricRepository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Exactly(3));
    }

    [Test]
    public async Task ProcessAsync_WithNoRunningNodes_ShouldNotSaveMetrics()
    {
        // Arrange
        _nodeRepository
            .Setup(repository => repository.GetAllAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ComputeNode>());

        // Act
        await _processor.ProcessAsync();

        // Assert
        _metricRepository.Verify(
            repository => repository.AddAsync(
                It.IsAny<NodeMetric>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _metricRepository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task ProcessAsync_WhenSavingOneNodeFails_ShouldContinueProcessingOtherNodes()
    {
        // Arrange
        var node1 = CreateRunningNode();
        var node2 = CreateRunningNode();

        _nodeRepository
            .Setup(repository => repository.GetAllAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ComputeNode>
            {
                node1,
                node2
            });

        _metricRepository
            .SetupSequence(repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"))
            .Returns(Task.CompletedTask);

        // Act
        await _processor.ProcessAsync();

        // Assert
        _metricRepository.Verify(
            repository => repository.AddAsync(
                It.IsAny<NodeMetric>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(2));

        _metricRepository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Test]
    public void ProcessAsync_WhenCancellationIsRequested_ShouldPropagateCancellation()
    {
        // Arrange
        using var cancellationTokenSource =
            new CancellationTokenSource();

        cancellationTokenSource.Cancel();

        _nodeRepository
            .Setup(repository => repository.GetAllAsync(
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        // Act & Assert
        Assert.ThrowsAsync<OperationCanceledException>(
            async () => await _processor.ProcessAsync(
                cancellationTokenSource.Token));
    }

    private static ComputeNode CreateAvailableNode()
    {
        var node = new ComputeNode(
            $"metrics-test-node-{Guid.NewGuid():N}",
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

    private static Incident CreateIncident()
    {
        var node = CreateRunningNode();

        return new Incident(
            node.Id,
            IncidentSeverity.High,
            "Test incident");
    }
}