using System.Net;
using System.Text.Json;
using BeeCloud.ApiTests.Clients;
using BeeCloud.Domain.Enums;

namespace BeeCloud.ApiTests;

[TestFixture]
public class NodesApiTests
{
    private NodesClient _nodesClient = null!;

    [SetUp]
    public void SetUp()
    {
        _nodesClient = new NodesClient(
            TestConfiguration.BaseUrl);
    }

    [Test]
    public async Task CreateNode_WithValidData_ShouldReturnAccepted()
    {
        // Arrange
        var name = $"api-test-node-{Guid.NewGuid():N}";
        
        // Act
        var response =
            await _nodesClient.CreateAsync(
                name,
                "NVIDIA A100",
                2);

        // Assert
        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.Accepted));

        Assert.That(
            response.Content,
            Is.Not.Null);

        TestContext.WriteLine(
            $"Create node response: {response.Content}");

        using var document =
            JsonDocument.Parse(response.Content!);

        var status =
            document.RootElement
                .GetProperty("status")
                .GetString();

        Assert.That(
            status,
            Is.EqualTo(nameof(NodeStatus.Provisioning)));
    }

    [Test]
    public async Task CreateNode_WithInvalidData_ShouldReturnBadRequest()
    {
        // Act
        var response =
            await _nodesClient.CreateAsync(
                "",
                "",
                0);

        // Assert
        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.BadRequest));

        Assert.That(
            response.Content,
            Is.Not.Null);

        TestContext.WriteLine(
            $"Invalid create node response: {response.Content}");
    }

    [Test]
    public async Task GetNodeById_WhenNodeExists_ShouldReturnNode()
    {
        // Arrange
        var nodeId =
            await CreateNodeAsync();

        // Wait until provisioning is completed.
        await _nodesClient.WaitForAvailableAsync(nodeId);

        // Act
        var response =
            await _nodesClient.GetByIdAsync(nodeId);

        // Assert
        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.OK));

        Assert.That(
            response.Content,
            Is.Not.Null);

        TestContext.WriteLine(
            $"Get node response: {response.Content}");

        using var document =
            JsonDocument.Parse(response.Content!);

        var returnedId =
            document.RootElement
                .GetProperty("id")
                .GetGuid();

        var status =
            document.RootElement
                .GetProperty("status")
                .GetString();

        Assert.That(
            returnedId,
            Is.EqualTo(nodeId));

        Assert.That(
            status,
            Is.EqualTo(nameof(NodeStatus.Available)));
    }

    [Test]
    public async Task GetNodeById_WhenNodeDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        var nodeId = Guid.NewGuid();

        // Act
        var response =
            await _nodesClient.GetByIdAsync(nodeId);

        // Assert
        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.NotFound));

        TestContext.WriteLine(
            $"Get non-existing node response: {response.Content}");
    }

    [Test]
    public async Task StartNode_WhenNodeIsAvailable_ShouldReturnRunning()
    {
        // Arrange
        var nodeId =
            await CreateNodeAsync();

        await _nodesClient.WaitForAvailableAsync(nodeId);

        // Act
        var response =
            await _nodesClient.StartAsync(nodeId);

        // Assert
        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.OK));

        Assert.That(
            response.Content,
            Is.Not.Null);

        TestContext.WriteLine(
            $"Start node response: {response.Content}");

        using var document =
            JsonDocument.Parse(response.Content!);

        var status =
            document.RootElement
                .GetProperty("status")
                .GetString();

        Assert.That(
            status,
            Is.EqualTo(nameof(NodeStatus.Running)));
    }

    [Test]
    public async Task StartNode_WhenNodeIsAlreadyRunning_ShouldReturnConflict()
    {
        // Arrange
        var nodeId =
            await CreateNodeAsync();

        await _nodesClient.WaitForAvailableAsync(nodeId);

        var firstStartResponse =
            await _nodesClient.StartAsync(nodeId);

        Assert.That(
            firstStartResponse.StatusCode,
            Is.EqualTo(HttpStatusCode.OK));

        // Act
        var secondStartResponse =
            await _nodesClient.StartAsync(nodeId);

        // Assert
        Assert.That(
            secondStartResponse.StatusCode,
            Is.EqualTo(HttpStatusCode.Conflict));

        TestContext.WriteLine(
            $"Second start response: {secondStartResponse.Content}");
    }

    [Test]
    public async Task StopNode_WhenNodeIsRunning_ShouldReturnStopping()
    {
        // Arrange
        var nodeId =
            await CreateNodeAsync();

        await _nodesClient.WaitForAvailableAsync(nodeId);

        var startResponse =
            await _nodesClient.StartAsync(nodeId);

        Assert.That(
            startResponse.StatusCode,
            Is.EqualTo(HttpStatusCode.OK));

        // Act
        var response =
            await _nodesClient.StopAsync(nodeId);

        // Assert
        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.OK));

        Assert.That(
            response.Content,
            Is.Not.Null);

        TestContext.WriteLine(
            $"Stop node response: {response.Content}");

        using var document =
            JsonDocument.Parse(response.Content!);

        var status =
            document.RootElement
                .GetProperty("status")
                .GetString();

        Assert.That(
            status,
            Is.EqualTo(nameof(NodeStatus.Stopping)));
    }

    [Test]
    public async Task StopNode_WhenNodeIsNotRunning_ShouldReturnConflict()
    {
        // Arrange
        var nodeId =
            await CreateNodeAsync();

        await _nodesClient.WaitForAvailableAsync(nodeId);

        // Act
        var response =
            await _nodesClient.StopAsync(nodeId);

        // Assert
        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.Conflict));

        TestContext.WriteLine(
            $"Stop non-running node response: {response.Content}");
    }
    
    [Test]
    public async Task SimulateFault_WhenNodeDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        var nodeId = Guid.NewGuid();

        // Act
        var response =
            await _nodesClient.SimulateFaultAsync(
                nodeId,
                NodeFault.GpuFailure);

        // Assert
        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.NotFound));

        TestContext.WriteLine(
            $"Simulation for non-existing node: {response.Content}");
    }
    
    [Test]
    [TestCase(NodeFault.GpuFailure)]
    [TestCase(NodeFault.GpuOverheat)]
    [TestCase(NodeFault.NetworkFailure)]
    [TestCase(NodeFault.ServiceCrash)]
    public async Task SimulateFault_WhenValidFaultIsRequested_ShouldReturnRequestedFault(
        NodeFault fault)
    {
        // Arrange
        var nodeId = await CreateNodeAsync();

        await _nodesClient.WaitForAvailableAsync(nodeId);

        // Act
        var response =
            await _nodesClient.SimulateFaultAsync(
                nodeId,
                fault);

        // Assert
        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.OK));

        using var document =
            JsonDocument.Parse(response.Content!);

        var activeFault =
            document.RootElement
                .GetProperty("activeFault")
                .GetString();

        Assert.That(
            activeFault,
            Is.EqualTo(fault.ToString()));
    }
    [Test]
    public async Task SimulateFault_WhenNoneIsRequested_ShouldReturnBadRequest()
    {
        // Arrange
        var nodeId = await CreateNodeAsync();

        await _nodesClient.WaitForAvailableAsync(nodeId);

        // Act
        var response =
            await _nodesClient.SimulateFaultAsync(
                nodeId,
                NodeFault.None);

        // Assert
        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.BadRequest));
    }

    private async Task<Guid> CreateNodeAsync()
    {
        var response =
            await _nodesClient.CreateAsync(
                $"api-test-node-{Guid.NewGuid():N}",
                "NVIDIA A100",
                2);

        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.Accepted));

        Assert.That(
            response.Content,
            Is.Not.Null);

        using var document =
            JsonDocument.Parse(response.Content!);

        var nodeId =
            document.RootElement
                .GetProperty("id")
                .GetGuid();

        Assert.That(
            nodeId,
            Is.Not.EqualTo(Guid.Empty));

        return nodeId;
    }
}