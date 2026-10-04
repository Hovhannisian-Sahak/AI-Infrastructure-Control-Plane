using System.Net;
using BeeCloud.ApiTests.Clients;
using BeeCloud.ApiTests.Models;
using NUnit.Framework;

namespace BeeCloud.ApiTests;

[TestFixture]
public class NetworkApiTests
{
    private NetworksClient _networksClient = null!;

    [SetUp]
    public void SetUp()
    {
        _networksClient = new NetworksClient(
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
}