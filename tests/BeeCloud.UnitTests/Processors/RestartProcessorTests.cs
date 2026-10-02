using BeeCloud.Application.Interfaces;
using BeeCloud.Domain.Entities;
using BeeCloud.Domain.Enums;
using BeeCloud.Infrastructure.Persistence.Repositories;
using BeeCloud.Worker.Processors;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace BeeCloud.UnitTests.Worker;

[TestFixture]
public class RestartProcessorTests
{
    [Test]
    public async Task ProcessAsync_WhenNodeIsStopping_ShouldRestartNode()
    {
        // Arrange
        var node = CreateRunningNode();
        node.Stop();

        var repository =
            new Mock<IComputeNodeRepository>();

        repository
            .Setup(repository => repository.GetByIdAsync(
                node.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(node);

        var queue =
            new Mock<IRestartQueue>();

        queue
            .SetupSequence(queue => queue.DequeueAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(node.Id)
            .ReturnsAsync((Guid?)null);

        var logger =
            Mock.Of<ILogger<RestartProcessor>>();

        var processor =
            new RestartProcessor(
                repository.Object,
                queue.Object,
                logger);

        // Act
        await processor.ProcessAsync();

        // Assert
        Assert.That(
            node.Status,
            Is.EqualTo(NodeStatus.Running));

        repository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Test]
    public async Task ProcessAsync_WhenNodeDoesNotExist_ShouldNotSaveChanges()
    {
        // Arrange
        var nodeId = Guid.NewGuid();

        var repository =
            new Mock<IComputeNodeRepository>();

        repository
            .Setup(repository => repository.GetByIdAsync(
                nodeId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ComputeNode?)null);

        var queue =
            new Mock<IRestartQueue>();

        queue
            .SetupSequence(queue => queue.DequeueAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(nodeId)
            .ReturnsAsync((Guid?)null);

        var logger =
            Mock.Of<ILogger<RestartProcessor>>();

        var processor =
            new RestartProcessor(
                repository.Object,
                queue.Object,
                logger);

        // Act
        await processor.ProcessAsync();

        // Assert
        repository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task ProcessAsync_WhenNodeIsNotStopping_ShouldSkipRestart()
    {
        // Arrange
        var node = CreateRunningNode();

        var repository =
            new Mock<IComputeNodeRepository>();

        repository
            .Setup(repository => repository.GetByIdAsync(
                node.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(node);

        var queue =
            new Mock<IRestartQueue>();

        queue
            .SetupSequence(queue => queue.DequeueAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(node.Id)
            .ReturnsAsync((Guid?)null);

        var logger =
            Mock.Of<ILogger<RestartProcessor>>();

        var processor =
            new RestartProcessor(
                repository.Object,
                queue.Object,
                logger);

        // Act
        await processor.ProcessAsync();

        // Assert
        Assert.That(
            node.Status,
            Is.EqualTo(NodeStatus.Running));

        repository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static ComputeNode CreateRunningNode()
    {
        var node = new ComputeNode(
            "restart-test-node",
            "NVIDIA A100",
            2);

        node.MarkAvailable();
        node.Start();

        return node;
    }
}