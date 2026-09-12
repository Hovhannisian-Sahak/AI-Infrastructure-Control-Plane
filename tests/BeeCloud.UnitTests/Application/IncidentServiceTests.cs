using BeeCloud.Application.DTOs.Incidents;
using BeeCloud.Application.Interfaces;
using BeeCloud.Application.Services;
using BeeCloud.Domain.Entities;
using BeeCloud.Domain.Enums;
using Moq;
using NUnit.Framework;

namespace BeeCloud.UnitTests.Application;

[TestFixture]
public class IncidentServiceTests
{
    private Mock<IIncidentRepository> _incidentRepository = null!;
    private Mock<IComputeNodeRepository> _computeNodeRepository = null!;
    private IncidentService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _incidentRepository = new Mock<IIncidentRepository>();
        _computeNodeRepository = new Mock<IComputeNodeRepository>();

        _service = new IncidentService(
            _incidentRepository.Object,
            _computeNodeRepository.Object);
    }

    [Test]
    public async Task CreateAsync_WhenNodeExists_ShouldCreateIncident()
    {
        var node = CreateNode();

        _computeNodeRepository
            .Setup(repository => repository.GetByIdAsync(
                node.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(node);

        var request = new CreateIncidentRequest
        {
            ComputeNodeId = node.Id,
            Severity = IncidentSeverity.Critical,
            Title = "GPU overheating",
            Description = "GPU temperature exceeded the threshold."
        };

        var result = await _service.CreateAsync(request);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.ComputeNodeId, Is.EqualTo(node.Id));
        Assert.That(result.Severity, Is.EqualTo(IncidentSeverity.Critical));
        Assert.That(result.Status, Is.EqualTo(IncidentStatus.Open));
        Assert.That(result.Title, Is.EqualTo("GPU overheating"));

        _incidentRepository.Verify(
            repository => repository.AddAsync(
                It.IsAny<Incident>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _incidentRepository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public void CreateAsync_WhenNodeDoesNotExist_ShouldThrow()
    {
        var nodeId = Guid.NewGuid();

        _computeNodeRepository
            .Setup(repository => repository.GetByIdAsync(
                nodeId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ComputeNode?)null);

        var request = new CreateIncidentRequest
        {
            ComputeNodeId = nodeId,
            Severity = IncidentSeverity.High,
            Title = "GPU failure"
        };

        var exception = Assert.ThrowsAsync<KeyNotFoundException>(
            async () => await _service.CreateAsync(request));

        Assert.That(
            exception!.Message,
            Does.Contain(nodeId.ToString()));

        _incidentRepository.Verify(
            repository => repository.AddAsync(
                It.IsAny<Incident>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public void CreateAsync_WhenNodeIdIsEmpty_ShouldThrow()
    {
        var request = new CreateIncidentRequest
        {
            ComputeNodeId = Guid.Empty,
            Severity = IncidentSeverity.High,
            Title = "GPU failure"
        };

        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.CreateAsync(request));
    }

    [Test]
    public void CreateAsync_WhenTitleIsEmpty_ShouldThrow()
    {
        var node = CreateNode();

        _computeNodeRepository
            .Setup(repository => repository.GetByIdAsync(
                node.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(node);

        var request = new CreateIncidentRequest
        {
            ComputeNodeId = node.Id,
            Severity = IncidentSeverity.High,
            Title = ""
        };

        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.CreateAsync(request));
    }

    [Test]
    public async Task StartInvestigationAsync_WhenIncidentIsOpen_ShouldInvestigate()
    {
        var incident = CreateIncident();

        _incidentRepository
            .Setup(repository => repository.GetByIdAsync(
                incident.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(incident);

        var result = await _service.StartInvestigationAsync(incident.Id);

        Assert.That(
            result.Status,
            Is.EqualTo(IncidentStatus.Investigating));

        _incidentRepository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task ResolveAsync_WhenIncidentIsOpen_ShouldResolve()
    {
        var incident = CreateIncident();

        _incidentRepository
            .Setup(repository => repository.GetByIdAsync(
                incident.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(incident);

        var result = await _service.ResolveAsync(incident.Id);

        Assert.That(
            result.Status,
            Is.EqualTo(IncidentStatus.Resolved));

        Assert.That(result.ResolvedAt, Is.Not.Null);

        _incidentRepository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public void GetByIdAsync_WhenIncidentDoesNotExist_ShouldReturnNull()
    {
        var incidentId = Guid.NewGuid();

        _incidentRepository
            .Setup(repository => repository.GetByIdAsync(
                incidentId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Incident?)null);

        var result = _service.GetByIdAsync(incidentId);

        Assert.That(result.Result, Is.Null);
    }

    [Test]
    public async Task StartInvestigationAsync_WhenIncidentDoesNotExist_ShouldThrow()
    {
        var incidentId = Guid.NewGuid();

        _incidentRepository
            .Setup(repository => repository.GetByIdAsync(
                incidentId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Incident?)null);

        Assert.ThrowsAsync<KeyNotFoundException>(
            async () => await _service.StartInvestigationAsync(incidentId));
    }

    [Test]
    public async Task ResolveAsync_WhenIncidentDoesNotExist_ShouldThrow()
    {
        var incidentId = Guid.NewGuid();

        _incidentRepository
            .Setup(repository => repository.GetByIdAsync(
                incidentId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Incident?)null);

        Assert.ThrowsAsync<KeyNotFoundException>(
            async () => await _service.ResolveAsync(incidentId));
    }

    private static ComputeNode CreateNode()
    {
        return new ComputeNode(
            "test-node",
            "NVIDIA H100",
            8);
    }

    private static Incident CreateIncident()
    {
        return new Incident(
            Guid.NewGuid(),
            IncidentSeverity.High,
            "GPU failure",
            "GPU became unavailable.");
    }
}