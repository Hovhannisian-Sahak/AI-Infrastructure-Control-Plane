using System.Net;
using BeeCloud.ApiTests;
using BeeCloud.ApiTests.Clients;
using BeeCloud.ApiTests.Models.Requests;
using BeeCloud.ApiTests.TestData;
using NUnit.Framework;

namespace BeeCloud.ApiTests.Networks;

[TestFixture]
public class NetworkAttachmentApiTests
{
    private NodesClient _nodesClient = null!;
    private NetworksClient _networksClient = null!;
    private ApiTestDataHelper _testData = null!;
    [SetUp]
    public void SetUp()
    {
        var baseUrl = TestConfiguration.BaseUrl;

        _nodesClient = new NodesClient(baseUrl);
        _networksClient = new NetworksClient(baseUrl);
        _testData = new ApiTestDataHelper(_nodesClient);
    }

    [Test]
    public async Task AttachNetworkToNode_WhenValid_ShouldReturnCreated()
    {
        // Arrange
        var nodeId = await _testData.CreateAvailableNodeAsync();
        var networkId = await CreateNetworkAsync();

        // Act
        var response = await _networksClient.AttachToNodeAsync(
            nodeId,
            networkId);

        // Assert
        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.Created));

        Assert.That(
            response.Data,
            Is.Not.Null);

        Assert.That(
            response.Data!.Id,
            Is.Not.EqualTo(Guid.Empty));

        Assert.That(
            response.Data.ComputeNodeId,
            Is.EqualTo(nodeId));

        Assert.That(
            response.Data.NetworkId,
            Is.EqualTo(networkId));

        Assert.That(
            response.Data.AttachedAt,
            Is.Not.EqualTo(default(DateTime)));

        TestContext.WriteLine(
            $"Attachment Id: {response.Data.Id}");
    }

    [Test]
    public async Task AttachNetworkToNode_WhenAlreadyAttached_ShouldReturnConflict()
    {
        // Arrange
        var nodeId = await _testData.CreateAvailableNodeAsync();
        var networkId = await CreateNetworkAsync();

        var firstResponse = await _networksClient.AttachToNodeAsync(
            nodeId,
            networkId);

        Assert.That(
            firstResponse.StatusCode,
            Is.EqualTo(HttpStatusCode.Created));

        // Act
        var secondResponse = await _networksClient.AttachToNodeAsync(
            nodeId,
            networkId);

        // Assert
        Assert.That(
            secondResponse.StatusCode,
            Is.EqualTo(HttpStatusCode.Conflict));

        TestContext.WriteLine(
            $"Duplicate attachment response: {secondResponse.Content}");
    }

    [Test]
    public async Task GetNodeNetworks_AfterAttachment_ShouldReturnAttachment()
    {
        // Arrange
        var nodeId = await _testData.CreateAvailableNodeAsync();
        var networkId = await CreateNetworkAsync();

        var attachResponse = await _networksClient.AttachToNodeAsync(
            nodeId,
            networkId);

        Assert.That(
            attachResponse.StatusCode,
            Is.EqualTo(HttpStatusCode.Created));

        // Act
        var response = await _networksClient.GetByNodeIdAsync(
            nodeId);

        // Assert
        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.OK));

        Assert.That(
            response.Data,
            Is.Not.Null);

        Assert.That(
            response.Data!,
            Is.Not.Empty);

        var attachment = response.Data!
            .SingleOrDefault(x =>
                x.ComputeNodeId == nodeId &&
                x.NetworkId == networkId);

        Assert.That(
            attachment,
            Is.Not.Null,
            $"Expected attachment was not found. " +
            $"NodeId: {nodeId}, " +
            $"NetworkId: {networkId}");

        Assert.That(
            attachment!.Id,
            Is.Not.EqualTo(Guid.Empty));

        Assert.That(
            attachment.AttachedAt,
            Is.Not.EqualTo(default(DateTime)));

        TestContext.WriteLine(
            $"Attachment Id: {attachment.Id}");

        TestContext.WriteLine(
            $"Attachment NodeId: {attachment.ComputeNodeId}");

        TestContext.WriteLine(
            $"Attachment NetworkId: {attachment.NetworkId}");
    }

    [Test]
    public async Task GetNetworkNodes_AfterAttachment_ShouldReturnAttachment()
    {
        // Arrange
        var nodeId = await _testData.CreateAvailableNodeAsync();
        var networkId = await CreateNetworkAsync();

        var attachResponse = await _networksClient.AttachToNodeAsync(
            nodeId,
            networkId);

        Assert.That(
            attachResponse.StatusCode,
            Is.EqualTo(HttpStatusCode.Created));

        // Act
        var response = await _networksClient.GetByNetworkIdAsync(
            networkId);

        // Assert
        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.OK));

        Assert.That(
            response.Data,
            Is.Not.Null);

        Assert.That(
            response.Data!,
            Is.Not.Empty);

        var attachment = response.Data!
            .SingleOrDefault(x =>
                x.ComputeNodeId == nodeId &&
                x.NetworkId == networkId);

        Assert.That(
            attachment,
            Is.Not.Null,
            $"Expected attachment was not found. " +
            $"NodeId: {nodeId}, " +
            $"NetworkId: {networkId}");

        Assert.That(
            attachment!.Id,
            Is.Not.EqualTo(Guid.Empty));

        Assert.That(
            attachment.AttachedAt,
            Is.Not.EqualTo(default(DateTime)));

        TestContext.WriteLine(
            $"Attachment Id: {attachment.Id}");

        TestContext.WriteLine(
            $"Attachment NodeId: {attachment.ComputeNodeId}");

        TestContext.WriteLine(
            $"Attachment NetworkId: {attachment.NetworkId}");
    }

    [Test]
    public async Task DetachNetworkFromNode_WhenAttached_ShouldReturnNoContent()
    {
        // Arrange
        var nodeId = await _testData.CreateAvailableNodeAsync();
        var networkId = await CreateNetworkAsync();

        var attachResponse = await _networksClient.AttachToNodeAsync(
            nodeId,
            networkId);

        Assert.That(
            attachResponse.StatusCode,
            Is.EqualTo(HttpStatusCode.Created));

        // Act
        var detachResponse = await _networksClient.DetachFromNodeAsync(
            nodeId,
            networkId);

        // Assert
        Assert.That(
            detachResponse.StatusCode,
            Is.EqualTo(HttpStatusCode.NoContent));
    }

    [Test]
    public async Task DetachNetworkFromNode_WhenNotAttached_ShouldReturnNotFound()
    {
        // Arrange
        var nodeId = await _testData.CreateAvailableNodeAsync();
        var networkId = await CreateNetworkAsync();

        // Act
        var response = await _networksClient.DetachFromNodeAsync(
            nodeId,
            networkId);

        // Assert
        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.NotFound));

        TestContext.WriteLine(
            $"Detach missing attachment response: {response.Content}");
    }

    [Test]
    public async Task AttachNetworkToNode_WhenNetworkIsInactive_ShouldReturnConflict()
    {
        // Arrange
        var nodeId = await _testData.CreateAvailableNodeAsync();
        var networkId = await CreateNetworkAsync();

        var deactivateResponse =
            await _networksClient.DeactivateAsync(networkId);

        Assert.That(
            deactivateResponse.StatusCode,
            Is.EqualTo(HttpStatusCode.NoContent));

        // Act
        var response =
            await _networksClient.AttachToNodeAsync(
                nodeId,
                networkId);

        // Assert
        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.Conflict));

        TestContext.WriteLine(
            $"Attach to inactive network response: {response.Content}");
    }
    [Test]
    public async Task AttachNetworkToNode_WhenNodeDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        var networkId = await CreateNetworkAsync();

        // Act
        var response =
            await _networksClient.AttachToNodeAsync(
                nodeId,
                networkId);

        // Assert
        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.NotFound));

        TestContext.WriteLine(
            $"Attach to non-existing node response: {response.Content}");
    }
    [Test]
    public async Task AttachNetworkToNode_WhenNetworkDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        var nodeId = await _testData.CreateAvailableNodeAsync();
        var networkId = Guid.NewGuid();

        // Act
        var response =
            await _networksClient.AttachToNodeAsync(
                nodeId,
                networkId);

        // Assert
        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.NotFound));

        TestContext.WriteLine(
            $"Attach to non-existing network response: {response.Content}");
    }
    [Test]
    public async Task AttachNetworkToNode_WhenNetworkReachesMaxAttachments_ShouldReturnConflict()
    {
        // Arrange
        var networkId = await CreateFullNetworkAsync();

        var fifthNodeId = await _testData.CreateAvailableNodeAsync();

        // Act
        var response =
            await _networksClient.AttachToNodeAsync(
                fifthNodeId,
                networkId);

        // Assert
        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.Conflict));

        TestContext.WriteLine(
            $"Attach beyond network capacity response: {response.Content}");
    }
    
    private async Task<Guid> CreateNetworkAsync()
    {
        var networkName =
            $"api-test-network-{Guid.NewGuid():N}";

        var response =
            await _networksClient.CreateAsync(
                networkName,
                "API test network");

        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.Created),
            $"Network creation failed. Response: {response.Content}");

        Assert.That(
            response.Data,
            Is.Not.Null);

        var networkId = response.Data!.Id;

        Assert.That(
            networkId,
            Is.Not.EqualTo(Guid.Empty));

        TestContext.WriteLine(
            $"Created network: {networkId}");

        return networkId;
    }
    
    private async Task<Guid> CreateFullNetworkAsync()
    {
        var networkId = await CreateNetworkAsync();

        for (var i = 0; i < 4; i++)
        {
            var nodeId = await _testData.CreateAvailableNodeAsync();

            var response =
                await _networksClient.AttachToNodeAsync(
                    nodeId,
                    networkId);

            Assert.That(
                response.StatusCode,
                Is.EqualTo(HttpStatusCode.Created),
                $"Attachment {i + 1} failed. " +
                $"Response: {response.Content}");
        }

        return networkId;
    }
}