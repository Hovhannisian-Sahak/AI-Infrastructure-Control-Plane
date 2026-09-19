using System.Net;
using System.Text.Json;
using BeeCloud.ApiTests.Clients;
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
        var nodeId = await CreateNodeAsync();
        var networkId = await CreateNetworkAsync();

        var response = await _networksClient.AttachToNodeAsync(
            nodeId,
            networkId);

        Assert.That(
            (int)response.StatusCode,
            Is.EqualTo(201));

        Assert.That(
            response.Content,
            Is.Not.Null.And.Not.Empty);

        TestContext.WriteLine(
            $"Attachment response: {response.Content}");
    }

    [Test]
    public async Task AttachNetworkToNode_WhenAlreadyAttached_ShouldReturnConflict()
    {
        var nodeId = await CreateNodeAsync();
        var networkId = await CreateNetworkAsync();

        var firstResponse = await _networksClient.AttachToNodeAsync(
            nodeId,
            networkId);

        Assert.That(
            (int)firstResponse.StatusCode,
            Is.EqualTo(201));

        var secondResponse = await _networksClient.AttachToNodeAsync(
            nodeId,
            networkId);

        Assert.That(
            (int)secondResponse.StatusCode,
            Is.EqualTo(409));

        TestContext.WriteLine(
            $"Duplicate attachment response: {secondResponse.Content}");
    }

    [Test]
    public async Task GetNodeNetworks_AfterAttachment_ShouldReturnAttachment()
    {
        var nodeId = await CreateNodeAsync();
        var networkId = await CreateNetworkAsync();

        var attachResponse = await _networksClient.AttachToNodeAsync(
            nodeId,
            networkId);

        Assert.That(
            (int)attachResponse.StatusCode,
            Is.EqualTo(201));

        var response = await _networksClient.GetByNodeIdAsync(
            nodeId);

        Assert.That(
            (int)response.StatusCode,
            Is.EqualTo(200));

        Assert.That(
            response.Content,
            Is.Not.Null.And.Not.Empty);

        var json = JsonSerializer.Deserialize<JsonElement>(
            response.Content!);

        Assert.That(
            json.ValueKind,
            Is.EqualTo(JsonValueKind.Array));

        Assert.That(
            json.GetArrayLength(),
            Is.GreaterThan(0));

        var found = false;

        foreach (var item in json.EnumerateArray())
        {
            var returnedNodeId = item
                .GetProperty("computeNodeId")
                .GetGuid();

            var returnedNetworkId = item
                .GetProperty("networkId")
                .GetGuid();

            TestContext.WriteLine(
                $"Attachment Id: {item.GetProperty("id").GetGuid()}");

            TestContext.WriteLine(
                $"Attachment NodeId: {returnedNodeId}");

            TestContext.WriteLine(
                $"Attachment NetworkId: {returnedNetworkId}");

            if (returnedNodeId == nodeId &&
                returnedNetworkId == networkId)
            {
                found = true;

                var attachmentId = item
                    .GetProperty("id")
                    .GetGuid();

                Assert.That(
                    attachmentId,
                    Is.Not.EqualTo(Guid.Empty));

                var attachedAt = item
                    .GetProperty("attachedAt")
                    .GetDateTime();

                Assert.That(
                    attachedAt,
                    Is.Not.EqualTo(default(DateTime)));

                break;
            }
        }

        Assert.That(
            found,
            Is.True,
            $"Expected attachment was not found. " +
            $"NodeId: {nodeId}, " +
            $"NetworkId: {networkId}");
    }

    [Test]
    public async Task GetNetworkNodes_AfterAttachment_ShouldReturnAttachment()
    {
        var nodeId = await CreateNodeAsync();
        var networkId = await CreateNetworkAsync();

        var attachResponse = await _networksClient.AttachToNodeAsync(
            nodeId,
            networkId);

        Assert.That(
            (int)attachResponse.StatusCode,
            Is.EqualTo(201));

        var response = await _networksClient.GetByNetworkIdAsync(
            networkId);

        Assert.That(
            (int)response.StatusCode,
            Is.EqualTo(200));

        Assert.That(
            response.Content,
            Is.Not.Null.And.Not.Empty);

        var json = JsonSerializer.Deserialize<JsonElement>(
            response.Content!);

        Assert.That(
            json.ValueKind,
            Is.EqualTo(JsonValueKind.Array));

        Assert.That(
            json.GetArrayLength(),
            Is.GreaterThan(0));

        var found = false;

        foreach (var item in json.EnumerateArray())
        {
            var returnedNodeId = item
                .GetProperty("computeNodeId")
                .GetGuid();

            var returnedNetworkId = item
                .GetProperty("networkId")
                .GetGuid();

            TestContext.WriteLine(
                $"Attachment Id: {item.GetProperty("id").GetGuid()}");

            TestContext.WriteLine(
                $"Attachment NodeId: {returnedNodeId}");

            TestContext.WriteLine(
                $"Attachment NetworkId: {returnedNetworkId}");

            if (returnedNodeId == nodeId &&
                returnedNetworkId == networkId)
            {
                found = true;

                var attachmentId = item
                    .GetProperty("id")
                    .GetGuid();

                Assert.That(
                    attachmentId,
                    Is.Not.EqualTo(Guid.Empty));

                var attachedAt = item
                    .GetProperty("attachedAt")
                    .GetDateTime();

                Assert.That(
                    attachedAt,
                    Is.Not.EqualTo(default(DateTime)));

                break;
            }
        }

        Assert.That(
            found,
            Is.True,
            $"Expected attachment was not found. " +
            $"NodeId: {nodeId}, " +
            $"NetworkId: {networkId}");
    }

    [Test]
    public async Task DetachNetworkFromNode_WhenAttached_ShouldReturnNoContent()
    {
        var nodeId = await CreateNodeAsync();
        var networkId = await CreateNetworkAsync();

        var attachResponse = await _networksClient.AttachToNodeAsync(
            nodeId,
            networkId);

        Assert.That(
            (int)attachResponse.StatusCode,
            Is.EqualTo(201));

        var detachResponse = await _networksClient.DetachFromNodeAsync(
            nodeId,
            networkId);

        Assert.That(
            (int)detachResponse.StatusCode,
            Is.EqualTo(204));
    }

    [Test]
    public async Task DetachNetworkFromNode_WhenNotAttached_ShouldReturnNotFound()
    {
        var nodeId = await CreateNodeAsync();
        var networkId = await CreateNetworkAsync();

        var response = await _networksClient.DetachFromNodeAsync(
            nodeId,
            networkId);

        Assert.That(
            (int)response.StatusCode,
            Is.EqualTo(404));

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
            await _networksClient.DeactivateAsync(
                networkId);

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
        var nodeName = $"api-test-node-{Guid.NewGuid():N}";

        var response = await _nodesClient.CreateAsync(
            nodeName,
            "NVIDIA A100",
            2);

        Assert.That(
            (int)response.StatusCode,
            Is.EqualTo(202),
            $"Node creation failed. Response: {response.Content}");

        Assert.That(
            response.Content,
            Is.Not.Null.And.Not.Empty);

        var json = JsonSerializer.Deserialize<JsonElement>(
            response.Content!);

        var nodeId = json
            .GetProperty("id")
            .GetGuid();

        Assert.That(
            nodeId,
            Is.Not.EqualTo(Guid.Empty));

        TestContext.WriteLine(
            $"Created node: {nodeId}");

        return nodeId;
    }

    private async Task<Guid> CreateNetworkAsync()
    {
        var networkName = $"api-test-network-{Guid.NewGuid():N}";

        var response = await _networksClient.CreateAsync(
            networkName,
            "API test network");

        Assert.That(
            (int)response.StatusCode,
            Is.EqualTo(201),
            $"Network creation failed. Response: {response.Content}");

        Assert.That(
            response.Content,
            Is.Not.Null.And.Not.Empty);

        var json = JsonSerializer.Deserialize<JsonElement>(
            response.Content!);

        var networkId = json
            .GetProperty("id")
            .GetGuid();

        Assert.That(
            networkId,
            Is.Not.EqualTo(Guid.Empty));

        TestContext.WriteLine(
            $"Created network: {networkId}");

        return networkId;
    }
    
    private async Task<Guid> CreateAvailableNodeAsync()
    {
        var nodeId = await CreateNodeAsync();

        await _nodesClient.WaitForAvailableAsync(nodeId);

        return nodeId;
    }
}