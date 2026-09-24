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
    private Mock<ILogger<MetricsProcessor>> _logger = null!;
    private MetricsProcessor _processor = null!;

    [SetUp]
    public void SetUp()
    {
        _nodeRepository = new Mock<IComputeNodeRepository>();
        _metricRepository = new Mock<INodeMetricRepository>();
        _logger = new Mock<ILogger<MetricsProcessor>>();

        _processor = new MetricsProcessor(
            _nodeRepository.Object,
            _metricRepository.Object,
            _logger.Object);
    }

    [Test]
    public async Task ProcessAsync_ShouldRequestRunningNodes()
    {
        _nodeRepository
            .Setup(repository =>
                repository.GetByStatusAsync(
                    NodeStatus.Running,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ComputeNode>());

        await _processor.ProcessAsync();

        _nodeRepository.Verify(
            repository =>
                repository.GetByStatusAsync(
                    NodeStatus.Running,
                    It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task ProcessAsync_WithRunningNode_ShouldPersistMetric()
    {
        var node = CreateRunningNode();

        _nodeRepository
            .Setup(repository =>
                repository.GetByStatusAsync(
                    NodeStatus.Running,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ComputeNode> { node });

        await _processor.ProcessAsync();

        _metricRepository.Verify(
            repository =>
                repository.AddAsync(
                    It.Is<NodeMetric>(metric =>
                        metric.ComputeNodeId == node.Id),
                    It.IsAny<CancellationToken>()),
            Times.Once);

        _metricRepository.Verify(
            repository =>
                repository.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task ProcessAsync_WithMultipleRunningNodes_ShouldPersistMetricForEachNode()
    {
        var firstNode = CreateRunningNode();
        var secondNode = CreateRunningNode();
        var thirdNode = CreateRunningNode();

        _nodeRepository
            .Setup(repository =>
                repository.GetByStatusAsync(
                    NodeStatus.Running,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new List<ComputeNode>
                {
                    firstNode,
                    secondNode,
                    thirdNode
                });

        await _processor.ProcessAsync();

        _metricRepository.Verify(
            repository =>
                repository.AddAsync(
                    It.IsAny<NodeMetric>(),
                    It.IsAny<CancellationToken>()),
            Times.Exactly(3));

        _metricRepository.Verify(
            repository =>
                repository.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
            Times.Exactly(3));
    }

    [Test]
    public async Task ProcessAsync_WithNoRunningNodes_ShouldNotPersistMetrics()
    {
        _nodeRepository
            .Setup(repository =>
                repository.GetByStatusAsync(
                    NodeStatus.Running,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ComputeNode>());

        await _processor.ProcessAsync();

        _metricRepository.Verify(
            repository =>
                repository.AddAsync(
                    It.IsAny<NodeMetric>(),
                    It.IsAny<CancellationToken>()),
            Times.Never);

        _metricRepository.Verify(
            repository =>
                repository.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task ProcessAsync_WhenMetricPersistenceFails_ShouldContinueProcessingOtherNodes()
    {
        var firstNode = CreateRunningNode();
        var secondNode = CreateRunningNode();

        _nodeRepository
            .Setup(repository =>
                repository.GetByStatusAsync(
                    NodeStatus.Running,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new List<ComputeNode>
                {
                    firstNode,
                    secondNode
                });

        var callCount = 0;

        _metricRepository
            .Setup(repository =>
                repository.AddAsync(
                    It.IsAny<NodeMetric>(),
                    It.IsAny<CancellationToken>()))
            .Callback(() => callCount++)
            .ThrowsAsync(new InvalidOperationException("Database failure"));

        await _processor.ProcessAsync();

        Assert.That(callCount, Is.EqualTo(2));
    }

    [Test]
    public void ProcessAsync_WhenCancellationRequested_ShouldThrowOperationCanceledException()
    {
        using var cancellationTokenSource =
            new CancellationTokenSource();

        cancellationTokenSource.Cancel();

        Assert.ThrowsAsync<OperationCanceledException>(
            async () =>
                await _processor.ProcessAsync(
                    cancellationTokenSource.Token));
    }

    private static ComputeNode CreateRunningNode()
    {
        var node = new ComputeNode(
            "test-node",
            "NVIDIA-A100",
            1);

        node.MarkAvailable();
        node.Start();

        return node;
    }
}