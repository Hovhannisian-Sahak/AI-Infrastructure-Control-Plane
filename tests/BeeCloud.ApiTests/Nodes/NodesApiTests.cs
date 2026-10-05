using System.Net;
using BeeCloud.ApiTests.Clients;
using BeeCloud.ApiTests.Models;
using BeeCloud.ApiTests.Models.Requests;
using BeeCloud.ApiTests.TestData;
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
        var request =
            TestDataFactory.CreateNodeRequest();

        // Act
        var response =
            await _nodesClient.CreateAsync(request);

        // Assert
        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.Accepted));

        Assert.That(
            response.Data,
            Is.Not.Null);

        TestContext.WriteLine(
            $"Create node response: {response.Content}");

        Assert.That(
            response.Data!.Status,
            Is.EqualTo(nameof(NodeStatus.Provisioning)));
    }

    [Test]
    public async Task CreateNode_WithInvalidData_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new CreateNodeRequestModel
        {
            Name = "",
            GpuModel = "",
            GpuCount = 0
        };

        // Act
        var response =
            await _nodesClient.CreateAsync(request);

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
    public async Task CreateNode_WithGpuCountGreaterThan16_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new CreateNodeRequestModel
        {
            Name = $"api-test-node-{Guid.NewGuid():N}",
            GpuModel = "NVIDIA H100",
            GpuCount = 17
        };

        // Act
        var response =
            await _nodesClient.CreateAsync(request);

        // Assert
        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task CreateNode_WithNameLongerThan100Characters_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new CreateNodeRequestModel
        {
            Name = new string('a', 101),
            GpuModel = "NVIDIA H100",
            GpuCount = 1
        };

        // Act
        var response =
            await _nodesClient.CreateAsync(request);

        // Assert
        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.BadRequest));
    }
    [Test]
    public async Task CreateNode_WithGpuModelLongerThan100Characters_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new CreateNodeRequestModel
        {
            Name = $"api-test-node-{Guid.NewGuid():N}",
            GpuModel = new string('a', 101),
            GpuCount = 1
        };

        // Act
        var response =
            await _nodesClient.CreateAsync(request);

        // Assert
        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.BadRequest));
    }
    [Test]
    public async Task GetAllNodes_ShouldReturnOk()
    {
        // Act
        var response =
            await _nodesClient.GetAllAsync();

        // Assert
        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.OK));

        Assert.That(
            response.Data,
            Is.Not.Null);

        Assert.That(
            response.Data,
            Is.InstanceOf<List<ComputeNodeResponseModel>>());
    }
    [Test]
    public async Task GetNodeById_WhenNodeExists_ShouldReturnNode()
    {
        // Arrange
        var nodeId =
            await CreateNodeAsync();

        await _nodesClient.WaitForAvailableAsync(nodeId);

        // Act
        var response =
            await _nodesClient.GetByIdAsync(nodeId);

        // Assert
        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.OK));

        Assert.That(
            response.Data,
            Is.Not.Null);

        TestContext.WriteLine(
            $"Get node response: {response.Content}");

        Assert.That(
            response.Data!.Id,
            Is.EqualTo(nodeId));

        Assert.That(
            response.Data.Status,
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
    public async Task DeleteNode_WhenNodeExists_ShouldReturnNoContent()
    {
        // Arrange
        var nodeId =
            await CreateNodeAsync();

        await _nodesClient.WaitForAvailableAsync(nodeId);

        // Act
        var response =
            await _nodesClient.DeleteAsync(nodeId);

        // Assert
        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.NoContent));

        Assert.That(
            response.Content,
            Is.Null.Or.Empty);

        TestContext.WriteLine(
            $"Delete node response: {response.Content}");

        var getResponse =
            await _nodesClient.GetByIdAsync(nodeId);

        Assert.That(
            getResponse.StatusCode,
            Is.EqualTo(HttpStatusCode.NotFound));
    }
    [Test]
    public async Task DeleteNode_WhenNodeDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        var nodeId = Guid.NewGuid();

        // Act
        var response =
            await _nodesClient.DeleteAsync(nodeId);

        // Assert
        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.NotFound));

        TestContext.WriteLine(
            $"Delete non-existing node response: {response.Content}");
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
            response.Data,
            Is.Not.Null);

        TestContext.WriteLine(
            $"Start node response: {response.Content}");

        Assert.That(
            response.Data!.Status,
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
            response.Data,
            Is.Not.Null);

        TestContext.WriteLine(
            $"Stop node response: {response.Content}");

        Assert.That(
            response.Data!.Status,
            Is.EqualTo(nameof(NodeStatus.Stopping)));
        
        var stoppingNode =
            await _nodesClient.WaitForStoppingAsync(nodeId);

        Assert.That(
            stoppingNode.Status,
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
    public async Task StopNode_WhenNodeIsStopped_ShouldReturnConflict()
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

        var stopResponse =
            await _nodesClient.StopAsync(nodeId);

        Assert.That(
            stopResponse.StatusCode,
            Is.EqualTo(HttpStatusCode.OK));

        await _nodesClient.WaitForStoppingAsync(nodeId);

        // Act
        var response =
            await _nodesClient.StopAsync(nodeId);

        // Assert
        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.Conflict));

        TestContext.WriteLine(
            $"Stop stopped node response: {response.Content}");
    }
    [Test]
    public async Task RestartNode_WhenNodeIsRunning_ShouldReturnStoppingAndEventuallyRunning()
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

        await _nodesClient.WaitForRunningAsync(nodeId);

        // Act
        var response =
            await _nodesClient.RestartAsync(nodeId);

        // Assert
        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.OK));

        Assert.That(
            response.Data,
            Is.Not.Null);

        TestContext.WriteLine(
            $"Restart node response: {response.Content}");

        Assert.That(
            response.Data!.Status,
            Is.EqualTo(nameof(NodeStatus.Stopping)));
        
        var stoppingNode =
            await _nodesClient.WaitForStoppingAsync(nodeId);

        Assert.That(
            stoppingNode.Status,
            Is.EqualTo(nameof(NodeStatus.Stopping)));

        var finalNode =
            await _nodesClient.WaitForRunningAsync(nodeId);

        Assert.That(
            finalNode.Status,
            Is.EqualTo(nameof(NodeStatus.Running)));
    }
    [Test]
    public async Task RestartNode_WhenNodeIsNotRunning_ShouldReturnConflict()
    {
        // Arrange
        var nodeId =
            await CreateNodeAsync();

        await _nodesClient.WaitForAvailableAsync(nodeId);

        // Act
        var response =
            await _nodesClient.RestartAsync(nodeId);

        // Assert
        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.Conflict));

        TestContext.WriteLine(
            $"Restart non-running node response: {response.Content}");
    }
    [Test]
    public async Task RestartNode_WhenNodeIsStopped_ShouldReturnConflict()
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

        await _nodesClient.WaitForRunningAsync(nodeId);

        var stopResponse =
            await _nodesClient.StopAsync(nodeId);

        Assert.That(
            stopResponse.StatusCode,
            Is.EqualTo(HttpStatusCode.OK));

        await _nodesClient.WaitForStoppingAsync(nodeId);

        // Act
        var response =
            await _nodesClient.RestartAsync(nodeId);

        // Assert
        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.Conflict));

        TestContext.WriteLine(
            $"Restart stopped node response: {response.Content}");
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
        var nodeId =
            await CreateNodeAsync();

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

        Assert.That(
            response.Data,
            Is.Not.Null);

        TestContext.WriteLine(
            $"Simulation response: {response.Content}");

        Assert.That(
            response.Data!.ActiveFault,
            Is.EqualTo(fault.ToString()));
    }

    [Test]
    public async Task SimulateFault_WhenNoneIsRequested_ShouldReturnBadRequest()
    {
        // Arrange
        var nodeId =
            await CreateNodeAsync();

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
    
    [Test]
    public async Task ClearFault_WhenNodeHasActiveFault_ShouldReturnOkAndClearFault()
    {
        var nodeId =
            await CreateNodeAsync();

        await _nodesClient.WaitForAvailableAsync(nodeId);

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
            Is.EqualTo(NodeFault.GpuFailure.ToString()));

        var clearResponse =
            await _nodesClient.ClearFaultAsync(nodeId);

        Assert.That(
            clearResponse.StatusCode,
            Is.EqualTo(HttpStatusCode.OK));

        Assert.That(
            clearResponse.Data,
            Is.Not.Null);

        TestContext.WriteLine(
            $"Clear fault response: {clearResponse.Content}");

        Assert.That(
            clearResponse.Data!.ActiveFault,
            Is.EqualTo(nameof(NodeFault.None)));
    }
    
    [Test]
    public async Task ClearFault_WhenNodeDoesNotExist_ShouldReturnNotFound()
    {
        var nodeId = Guid.NewGuid();

        var response =
            await _nodesClient.ClearFaultAsync(nodeId);

        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.NotFound));

        TestContext.WriteLine(
            $"Clear fault for non-existing node: {response.Content}");
    }
    
    [Test]
    public async Task NodeLifecycle_CreateStartAndStop_ShouldFollowExpectedStates()
    {
        var request =
            TestDataFactory.CreateNodeRequest();

        var createResponse =
            await _nodesClient.CreateAsync(request);

        Assert.That(
            createResponse.StatusCode,
            Is.EqualTo(HttpStatusCode.Accepted));

        Assert.That(
            createResponse.Data,
            Is.Not.Null);

        var nodeId =
            createResponse.Data!.Id;

        Assert.That(
            nodeId,
            Is.Not.EqualTo(Guid.Empty));

        Assert.That(
            createResponse.Data.Status,
            Is.EqualTo(nameof(NodeStatus.Provisioning)));

        var availableNode =
            await _nodesClient.WaitForAvailableAsync(nodeId);

        Assert.That(
            availableNode.Status,
            Is.EqualTo(nameof(NodeStatus.Available)));

        var startResponse =
            await _nodesClient.StartAsync(nodeId);

        Assert.That(
            startResponse.StatusCode,
            Is.EqualTo(HttpStatusCode.OK));

        Assert.That(
            startResponse.Data,
            Is.Not.Null);

        Assert.That(
            startResponse.Data!.Status,
            Is.EqualTo(nameof(NodeStatus.Running)));

        var stopResponse =
            await _nodesClient.StopAsync(nodeId);

        Assert.That(
            stopResponse.StatusCode,
            Is.EqualTo(HttpStatusCode.OK));

        Assert.That(
            stopResponse.Data,
            Is.Not.Null);

        Assert.That(
            stopResponse.Data!.Status,
            Is.EqualTo(nameof(NodeStatus.Stopping)));
    }

    private async Task<Guid> CreateNodeAsync()
    {
        var request =
            TestDataFactory.CreateNodeRequest();

        var response =
            await _nodesClient.CreateAsync(request);

        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.Accepted));

        Assert.That(
            response.Data,
            Is.Not.Null);

        var nodeId = response.Data!.Id;

        Assert.That(
            nodeId,
            Is.Not.EqualTo(Guid.Empty));

        return nodeId;
    }
}