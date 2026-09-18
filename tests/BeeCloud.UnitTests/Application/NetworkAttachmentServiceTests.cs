using BeeCloud.Application.DTOs.Networks;
using BeeCloud.Application.Interfaces;
using BeeCloud.Application.Services;
using BeeCloud.Domain.Entities;
using Moq;

namespace BeeCloud.UnitTests.Application;

[TestFixture]
public class NetworkAttachmentServiceTests
{
    private Mock<IComputeNodeRepository> _computeNodeRepositoryMock = null!;
    private Mock<INetworkRepository> _networkRepositoryMock = null!;
    private Mock<INetworkAttachmentRepository> _attachmentRepositoryMock = null!;

    private NetworkAttachmentService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _computeNodeRepositoryMock =
            new Mock<IComputeNodeRepository>();

        _networkRepositoryMock =
            new Mock<INetworkRepository>();

        _attachmentRepositoryMock =
            new Mock<INetworkAttachmentRepository>();

        _service = new NetworkAttachmentService(
            _computeNodeRepositoryMock.Object,
            _networkRepositoryMock.Object,
            _attachmentRepositoryMock.Object);
    }

    [Test]
    public void AttachAsync_WhenNodeDoesNotExist_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        var networkId = Guid.NewGuid();

        _computeNodeRepositoryMock
            .Setup(x => x.GetByIdAsync(
                nodeId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ComputeNode?)null);

        // Act
        var exception = Assert.ThrowsAsync<KeyNotFoundException>(
            async () => await _service.AttachAsync(nodeId, networkId));

        // Assert
        Assert.That(
            exception!.Message,
            Does.Contain($"Compute node with id '{nodeId}' was not found."));

        _networkRepositoryMock.Verify(
            x => x.GetByIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _attachmentRepositoryMock.Verify(
            x => x.AddAsync(
                It.IsAny<NetworkAttachment>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public void AttachAsync_WhenNetworkDoesNotExist_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var node = new ComputeNode(
            "node-1",
            "NVIDIA A100",
            4);

        var networkId = Guid.NewGuid();

        _computeNodeRepositoryMock
            .Setup(x => x.GetByIdAsync(
                node.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(node);

        _networkRepositoryMock
            .Setup(x => x.GetByIdAsync(
                networkId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Network?)null);

        // Act
        var exception = Assert.ThrowsAsync<KeyNotFoundException>(
            async () => await _service.AttachAsync(node.Id, networkId));

        // Assert
        Assert.That(
            exception!.Message,
            Does.Contain($"Network with id '{networkId}' was not found."));

        _attachmentRepositoryMock.Verify(
            x => x.AddAsync(
                It.IsAny<NetworkAttachment>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public void AttachAsync_WhenAttachmentAlreadyExists_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var node = new ComputeNode(
            "node-1",
            "NVIDIA A100",
            4);

        var network = new Network("production-network");

        var existingAttachment = new NetworkAttachment(
            node.Id,
            network.Id);

        _computeNodeRepositoryMock
            .Setup(x => x.GetByIdAsync(
                node.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(node);

        _networkRepositoryMock
            .Setup(x => x.GetByIdAsync(
                network.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(network);

        _attachmentRepositoryMock
            .Setup(x => x.GetAsync(
                node.Id,
                network.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingAttachment);

        // Act
        var exception = Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _service.AttachAsync(node.Id, network.Id));

        // Assert
        Assert.That(
            exception!.Message,
            Does.Contain("is already attached"));

        _attachmentRepositoryMock.Verify(
            x => x.AddAsync(
                It.IsAny<NetworkAttachment>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _attachmentRepositoryMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task AttachAsync_WithValidData_ShouldCreateAttachment()
    {
        // Arrange
        var node = new ComputeNode(
            "node-1",
            "NVIDIA A100",
            4);

        var network = new Network(
            "production-network",
            "Production network");

        _computeNodeRepositoryMock
            .Setup(x => x.GetByIdAsync(
                node.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(node);

        _networkRepositoryMock
            .Setup(x => x.GetByIdAsync(
                network.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(network);

        _attachmentRepositoryMock
            .Setup(x => x.GetAsync(
                node.Id,
                network.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((NetworkAttachment?)null);

        // Act
        var result = await _service.AttachAsync(
            node.Id,
            network.Id);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Id, Is.Not.EqualTo(Guid.Empty));
            Assert.That(result.ComputeNodeId, Is.EqualTo(node.Id));
            Assert.That(result.NetworkId, Is.EqualTo(network.Id));
        });

        _attachmentRepositoryMock.Verify(
            x => x.AddAsync(
                It.Is<NetworkAttachment>(attachment =>
                    attachment.ComputeNodeId == node.Id &&
                    attachment.NetworkId == network.Id),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _attachmentRepositoryMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public void DetachAsync_WhenAttachmentDoesNotExist_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        var networkId = Guid.NewGuid();

        _attachmentRepositoryMock
            .Setup(x => x.GetAsync(
                nodeId,
                networkId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((NetworkAttachment?)null);

        // Act
        var exception = Assert.ThrowsAsync<KeyNotFoundException>(
            async () => await _service.DetachAsync(nodeId, networkId));

        // Assert
        Assert.That(
            exception!.Message,
            Does.Contain("is not attached"));

        _attachmentRepositoryMock.Verify(
            x => x.Remove(It.IsAny<NetworkAttachment>()),
            Times.Never);

        _attachmentRepositoryMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task DetachAsync_WhenAttachmentExists_ShouldRemoveAttachment()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        var networkId = Guid.NewGuid();

        var attachment = new NetworkAttachment(
            nodeId,
            networkId);

        _attachmentRepositoryMock
            .Setup(x => x.GetAsync(
                nodeId,
                networkId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(attachment);

        // Act
        await _service.DetachAsync(nodeId, networkId);

        // Assert
        _attachmentRepositoryMock.Verify(
            x => x.Remove(attachment),
            Times.Once);

        _attachmentRepositoryMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public void GetByNodeIdAsync_WhenNodeDoesNotExist_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var nodeId = Guid.NewGuid();

        _computeNodeRepositoryMock
            .Setup(x => x.GetByIdAsync(
                nodeId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ComputeNode?)null);

        // Act
        var exception = Assert.ThrowsAsync<KeyNotFoundException>(
            async () => await _service.GetByNodeIdAsync(nodeId));

        // Assert
        Assert.That(
            exception!.Message,
            Does.Contain($"Compute node with id '{nodeId}' was not found."));

        _attachmentRepositoryMock.Verify(
            x => x.GetByNodeIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task GetByNodeIdAsync_WhenNodeExists_ShouldReturnAttachments()
    {
        // Arrange
        var node = new ComputeNode(
            "node-1",
            "NVIDIA A100",
            4);

        var network1 = new Network("network-1");
        var network2 = new Network("network-2");

        var attachment1 = new NetworkAttachment(
            node.Id,
            network1.Id);

        var attachment2 = new NetworkAttachment(
            node.Id,
            network2.Id);

        _computeNodeRepositoryMock
            .Setup(x => x.GetByIdAsync(
                node.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(node);

        _attachmentRepositoryMock
            .Setup(x => x.GetByNodeIdAsync(
                node.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<NetworkAttachment>
            {
                attachment1,
                attachment2
            });

        // Act
        var result = await _service.GetByNodeIdAsync(node.Id);

        // Assert
        Assert.That(result, Has.Count.EqualTo(2));

        Assert.Multiple(() =>
        {
            Assert.That(result[0].Id, Is.EqualTo(attachment1.Id));
            Assert.That(result[0].NetworkId, Is.EqualTo(network1.Id));

            Assert.That(result[1].Id, Is.EqualTo(attachment2.Id));
            Assert.That(result[1].NetworkId, Is.EqualTo(network2.Id));
        });
    }

    [Test]
    public void GetByNetworkIdAsync_WhenNetworkDoesNotExist_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var networkId = Guid.NewGuid();

        _networkRepositoryMock
            .Setup(x => x.GetByIdAsync(
                networkId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Network?)null);

        // Act
        var exception = Assert.ThrowsAsync<KeyNotFoundException>(
            async () => await _service.GetByNetworkIdAsync(networkId));

        // Assert
        Assert.That(
            exception!.Message,
            Does.Contain($"Network with id '{networkId}' was not found."));

        _attachmentRepositoryMock.Verify(
            x => x.GetByNetworkIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task GetByNetworkIdAsync_WhenNetworkExists_ShouldReturnAttachments()
    {
        // Arrange
        var network = new Network("production-network");

        var node1 = new ComputeNode(
            "node-1",
            "NVIDIA A100",
            4);

        var node2 = new ComputeNode(
            "node-2",
            "NVIDIA H100",
            2);

        var attachment1 = new NetworkAttachment(
            node1.Id,
            network.Id);

        var attachment2 = new NetworkAttachment(
            node2.Id,
            network.Id);

        _networkRepositoryMock
            .Setup(x => x.GetByIdAsync(
                network.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(network);

        _attachmentRepositoryMock
            .Setup(x => x.GetByNetworkIdAsync(
                network.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<NetworkAttachment>
            {
                attachment1,
                attachment2
            });

        // Act
        var result = await _service.GetByNetworkIdAsync(network.Id);

        // Assert
        Assert.That(result, Has.Count.EqualTo(2));

        Assert.Multiple(() =>
        {
            Assert.That(result[0].Id, Is.EqualTo(attachment1.Id));
            Assert.That(result[0].ComputeNodeId, Is.EqualTo(node1.Id));

            Assert.That(result[1].Id, Is.EqualTo(attachment2.Id));
            Assert.That(result[1].ComputeNodeId, Is.EqualTo(node2.Id));
        });
    }
}