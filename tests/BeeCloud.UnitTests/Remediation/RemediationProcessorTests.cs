using BeeCloud.Application.Interfaces;
using BeeCloud.Domain.Entities;
using BeeCloud.Domain.Enums;
using BeeCloud.Worker.Processors;
using Microsoft.Extensions.Logging;
using Moq;

namespace BeeCloud.UnitTests.Remediation;

[TestFixture]
public class RemediationProcessorTests
{
    private Mock<IComputeNodeRepository> _nodeRepository = null!;
    private Mock<IIncidentService> _incidentService = null!;
    private Mock<IOperationalMetricsService>
        _operationalMetricsService = null!;
    private Mock<ILogger<RemediationProcessor>> _logger = null!;

    private RemediationProcessor _processor = null!;

    [SetUp]
    public void SetUp()
    {
        _nodeRepository =
            new Mock<IComputeNodeRepository>();

        _incidentService =
            new Mock<IIncidentService>();

        _operationalMetricsService =
            new Mock<IOperationalMetricsService>();

        _logger =
            new Mock<ILogger<RemediationProcessor>>();

        _processor = new RemediationProcessor(
            _nodeRepository.Object,
            _incidentService.Object,
            _operationalMetricsService.Object,
            _logger.Object);
    }

    [Test]
    public async Task ProcessAsync_WithNoUnhealthyOrQuarantinedNodes_ShouldDoNothing()
    {
        _nodeRepository
            .Setup(repository =>
                repository.GetByStatusAsync(
                    NodeStatus.Unhealthy,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ComputeNode>());

        _nodeRepository
            .Setup(repository =>
                repository.GetByStatusAsync(
                    NodeStatus.Quarantined,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ComputeNode>());

        await _processor.ProcessAsync();

        _operationalMetricsService.Verify(
            service =>
                service.IncrementRemediationAsync(
                    It.IsAny<CancellationToken>()),
            Times.Never);

        _operationalMetricsService.Verify(
            service =>
                service.IncrementRemediationFailureAsync(
                    It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task ProcessAsync_WithQuarantinedNode_ShouldIncrementRemediationMetric()
    {
        var node = CreateQuarantinedNode();

        _nodeRepository
            .Setup(repository =>
                repository.GetByStatusAsync(
                    NodeStatus.Unhealthy,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ComputeNode>());

        _nodeRepository
            .Setup(repository =>
                repository.GetByStatusAsync(
                    NodeStatus.Quarantined,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ComputeNode> { node });

        await _processor.ProcessAsync();

        _operationalMetricsService.Verify(
            service =>
                service.IncrementRemediationAsync(
                    It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task ProcessAsync_WithServiceCrash_ShouldIncrementRemediationFailureMetric()
    {
        var node = CreateQuarantinedServiceCrashNode();

        _nodeRepository
            .Setup(repository =>
                repository.GetByStatusAsync(
                    NodeStatus.Unhealthy,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ComputeNode>());

        _nodeRepository
            .Setup(repository =>
                repository.GetByStatusAsync(
                    NodeStatus.Quarantined,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ComputeNode> { node });

        await _processor.ProcessAsync();

        _operationalMetricsService.Verify(
            service =>
                service.IncrementRemediationFailureAsync(
                    It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task ProcessAsync_WithQuarantinedNode_ShouldResolveIncident()
    {
        var node = CreateQuarantinedNode();

        _nodeRepository
            .Setup(repository =>
                repository.GetByStatusAsync(
                    NodeStatus.Unhealthy,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ComputeNode>());

        _nodeRepository
            .Setup(repository =>
                repository.GetByStatusAsync(
                    NodeStatus.Quarantined,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ComputeNode> { node });

        await _processor.ProcessAsync();

        _incidentService.Verify(
            service =>
                service.ResolveForNodeAsync(
                    node.Id,
                    It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task ProcessAsync_WithSuccessfulRemediation_ShouldMakeNodeAvailable()
    {
        var node = CreateQuarantinedNode();

        _nodeRepository
            .Setup(repository =>
                repository.GetByStatusAsync(
                    NodeStatus.Unhealthy,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ComputeNode>());

        _nodeRepository
            .Setup(repository =>
                repository.GetByStatusAsync(
                    NodeStatus.Quarantined,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ComputeNode> { node });

        await _processor.ProcessAsync();

        Assert.That(
            node.Status,
            Is.EqualTo(NodeStatus.Available));

        Assert.That(
            node.ActiveFault,
            Is.EqualTo(NodeFault.None));
    }

    [Test]
    public async Task ProcessAsync_WithServiceCrash_ShouldMarkNodeAsFailed()
    {
        var node = CreateQuarantinedServiceCrashNode();

        _nodeRepository
            .Setup(repository =>
                repository.GetByStatusAsync(
                    NodeStatus.Unhealthy,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ComputeNode>());

        _nodeRepository
            .Setup(repository =>
                repository.GetByStatusAsync(
                    NodeStatus.Quarantined,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ComputeNode> { node });

        await _processor.ProcessAsync();

        Assert.That(
            node.Status,
            Is.EqualTo(NodeStatus.Failed));

        Assert.That(
            node.ActiveFault,
            Is.EqualTo(NodeFault.ServiceCrash));
    }

    private static ComputeNode CreateQuarantinedNode()
    {
        var node = new ComputeNode(
            $"remediation-test-node-{Guid.NewGuid():N}",
            "NVIDIA A100",
            4);

        node.MarkAvailable();
        node.Start();
        node.MarkUnhealthy();
        node.Quarantine();

        return node;
    }

    private static ComputeNode CreateQuarantinedServiceCrashNode()
    {
        var node = CreateQuarantinedNode();

        node.SimulateFault(
            NodeFault.ServiceCrash);

        return node;
    }
}