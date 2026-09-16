using BeeCloud.Application.Interfaces;
using BeeCloud.Application.Services;
using BeeCloud.Domain.Entities;
using Moq;

namespace BeeCloud.UnitTests.NodeMetrics;

[TestFixture]
public class NodeMetricServiceTests
{
    private Mock<INodeMetricRepository> _metricRepository = null!;
    private Mock<IComputeNodeRepository> _nodeRepository = null!;
    private NodeMetricService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _metricRepository = new Mock<INodeMetricRepository>();
        _nodeRepository = new Mock<IComputeNodeRepository>();

        _service = new NodeMetricService(
            _metricRepository.Object,
            _nodeRepository.Object);
    }

    [Test]
    public async Task GetByIdAsync_WhenMetricExists_ShouldReturnMetric()
    {
        // Arrange
        var node = CreateNode();

        var metric = new NodeMetric(
            node.Id,
            45.5,
            72.3,
            68.0);

        _metricRepository
            .Setup(repository => repository.GetByIdAsync(
                metric.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(metric);

        // Act
        var result = await _service.GetByIdAsync(metric.Id);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Id, Is.EqualTo(metric.Id));
        Assert.That(result.ComputeNodeId, Is.EqualTo(node.Id));
        Assert.That(result.CpuUsagePercent, Is.EqualTo(45.5));
        Assert.That(result.GpuUsagePercent, Is.EqualTo(72.3));
        Assert.That(result.GpuTemperatureCelsius, Is.EqualTo(68.0));
        Assert.That(result.RecordedAt, Is.EqualTo(metric.RecordedAt));

        _metricRepository.Verify(
            repository => repository.GetByIdAsync(
                metric.Id,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task GetByIdAsync_WhenMetricDoesNotExist_ShouldReturnNull()
    {
        // Arrange
        var metricId = Guid.NewGuid();

        _metricRepository
            .Setup(repository => repository.GetByIdAsync(
                metricId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((NodeMetric?)null);

        // Act
        var result = await _service.GetByIdAsync(metricId);

        // Assert
        Assert.That(result, Is.Null);

        _metricRepository.Verify(
            repository => repository.GetByIdAsync(
                metricId,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task GetByNodeIdAsync_WhenNodeExists_ShouldReturnMetrics()
    {
        // Arrange
        var node = CreateNode();

        var metric1 = new NodeMetric(
            node.Id,
            40,
            60,
            65);

        var metric2 = new NodeMetric(
            node.Id,
            50,
            70,
            70);

        _nodeRepository
            .Setup(repository => repository.GetByIdAsync(
                node.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(node);

        _metricRepository
            .Setup(repository => repository.GetByNodeIdAsync(
                node.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<NodeMetric>
            {
                metric1,
                metric2
            });

        // Act
        var result = await _service.GetByNodeIdAsync(node.Id);

        // Assert
        Assert.That(result, Has.Count.EqualTo(2));

        Assert.That(result[0].Id, Is.EqualTo(metric1.Id));
        Assert.That(result[0].ComputeNodeId, Is.EqualTo(node.Id));
        Assert.That(result[0].CpuUsagePercent, Is.EqualTo(40));

        Assert.That(result[1].Id, Is.EqualTo(metric2.Id));
        Assert.That(result[1].ComputeNodeId, Is.EqualTo(node.Id));
        Assert.That(result[1].GpuUsagePercent, Is.EqualTo(70));

        _nodeRepository.Verify(
            repository => repository.GetByIdAsync(
                node.Id,
                It.IsAny<CancellationToken>()),
            Times.Once);

        _metricRepository.Verify(
            repository => repository.GetByNodeIdAsync(
                node.Id,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task GetByNodeIdAsync_WhenNodeDoesNotExist_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var nodeId = Guid.NewGuid();

        _nodeRepository
            .Setup(repository => repository.GetByIdAsync(
                nodeId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ComputeNode?)null);

        // Act
        var exception = Assert.ThrowsAsync<KeyNotFoundException>(
            async () => await _service.GetByNodeIdAsync(nodeId));

        // Assert
        Assert.That(
            exception!.Message,
            Does.Contain(nodeId.ToString()));

        _metricRepository.Verify(
            repository => repository.GetByNodeIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task GetByNodeIdAsync_WhenNoMetricsExist_ShouldReturnEmptyList()
    {
        // Arrange
        var node = CreateNode();

        _nodeRepository
            .Setup(repository => repository.GetByIdAsync(
                node.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(node);

        _metricRepository
            .Setup(repository => repository.GetByNodeIdAsync(
                node.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<NodeMetric>());

        // Act
        var result = await _service.GetByNodeIdAsync(node.Id);

        // Assert
        Assert.That(result, Is.Empty);

        _metricRepository.Verify(
            repository => repository.GetByNodeIdAsync(
                node.Id,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static ComputeNode CreateNode()
    {
        return new ComputeNode(
            $"test-node-{Guid.NewGuid():N}",
            "NVIDIA A100",
            4);
    }
}