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

    [SetUp]
    public void SetUp()
    {
        var baseUrl = TestConfiguration.BaseUrl;

        _nodesClient = new NodesClient(baseUrl);
        _incidentsClient = new IncidentsClient(baseUrl);
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
        var nodeId = await CreateAvailableNodeAsync();

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
        var nodeId = await CreateAvailableNodeAsync();

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

        Assert.That(
            Enum.TryParse<IncidentStatus>(
                resolvedIncident.Status,
                ignoreCase: true,
                out _),
            Is.True,
            $"API returned invalid incident status: '{resolvedIncident.Status}'");

        Assert.That(
            resolvedIncident.ResolvedAt,
            Is.Not.Null);

        TestContext.WriteLine(
            $"Incident {incident.Id} was resolved.");

        TestContext.WriteLine(
            $"Node {nodeId} recovered to Available.");
    }

    private async Task<Guid> CreateNodeAsync()
    {
        var request =
            TestDataFactory.CreateNodeRequest();

        var response =
            await _nodesClient.CreateAsync(request);

        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.Accepted),
            $"Node creation failed. Response: {response.Content}");

        Assert.That(
            response.Data,
            Is.Not.Null);

        var nodeId = response.Data!.Id;

        Assert.That(
            nodeId,
            Is.Not.EqualTo(Guid.Empty));

        TestContext.WriteLine(
            $"Created node: {nodeId}");

        return nodeId;
    }

    private async Task<Guid> CreateAvailableNodeAsync()
    {
        var nodeId = await CreateNodeAsync();

        var node =
            await _nodesClient.WaitForAvailableAsync(nodeId);

        Assert.That(
            node.Status,
            Is.EqualTo(nameof(NodeStatus.Available)));

        return nodeId;
    }
}