using BeeCloud.Application.Interfaces;
using BeeCloud.Domain.Entities;
using BeeCloud.Domain.Enums;
using BeeCloud.Worker.Processors;
using Microsoft.Extensions.Logging;
using Moq;

namespace BeeCloud.UnitTests.Processors;

[TestFixture]
public class StoppingProcessorTests
{
    private Mock<IComputeNodeRepository> _nodeRepository = null!;
    private Mock<IStoppingQueue> _stoppingQueue = null!;
    private Mock<ILogger<StoppingProcessor>> _logger = null!;
    private StoppingProcessor _processor = null!;

    [SetUp]
    public void SetUp()
    {
        _nodeRepository =
            new Mock<IComputeNodeRepository>();

        _stoppingQueue =
            new Mock<IStoppingQueue>();

        _logger =
            new Mock<ILogger<StoppingProcessor>>();

        _processor = new StoppingProcessor(
            _nodeRepository.Object,
            _stoppingQueue.Object,
            _logger.Object);
    }

    [Test]
    public async Task ProcessAsync_WhenStoppingNodeExists_ShouldCompleteStopping()
    {
        // Arrange
        var node = CreateStoppingNode();

        _stoppingQueue
            .Setup(queue => queue.DequeueAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(node.Id)
            .Callback(() =>
            {
                _stoppingQueue
                    .Setup(queue => queue.DequeueAsync(
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync((Guid?)null);
            });

        _nodeRepository
            .Setup(repository => repository.GetByIdAsync(
                node.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(node);

        // Act
        await _processor.ProcessAsync();

        // Assert
        Assert.That(
            node.Status,
            Is.EqualTo(NodeStatus.Stopped));

        _nodeRepository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task ProcessAsync_WhenNodeDoesNotExist_ShouldNotSaveChanges()
    {
        // Arrange
        var nodeId = Guid.NewGuid();

        _stoppingQueue
            .Setup(queue => queue.DequeueAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(nodeId)
            .Callback(() =>
            {
                _stoppingQueue
                    .Setup(queue => queue.DequeueAsync(
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync((Guid?)null);
            });

        _nodeRepository
            .Setup(repository => repository.GetByIdAsync(
                nodeId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ComputeNode?)null);

        // Act
        await _processor.ProcessAsync();

        // Assert
        _nodeRepository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task ProcessAsync_WhenNodeIsNotStopping_ShouldSkipNode()
    {
        // Arrange
        var node = CreateRunningNode();

        _stoppingQueue
            .Setup(queue => queue.DequeueAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(node.Id)
            .Callback(() =>
            {
                _stoppingQueue
                    .Setup(queue => queue.DequeueAsync(
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync((Guid?)null);
            });

        _nodeRepository
            .Setup(repository => repository.GetByIdAsync(
                node.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(node);

        // Act
        await _processor.ProcessAsync();

        // Assert
        Assert.That(
            node.Status,
            Is.EqualTo(NodeStatus.Running));

        _nodeRepository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static ComputeNode CreateStoppingNode()
    {
        var node = CreateRunningNode();

        node.Stop();

        return node;
    }

    private static ComputeNode CreateRunningNode()
    {
        var node = new ComputeNode(
            $"test-node-{Guid.NewGuid():N}",
            "NVIDIA A100",
            4);

        node.MarkAvailable();
        node.Start();

        return node;
    }
}