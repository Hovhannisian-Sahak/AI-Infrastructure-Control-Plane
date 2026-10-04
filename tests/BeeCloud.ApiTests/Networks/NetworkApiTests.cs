using System.Net;
using BeeCloud.ApiTests.Clients;
using BeeCloud.ApiTests.Models;
using BeeCloud.ApiTests.Models.Requests;
using NUnit.Framework;

namespace BeeCloud.ApiTests;

[TestFixture]
public class NetworkApiTests
{
    private NetworksClient _networksClient = null!;
    private NodesClient _nodesClient = null!;

    [SetUp]
    public void SetUp()
    {
        _networksClient = new NetworksClient(
            TestConfiguration.BaseUrl);

        _nodesClient = new NodesClient(
            TestConfiguration.BaseUrl);
    }

    [Test]
    public async Task CreateNetwork_WithValidData_ShouldReturnCreated()
    {
        // Arrange
        var networkName =
            $"api-test-network-{Guid.NewGuid():N}";

        // Act
        var response = await _networksClient.CreateAsync(
            networkName,
            "API test network");

        // Assert
        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.Created));

        Assert.That(
            response.Data,
            Is.Not.Null);

        Assert.That(
            response.Data!.Name,
            Is.EqualTo(networkName));

        Assert.That(
            response.Data.Description,
            Is.EqualTo("API test network"));

        Assert.That(
            response.Data.Id,
            Is.Not.EqualTo(Guid.Empty));
    }

    [Test]
    public async Task CreateNetwork_WithEmptyName_ShouldReturnBadRequest()
    {
        // Act
        var response = await _networksClient.CreateAsync(
            string.Empty,
            "API test network");

        // Assert
        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task CreateNetwork_WithDuplicateName_ShouldReturnConflict()
    {
        // Arrange
        var networkName =
            $"api-test-network-{Guid.NewGuid():N}";

        var firstResponse = await _networksClient.CreateAsync(
            networkName,
            "API test network");

        Assert.That(
            firstResponse.StatusCode,
            Is.EqualTo(HttpStatusCode.Created));

        // Act
        var secondResponse = await _networksClient.CreateAsync(
            networkName,
            "API test network");

        // Assert
        Assert.That(
            secondResponse.StatusCode,
            Is.EqualTo(HttpStatusCode.Conflict));
    }
    
    [Test]
    public async Task CreateNetwork_WithNameLongerThan100Characters_ShouldReturnBadRequest()
    {
        // Arrange
        var networkName = new string('a', 101);

        // Act
        var response = await _networksClient.CreateAsync(
            networkName,
            "API test network");

        // Assert
        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task CreateNetwork_WithDescriptionLongerThan500Characters_ShouldReturnBadRequest()
    {
        // Arrange
        var networkName =
            $"api-test-network-{Guid.NewGuid():N}";

        var description = new string('a', 501);

        // Act
        var response = await _networksClient.CreateAsync(
            networkName,
            description);

        // Assert
        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task GetAllNetworks_ShouldReturnOk()
    {
        // Act
        var response = await _networksClient.GetAllAsync();

        // Assert
        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.OK));

        Assert.That(
            response.Data,
            Is.Not.Null);

        Assert.That(
            response.Data,
            Is.InstanceOf<List<NetworkResponseModel>>());
    }

    [Test]
    public async Task GetNetworkById_WhenNetworkDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        var networkId = Guid.NewGuid();

        // Act
        var response = await _networksClient.GetByIdAsync(
            networkId);

        // Assert
        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task GetNetworkById_WhenNetworkExists_ShouldReturnNetwork()
    {
        // Arrange
        var networkName =
            $"api-test-network-{Guid.NewGuid():N}";

        var createResponse = await _networksClient.CreateAsync(
            networkName,
            "API test network");

        Assert.That(
            createResponse.StatusCode,
            Is.EqualTo(HttpStatusCode.Created));

        Assert.That(
            createResponse.Data,
            Is.Not.Null);

        var createdNetworkId =
            createResponse.Data!.Id;

        Assert.That(
            createdNetworkId,
            Is.Not.EqualTo(Guid.Empty));

        // Act
        var response = await _networksClient.GetByIdAsync(
            createdNetworkId);

        // Assert
        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.OK));

        Assert.That(
            response.Data,
            Is.Not.Null);

        Assert.That(
            response.Data!.Id,
            Is.EqualTo(createdNetworkId));

        Assert.That(
            response.Data.Name,
            Is.EqualTo(networkName));

        Assert.That(
            response.Data.Description,
            Is.EqualTo("API test network"));
    }
    
    [Test]
    public async Task DeleteNetwork_WithNoAttachments_ShouldReturnNoContent()
    {
        // Arrange
        var networkName =
            $"api-test-network-{Guid.NewGuid():N}";

        var createResponse = await _networksClient.CreateAsync(
            networkName,
            "Network to delete");

        Assert.That(
            createResponse.StatusCode,
            Is.EqualTo(HttpStatusCode.Created));

        Assert.That(
            createResponse.Data,
            Is.Not.Null);

        var networkId = createResponse.Data!.Id;

        // Act
        var deleteResponse = await _networksClient.DeleteAsync(
            networkId);

        // Assert
        Assert.That(
            deleteResponse.StatusCode,
            Is.EqualTo(HttpStatusCode.NoContent));
    }
    
    [Test]
    public async Task DeleteNetwork_WithAttachedNode_ShouldReturnConflict()
    {
        var networkName =
            $"api-test-network-{Guid.NewGuid():N}";

        var createNetworkResponse =
            await _networksClient.CreateAsync(
                networkName,
                "Network with attachment");

        Assert.That(
            createNetworkResponse.StatusCode,
            Is.EqualTo(HttpStatusCode.Created));

        Assert.That(
            createNetworkResponse.Data,
            Is.Not.Null);

        var networkId = createNetworkResponse.Data!.Id;

        var createNodeResponse = await _nodesClient.CreateAsync(
            new CreateNodeRequestModel
            {
                Name = $"api-test-node-{Guid.NewGuid():N}",
                GpuModel = "Test GPU",
                GpuCount = 1
            });

        Assert.That(
            createNodeResponse.StatusCode,
            Is.EqualTo(HttpStatusCode.Accepted));

        Assert.That(
            createNodeResponse.Data,
            Is.Not.Null);

        var nodeId = createNodeResponse.Data!.Id;

        try
        {
            await _nodesClient.WaitForAvailableAsync(nodeId);

            var attachResponse =
                await _networksClient.AttachToNodeAsync(
                    nodeId,
                    networkId);

            Assert.That(
                attachResponse.StatusCode,
                Is.EqualTo(HttpStatusCode.Created));

            var deleteResponse =
                await _networksClient.DeleteAsync(networkId);

            Assert.That(
                deleteResponse.StatusCode,
                Is.EqualTo(HttpStatusCode.Conflict));
        }
        finally
        {
            var detachResponse =
                await _networksClient.DetachFromNodeAsync(
                    nodeId,
                    networkId);

            Assert.That(
                detachResponse.StatusCode,
                Is.EqualTo(HttpStatusCode.NoContent));

            var cleanupDeleteResponse =
                await _networksClient.DeleteAsync(networkId);

            Assert.That(
                cleanupDeleteResponse.StatusCode,
                Is.EqualTo(HttpStatusCode.NoContent));

            var getDeletedNetworkResponse =
                await _networksClient.GetByIdAsync(networkId);

            Assert.That(
                getDeletedNetworkResponse.StatusCode,
                Is.EqualTo(HttpStatusCode.NotFound));
        }
    }
    
    [Test]
    public async Task DeleteNetwork_WhenNetworkDoesNotExist_ShouldReturnNotFound()
    {
        var networkId = Guid.NewGuid();

        var response =
            await _networksClient.DeleteAsync(networkId);

        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.NotFound));
    }
    
    [Test]
    public async Task DeleteNetwork_WhenNetworkIsAlreadyDeleted_ShouldReturnNotFound()
    {
        var networkName =
            $"api-test-network-{Guid.NewGuid():N}";

        var createResponse =
            await _networksClient.CreateAsync(
                networkName,
                "Network to delete twice");

        Assert.That(
            createResponse.StatusCode,
            Is.EqualTo(HttpStatusCode.Created));

        Assert.That(
            createResponse.Data,
            Is.Not.Null);

        var networkId = createResponse.Data!.Id;

        var firstDeleteResponse =
            await _networksClient.DeleteAsync(networkId);

        Assert.That(
            firstDeleteResponse.StatusCode,
            Is.EqualTo(HttpStatusCode.NoContent));

        var secondDeleteResponse =
            await _networksClient.DeleteAsync(networkId);

        Assert.That(
            secondDeleteResponse.StatusCode,
            Is.EqualTo(HttpStatusCode.NotFound));
    }
    
    [Test]
    public async Task CreateNetwork_WithNameOfDeletedNetwork_ShouldReturnCreated()
    {
        var networkName =
            $"api-test-network-{Guid.NewGuid():N}";

        // Create the original network.
        var firstCreateResponse =
            await _networksClient.CreateAsync(
                networkName,
                "Original network");

        Assert.That(
            firstCreateResponse.StatusCode,
            Is.EqualTo(HttpStatusCode.Created));

        Assert.That(
            firstCreateResponse.Data,
            Is.Not.Null);

        var firstNetworkId =
            firstCreateResponse.Data!.Id;

        // Soft-delete the original network.
        var deleteResponse =
            await _networksClient.DeleteAsync(firstNetworkId);

        Assert.That(
            deleteResponse.StatusCode,
            Is.EqualTo(HttpStatusCode.NoContent));

        // Reuse the same name.
        var secondCreateResponse =
            await _networksClient.CreateAsync(
                networkName,
                "Replacement network");

        Assert.That(
            secondCreateResponse.StatusCode,
            Is.EqualTo(HttpStatusCode.Created));

        Assert.That(
            secondCreateResponse.Data,
            Is.Not.Null);

        Assert.That(
            secondCreateResponse.Data!.Name,
            Is.EqualTo(networkName));

        Assert.That(
            secondCreateResponse.Data.Id,
            Is.Not.EqualTo(firstNetworkId));
    }
}