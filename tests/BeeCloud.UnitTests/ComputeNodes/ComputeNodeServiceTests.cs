using BeeCloud.Application.DTOs.ComputeNodes;
using BeeCloud.Application.Interfaces;
using BeeCloud.Application.Services;
using BeeCloud.Domain.Entities;
using BeeCloud.Domain.Enums;
using Moq;

namespace BeeCloud.UnitTests.ComputeNodes;

[TestFixture]
public class ComputeNodeServiceTests
{
    private Mock<IComputeNodeRepository> _repository = null!;
    private Mock<IProvisioningQueue> _provisioningQueue = null!;
    private ComputeNodeService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _repository = new Mock<IComputeNodeRepository>();
        _provisioningQueue = new Mock<IProvisioningQueue>();

        _service = new ComputeNodeService(
            _repository.Object,
            _provisioningQueue.Object);
    }

    [Test]
    public async Task CreateAsync_WithValidRequest_ShouldCreateNode()
    {
        // Arrange
        var request = new CreateComputeNodeRequest
        {
            Name = "test-node",
            GpuModel = "NVIDIA A100",
            GpuCount = 4
        };

        _repository
            .Setup(repository => repository.ExistsByNameAsync(
                request.Name,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _service.CreateAsync(request);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Name, Is.EqualTo(request.Name));
        Assert.That(result.GpuModel, Is.EqualTo(request.GpuModel));
        Assert.That(result.GpuCount, Is.EqualTo(request.GpuCount));
        Assert.That(
            result.Status,
            Is.EqualTo(NodeStatus.Provisioning.ToString()));

        _repository.Verify(
            repository => repository.AddAsync(
                It.Is<ComputeNode>(node =>
                    node.Name == request.Name &&
                    node.GpuModel == request.GpuModel &&
                    node.GpuCount == request.GpuCount &&
                    node.Status == NodeStatus.Provisioning),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _repository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task CreateAsync_WithValidRequest_ShouldEnqueueNodeForProvisioning()
    {
        // Arrange
        var request = new CreateComputeNodeRequest
        {
            Name = "test-node",
            GpuModel = "NVIDIA A100",
            GpuCount = 4
        };

        _repository
            .Setup(repository => repository.ExistsByNameAsync(
                request.Name,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _service.CreateAsync(request);

        // Assert
        _provisioningQueue.Verify(
            queue => queue.EnqueueAsync(
                result.Id,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task CreateAsync_WithDuplicateName_ShouldThrow()
    {
        // Arrange
        var request = new CreateComputeNodeRequest
        {
            Name = "existing-node",
            GpuModel = "NVIDIA A100",
            GpuCount = 4
        };

        _repository
            .Setup(repository => repository.ExistsByNameAsync(
                request.Name,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act & Assert
        var exception = Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _service.CreateAsync(request));

        Assert.That(
            exception!.Message,
            Does.Contain("existing-node"));

        _repository.Verify(
            repository => repository.AddAsync(
                It.IsAny<ComputeNode>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _provisioningQueue.Verify(
            queue => queue.EnqueueAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public void CreateAsync_WithEmptyName_ShouldThrow()
    {
        // Arrange
        var request = new CreateComputeNodeRequest
        {
            Name = "",
            GpuModel = "NVIDIA A100",
            GpuCount = 4
        };

        // Act & Assert
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.CreateAsync(request));

        _repository.VerifyNoOtherCalls();
        _provisioningQueue.VerifyNoOtherCalls();
    }

    [Test]
    public void CreateAsync_WithEmptyGpuModel_ShouldThrow()
    {
        // Arrange
        var request = new CreateComputeNodeRequest
        {
            Name = "test-node",
            GpuModel = "",
            GpuCount = 4
        };

        // Act & Assert
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.CreateAsync(request));

        _repository.VerifyNoOtherCalls();
        _provisioningQueue.VerifyNoOtherCalls();
    }

    [Test]
    public void CreateAsync_WithInvalidGpuCount_ShouldThrow()
    {
        // Arrange
        var request = new CreateComputeNodeRequest
        {
            Name = "test-node",
            GpuModel = "NVIDIA A100",
            GpuCount = 0
        };

        // Act & Assert
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.CreateAsync(request));

        _repository.VerifyNoOtherCalls();
        _provisioningQueue.VerifyNoOtherCalls();
    }

    [Test]
    public async Task GetByIdAsync_WhenNodeExists_ShouldReturnNode()
    {
        // Arrange
        var node = CreateNode();

        _repository
            .Setup(repository => repository.GetByIdAsync(
                node.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(node);

        // Act
        var result = await _service.GetByIdAsync(node.Id);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Id, Is.EqualTo(node.Id));
        Assert.That(result.Name, Is.EqualTo(node.Name));
        Assert.That(result.GpuModel, Is.EqualTo(node.GpuModel));
        Assert.That(result.GpuCount, Is.EqualTo(node.GpuCount));
        Assert.That(result.Status, Is.EqualTo(node.Status.ToString()));
    }

    [Test]
    public async Task GetByIdAsync_WhenNodeDoesNotExist_ShouldReturnNull()
    {
        // Arrange
        var nodeId = Guid.NewGuid();

        _repository
            .Setup(repository => repository.GetByIdAsync(
                nodeId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ComputeNode?)null);

        // Act
        var result = await _service.GetByIdAsync(nodeId);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetAllAsync_ShouldReturnMappedNodes()
    {
        // Arrange
        var nodes = new[]
        {
            CreateNode(),
            CreateNode()
        };

        _repository
            .Setup(repository => repository.GetAllAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(nodes);

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result[0].Id, Is.EqualTo(nodes[0].Id));
        Assert.That(result[1].Id, Is.EqualTo(nodes[1].Id));
    }

    [Test]
    public async Task StartAsync_WhenNodeExists_ShouldStartNode()
    {
        // Arrange
        var node = CreateAvailableNode();

        _repository
            .Setup(repository => repository.GetByIdAsync(
                node.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(node);

        // Act
        var result = await _service.StartAsync(node.Id);

        // Assert
        Assert.That(node.Status, Is.EqualTo(NodeStatus.Running));
        Assert.That(result.Status, Is.EqualTo(NodeStatus.Running.ToString()));

        _repository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public void StartAsync_WhenNodeDoesNotExist_ShouldThrow()
    {
        // Arrange
        var nodeId = Guid.NewGuid();

        _repository
            .Setup(repository => repository.GetByIdAsync(
                nodeId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ComputeNode?)null);

        // Act & Assert
        Assert.ThrowsAsync<KeyNotFoundException>(
            async () => await _service.StartAsync(nodeId));

        _repository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task StopAsync_WhenNodeExists_ShouldStopNode()
    {
        // Arrange
        var node = CreateRunningNode();

        _repository
            .Setup(repository => repository.GetByIdAsync(
                node.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(node);

        // Act
        var result = await _service.StopAsync(node.Id);

        // Assert
        Assert.That(node.Status, Is.EqualTo(NodeStatus.Stopping));
        Assert.That(result.Status, Is.EqualTo(NodeStatus.Stopping.ToString()));

        _repository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public void StopAsync_WhenNodeDoesNotExist_ShouldThrow()
    {
        // Arrange
        var nodeId = Guid.NewGuid();

        _repository
            .Setup(repository => repository.GetByIdAsync(
                nodeId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ComputeNode?)null);

        // Act & Assert
        Assert.ThrowsAsync<KeyNotFoundException>(
            async () => await _service.StopAsync(nodeId));

        _repository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
    [Test]
    public async Task SimulateFaultAsync_WhenNodeExists_ShouldSetFault()
    {
        // Arrange
        var node = new ComputeNode(
            "test-node",
            "NVIDIA A100",
            2);

        _repository
            .Setup(x => x.GetByIdAsync(
                node.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(node);

        // Act
        await _service.SimulateFaultAsync(
            node.Id,
            NodeFault.GpuFailure);

        // Assert
        Assert.That(
            node.ActiveFault,
            Is.EqualTo(NodeFault.GpuFailure));

        _repository.Verify(
            x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
    [Test]
    public void SimulateFaultAsync_WhenNodeDoesNotExist_ShouldThrowNotFound()
    {
        // Arrange
        var nodeId = Guid.NewGuid();

        _repository
            .Setup(x => x.GetByIdAsync(
                nodeId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ComputeNode?)null);

        // Act
        var exception = Assert.ThrowsAsync<KeyNotFoundException>(
            async () =>
                await _service.SimulateFaultAsync(
                    nodeId,
                    NodeFault.GpuFailure));

        // Assert
        Assert.That(
            exception!.Message,
            Does.Contain(nodeId.ToString()));

        _repository.Verify(
            x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
    private static ComputeNode CreateNode()
    {
        return new ComputeNode(
            $"test-node-{Guid.NewGuid():N}",
            "NVIDIA A100",
            4);
    }

    private static ComputeNode CreateAvailableNode()
    {
        var node = CreateNode();

        node.MarkAvailable();

        return node;
    }

    private static ComputeNode CreateRunningNode()
    {
        var node = CreateAvailableNode();

        node.Start();

        return node;
    }
}