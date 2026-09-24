using BeeCloud.Application.Interfaces;
using BeeCloud.Domain.Entities;
using BeeCloud.Domain.Enums;
using BeeCloud.Worker.Processors;
using Microsoft.Extensions.Logging;
using Moq;

namespace BeeCloud.UnitTests.Worker;

[TestFixture]
public class ProvisioningProcessorTests
{
    private Mock<IComputeNodeRepository> _repository = null!;
    private Mock<IProvisioningQueue> _queue = null!;
    private Mock<ILogger<ProvisioningProcessor>> _logger = null!;
    private Mock<IOperationalMetricsService> _operationalMetricsService = null!;
    private ProvisioningProcessor _processor = null!;

    [SetUp]
    public void SetUp()
    {
        _repository = new Mock<IComputeNodeRepository>();
        _queue = new Mock<IProvisioningQueue>();
        _logger = new Mock<ILogger<ProvisioningProcessor>>();
        _operationalMetricsService = new Mock<IOperationalMetricsService>();
        _processor = new ProvisioningProcessor(
            _repository.Object,
            _queue.Object,
            _operationalMetricsService.Object,
            _logger.Object);
    }

    [Test]
    public async Task ProcessAsync_WhenQueueIsEmpty_ShouldReturnWithoutProcessing()
    {
        _queue
            .Setup(queue => queue.DequeueAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid?)null);

        await _processor.ProcessAsync();

        _repository.Verify(
            repository => repository.GetByIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _repository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task ProcessAsync_WhenNodeIsProvisioning_ShouldMarkNodeAvailable()
    {
        var node = CreateNode();

        var nodeId = node.Id;

        _queue
            .SetupSequence(queue => queue.DequeueAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(nodeId)
            .ReturnsAsync((Guid?)null);

        _repository
            .Setup(repository => repository.GetByIdAsync(
                nodeId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(node);

        await _processor.ProcessAsync();

        Assert.That(
            node.Status,
            Is.EqualTo(NodeStatus.Available));

        _repository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task ProcessAsync_WhenNodeDoesNotExist_ShouldSkipNode()
    {
        var nodeId = Guid.NewGuid();

        _queue
            .SetupSequence(queue => queue.DequeueAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(nodeId)
            .ReturnsAsync((Guid?)null);

        _repository
            .Setup(repository => repository.GetByIdAsync(
                nodeId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ComputeNode?)null);

        await _processor.ProcessAsync();

        _repository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task ProcessAsync_WhenNodeIsNotProvisioning_ShouldSkipNode()
    {
        var node = CreateNode();

        node.MarkAvailable();

        var nodeId = node.Id;

        _queue
            .SetupSequence(queue => queue.DequeueAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(nodeId)
            .ReturnsAsync((Guid?)null);

        _repository
            .Setup(repository => repository.GetByIdAsync(
                nodeId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(node);

        await _processor.ProcessAsync();

        Assert.That(
            node.Status,
            Is.EqualTo(NodeStatus.Available));

        _repository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task ProcessAsync_WhenMultipleNodesAreQueued_ShouldProcessAllNodes()
    {
        var firstNode = CreateNode();
        var secondNode = CreateNode();

        _queue
            .SetupSequence(queue => queue.DequeueAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(firstNode.Id)
            .ReturnsAsync(secondNode.Id)
            .ReturnsAsync((Guid?)null);

        _repository
            .Setup(repository => repository.GetByIdAsync(
                firstNode.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(firstNode);

        _repository
            .Setup(repository => repository.GetByIdAsync(
                secondNode.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(secondNode);

        await _processor.ProcessAsync();

        Assert.Multiple(() =>
        {
            Assert.That(
                firstNode.Status,
                Is.EqualTo(NodeStatus.Available));

            Assert.That(
                secondNode.Status,
                Is.EqualTo(NodeStatus.Available));
        });

        _repository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Test]
    public async Task ProcessAsync_WhenOneNodeFails_ShouldContinueProcessingNextNode()
    {
        var failingNodeId = Guid.NewGuid();
        var successfulNode = CreateNode();

        _queue
            .SetupSequence(queue => queue.DequeueAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(failingNodeId)
            .ReturnsAsync(successfulNode.Id)
            .ReturnsAsync((Guid?)null);

        _repository
            .Setup(repository => repository.GetByIdAsync(
                failingNodeId,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(
                new InvalidOperationException("Database failure."));

        _repository
            .Setup(repository => repository.GetByIdAsync(
                successfulNode.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(successfulNode);

        await _processor.ProcessAsync();

        Assert.That(
            successfulNode.Status,
            Is.EqualTo(NodeStatus.Available));

        _repository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task ProcessAsync_WhenCancellationIsRequested_ShouldPropagateCancellation()
    {
        using var cancellationTokenSource =
            new CancellationTokenSource();

        cancellationTokenSource.Cancel();

        _queue
            .Setup(queue => queue.DequeueAsync(
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(
                new OperationCanceledException(
                    cancellationTokenSource.Token));

        Assert.ThrowsAsync<OperationCanceledException>(
            async () =>
                await _processor.ProcessAsync(
                    cancellationTokenSource.Token));
    }
    [Test]
    public async Task ProcessAsync_WithProvisioningNode_ShouldIncrementProvisioningMetric()
    {
        var node = CreateNode();

        _repository
            .Setup(repository =>
                repository.GetByIdAsync(
                    node.Id,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(node);

        _queue
            .SetupSequence(queue =>
                queue.DequeueAsync(
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(node.Id)
            .ReturnsAsync((Guid?)null);

        await _processor.ProcessAsync();

        _operationalMetricsService.Verify(
            service =>
                service.IncrementProvisioningAsync(
                    It.IsAny<CancellationToken>()),
            Times.Once);
    }
    [Test]
    public async Task ProcessAsync_WhenProvisioningFails_ShouldIncrementFailureMetric()
    {
        var node = CreateNode();

        _repository
            .Setup(repository =>
                repository.GetByIdAsync(
                    node.Id,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(node);

        _repository
            .Setup(repository =>
                repository.SaveChangesAsync(
                    It.IsAny<CancellationToken>()))
            .ThrowsAsync(
                new InvalidOperationException("Database failure"));

        _queue
            .SetupSequence(queue =>
                queue.DequeueAsync(
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(node.Id)
            .ReturnsAsync((Guid?)null);

        await _processor.ProcessAsync();

        _operationalMetricsService.Verify(
            service =>
                service.IncrementProvisioningFailureAsync(
                    It.IsAny<CancellationToken>()),
            Times.Once);
    }
    [Test]
    public async Task ProcessAsync_WithNonProvisioningNode_ShouldNotIncrementAnyMetric()
    {
        var node = CreateNode();
        node.MarkAvailable();

        _repository
            .Setup(repository =>
                repository.GetByIdAsync(
                    node.Id,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(node);

        _queue
            .SetupSequence(queue =>
                queue.DequeueAsync(
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(node.Id)
            .ReturnsAsync((Guid?)null);

        await _processor.ProcessAsync();

        _operationalMetricsService.Verify(
            service =>
                service.IncrementProvisioningAsync(
                    It.IsAny<CancellationToken>()),
            Times.Never);

        _operationalMetricsService.Verify(
            service =>
                service.IncrementProvisioningFailureAsync(
                    It.IsAny<CancellationToken>()),
            Times.Never);
    }
    private static ComputeNode CreateNode()
    {
        return new ComputeNode(
            $"test-node-{Guid.NewGuid():N}",
            "NVIDIA RTX 4090",
            1);
    }
}