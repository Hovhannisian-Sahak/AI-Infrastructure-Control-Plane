using System.Net;
using BeeCloud.ApiTests.Clients;
using BeeCloud.ApiTests.Models;
using BeeCloud.ApiTests.TestData;
using BeeCloud.Domain.Enums;
using NUnit.Framework;

namespace BeeCloud.ApiTests;

[TestFixture]
public class IncidentApiTests
{
    private NodesClient _nodesClient = null!;
    private IncidentsClient _incidentsClient = null!;
    private ApiTestDataHelper _testData = null!;

    [SetUp]
    public void SetUp()
    {
        var baseUrl = TestConfiguration.BaseUrl;

        _nodesClient = new NodesClient(baseUrl);
        _incidentsClient = new IncidentsClient(baseUrl);
        _testData = new ApiTestDataHelper(_nodesClient);
    }

    [Test]
    public async Task GetAllIncidents_ShouldReturnOk()
    {
        var response = await _incidentsClient.GetAllAsync();

        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.OK));

        Assert.That(
            response.Data,
            Is.Not.Null);

        foreach (var incident in response.Data!)
        {
            AssertValidIncidentEnums(incident);
        }
    }
    [Test]
    public async Task GetIncidentById_WhenIncidentExists_ShouldReturnOk()
    {
        // Arrange
        var nodeId = await _testData.CreateAvailableNodeAsync();

        var startResponse =
            await _nodesClient.StartAsync(nodeId);

        Assert.That(
            startResponse.StatusCode,
            Is.EqualTo(HttpStatusCode.OK),
            $"Node start failed. Response: {startResponse.Content}");

        await _nodesClient.WaitForRunningAsync(nodeId);

        var simulateResponse =
            await _nodesClient.SimulateFaultAsync(
                nodeId,
                NodeFault.GpuFailure);

        Assert.That(
            simulateResponse.StatusCode,
            Is.EqualTo(HttpStatusCode.OK));

        var incident =
            await _incidentsClient.WaitForIncidentAsync(nodeId);

        // Act
        var response =
            await _incidentsClient.GetByIdAsync(incident.Id);

        // Assert
        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.OK));

        Assert.That(
            response.Data,
            Is.Not.Null);

        Assert.That(
            response.Data!.Id,
            Is.EqualTo(incident.Id));

        Assert.That(
            response.Data.ComputeNodeId,
            Is.EqualTo(nodeId));

        Assert.That(
            response.Data.Severity,
            Is.EqualTo(incident.Severity));

        Assert.That(
            response.Data.Status,
            Is.EqualTo(incident.Status));
    }
    [Test]
    public async Task GetIncidentById_WhenIncidentDoesNotExist_ShouldReturnNotFound()
    {
        var incidentId = Guid.NewGuid();

        var response =
            await _incidentsClient.GetByIdAsync(incidentId);

        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task SimulateGpuFailure_ShouldCreateOpenIncident()
    {
        var nodeId = await _testData.CreateAvailableNodeAsync();

        var startResponse =
            await _nodesClient.StartAsync(nodeId);

        Assert.That(
            startResponse.StatusCode,
            Is.EqualTo(HttpStatusCode.OK),
            $"Node start failed. Response: {startResponse.Content}");

        var runningNode =
            await _nodesClient.WaitForRunningAsync(nodeId);

        Assert.That(
            runningNode.Status,
            Is.EqualTo(nameof(NodeStatus.Running)));

        var simulateResponse =
            await _nodesClient.SimulateFaultAsync(
                nodeId,
                NodeFault.GpuFailure);

        Assert.That(
            simulateResponse.StatusCode,
            Is.EqualTo(HttpStatusCode.OK));

        Assert.That(
            simulateResponse.Data,
            Is.Not.Null);

        Assert.That(
            simulateResponse.Data!.ActiveFault,
            Is.EqualTo(nameof(NodeFault.GpuFailure)));

        var incident =
            await _incidentsClient.WaitForIncidentAsync(nodeId);

        Assert.That(
            incident.Id,
            Is.Not.EqualTo(Guid.Empty));

        Assert.That(
            incident.ComputeNodeId,
            Is.EqualTo(nodeId));

        Assert.That(
            incident.Status,
            Is.EqualTo(nameof(IncidentStatus.Open)));

        Assert.That(
            incident.Severity,
            Is.EqualTo(nameof(IncidentSeverity.High)));

        AssertValidIncidentEnums(incident);

        Assert.That(
            incident.CreatedAt,
            Is.Not.EqualTo(default(DateTime)));

        TestContext.WriteLine(
            $"Created incident: {incident.Id}");

        TestContext.WriteLine(
            $"Severity: {incident.Severity}");

        TestContext.WriteLine(
            $"Status: {incident.Status}");
    }

    [Test]
    public async Task SimulateGpuFailure_ShouldCreateIncidentAndRecoverNode()
    {
        var nodeId = await _testData.CreateAvailableNodeAsync();

        var startResponse =
            await _nodesClient.StartAsync(nodeId);

        Assert.That(
            startResponse.StatusCode,
            Is.EqualTo(HttpStatusCode.OK),
            $"Node start failed. Response: {startResponse.Content}");

        var runningNode =
            await _nodesClient.WaitForRunningAsync(nodeId);

        Assert.That(
            runningNode.Status,
            Is.EqualTo(nameof(NodeStatus.Running)));

        var simulateResponse =
            await _nodesClient.SimulateFaultAsync(
                nodeId,
                NodeFault.GpuFailure);

        Assert.That(
            simulateResponse.StatusCode,
            Is.EqualTo(HttpStatusCode.OK));

        Assert.That(
            simulateResponse.Data,
            Is.Not.Null);

        Assert.That(
            simulateResponse.Data!.ActiveFault,
            Is.EqualTo(nameof(NodeFault.GpuFailure)));

        var incident =
            await _incidentsClient.WaitForIncidentAsync(nodeId);

        Assert.That(
            incident.ComputeNodeId,
            Is.EqualTo(nodeId));

        Assert.That(
            incident.Status,
            Is.EqualTo(nameof(IncidentStatus.Open)));

        Assert.That(
            incident.Severity,
            Is.EqualTo(nameof(IncidentSeverity.High)));

        AssertValidIncidentEnums(incident);

        var recoveredNode =
            await _nodesClient.WaitForAvailableAsync(
                nodeId,
                TimeSpan.FromSeconds(30));

        Assert.That(
            recoveredNode.Status,
            Is.EqualTo(nameof(NodeStatus.Available)));

        Assert.That(
            recoveredNode.ActiveFault,
            Is.EqualTo(nameof(NodeFault.None)));

        var resolvedIncident =
            await _incidentsClient.WaitForResolvedIncidentAsync(
                incident.Id);

        Assert.That(
            resolvedIncident.Id,
            Is.EqualTo(incident.Id));

        Assert.That(
            resolvedIncident.ComputeNodeId,
            Is.EqualTo(nodeId));

        Assert.That(
            resolvedIncident.Status,
            Is.EqualTo(nameof(IncidentStatus.Resolved)));

        AssertValidIncidentEnums(resolvedIncident);

        Assert.That(
            resolvedIncident.ResolvedAt,
            Is.Not.Null);

        TestContext.WriteLine(
            $"Incident {incident.Id} was resolved.");

        TestContext.WriteLine(
            $"Node {nodeId} recovered to Available.");
    }
    [TestCaseSource(nameof(AllIncidentSeverities))]
    public async Task GetIncidentsBySeverity_ShouldReturnOnlyMatchingIncidents(
        IncidentSeverity severity)
    {
        // Act
        var response =
            await _incidentsClient.GetAllAsync(
                severity: severity);

        // Assert
        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.OK));

        Assert.That(
            response.Data,
            Is.Not.Null);

        Assert.That(
            response.Data!,
            Has.All.Matches<IncidentResponseModel>(
                x => x.Severity == severity.ToString()));
    }
    [TestCaseSource(nameof(AllIncidentStatuses))]
    public async Task GetIncidentsByStatus_ShouldReturnOnlyMatchingIncidents(
        IncidentStatus status)
    {
        // Act
        var response =
            await _incidentsClient.GetAllAsync(
                status: status);

        // Assert
        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.OK));

        Assert.That(
            response.Data,
            Is.Not.Null);

        Assert.That(
            response.Data!,
            Has.All.Matches<IncidentResponseModel>(
                x => x.Status == status.ToString()));
    }
    private static IEnumerable<IncidentSeverity> AllIncidentSeverities =>
        Enum.GetValues<IncidentSeverity>();
    private static IEnumerable<IncidentStatus> AllIncidentStatuses =>
        Enum.GetValues<IncidentStatus>();
    private static void AssertValidIncidentEnums(
        IncidentResponseModel incident)
    {
        Assert.That(
            Enum.TryParse<IncidentSeverity>(
                incident.Severity,
                ignoreCase: true,
                out _),
            Is.True,
            $"API returned invalid incident severity: '{incident.Severity}'");

        Assert.That(
            Enum.TryParse<IncidentStatus>(
                incident.Status,
                ignoreCase: true,
                out _),
            Is.True,
            $"API returned invalid incident status: '{incident.Status}'");
    }
}