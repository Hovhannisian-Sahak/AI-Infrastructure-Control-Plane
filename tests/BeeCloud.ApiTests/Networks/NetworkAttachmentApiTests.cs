using System.Net;
using BeeCloud.ApiTests.Clients;
using BeeCloud.ApiTests.Models.Requests;
using BeeCloud.ApiTests.TestData;
using NUnit.Framework;

namespace BeeCloud.ApiTests;

[TestFixture]
public class NetworkAttachmentApiTests
{
    private NodesClient _nodesClient = null!;
    private NetworksClient _networksClient = null!;

    [SetUp]
    public void SetUp()
    {
        var baseUrl = TestConfiguration.BaseUrl;

        _nodesClient = new NodesClient(baseUrl);
        _networksClient = new NetworksClient(baseUrl);
    }

    [Test]
    public async Task AttachNetworkToNode_WhenValid_ShouldReturnCreated()
    {
        // Arrange
        var nodeId = await CreateAvailableNodeAsync();
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
        var nodeId = await CreateAvailableNodeAsync();
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
        var nodeId = await CreateAvailableNodeAsync();
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
        var nodeId = await CreateAvailableNodeAsync();
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
        var nodeId = await CreateAvailableNodeAsync();
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
        var nodeId = await CreateAvailableNodeAsync();
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
        var nodeId = await CreateAvailableNodeAsync();
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

        await _nodesClient.WaitForAvailableAsync(nodeId);

        return nodeId;
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
}