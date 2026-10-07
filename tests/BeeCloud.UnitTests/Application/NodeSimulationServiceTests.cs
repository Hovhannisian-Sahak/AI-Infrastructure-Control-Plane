using BeeCloud.Application.Interfaces;
using BeeCloud.Application.Services;
using BeeCloud.Domain.Entities;
using BeeCloud.Domain.Enums;
using Moq;

namespace BeeCloud.UnitTests.Application;

[TestFixture]
public class NodeSimulationServiceTests
{
    private Mock<IComputeNodeRepository> _repository = null!;
    private NodeSimulationService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _repository = new Mock<IComputeNodeRepository>();
        _service = new NodeSimulationService(_repository.Object);
        _repository
            .Setup(repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    [Test]
    public async Task SimulateFaultAsync_WhenNodeExists_PersistsAndReturnsFault()
    {
        var node = CreateNode();
        _repository
            .Setup(repository => repository.GetByIdAsync(
                node.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(node);

        var response = await _service.SimulateFaultAsync(
            node.Id,
            NodeFault.GpuFailure);

        Assert.That(node.ActiveFault, Is.EqualTo(NodeFault.GpuFailure));
        Assert.That(response.Id, Is.EqualTo(node.Id));
        Assert.That(response.Name, Is.EqualTo(node.Name));
        Assert.That(response.Status, Is.EqualTo(node.Status.ToString()));
        Assert.That(response.ActiveFault, Is.EqualTo(nameof(NodeFault.GpuFailure)));
        _repository.Verify(repository =>
            repository.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public void SimulateFaultAsync_WhenFaultIsNone_ThrowsWithoutSaving()
    {
        var node = CreateNode();
        _repository
            .Setup(repository => repository.GetByIdAsync(
                node.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(node);

        Assert.ThrowsAsync<ArgumentException>(async () =>
            await _service.SimulateFaultAsync(node.Id, NodeFault.None));

        _repository.Verify(repository =>
            repository.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public void SimulateFaultAsync_WhenNodeDoesNotExist_ThrowsNotFound()
    {
        var nodeId = Guid.NewGuid();
        _repository
            .Setup(repository => repository.GetByIdAsync(
                nodeId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ComputeNode?)null);

        Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            await _service.SimulateFaultAsync(nodeId, NodeFault.GpuFailure));
    }

    [Test]
    public async Task ClearFaultAsync_WhenNodeExists_ClearsAndPersistsFault()
    {
        var node = CreateNode();
        node.SimulateFault(NodeFault.NetworkFailure);
        _repository
            .Setup(repository => repository.GetByIdAsync(
                node.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(node);

        var response = await _service.ClearFaultAsync(node.Id);

        Assert.That(node.ActiveFault, Is.EqualTo(NodeFault.None));
        Assert.That(response.ActiveFault, Is.EqualTo(nameof(NodeFault.None)));
        Assert.That(response.Id, Is.EqualTo(node.Id));
        _repository.Verify(repository =>
            repository.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static ComputeNode CreateNode()
    {
        return new ComputeNode(
            $"simulation-unit-test-{Guid.NewGuid():N}",
            "NVIDIA A100",
            1);
    }
}
