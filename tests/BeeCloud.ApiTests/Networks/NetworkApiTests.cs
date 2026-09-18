using System.Net;
using System.Text.Json;
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
        var networkName =
            $"api-test-network-{Guid.NewGuid():N}";

        var response = await _networksClient.CreateAsync(
            networkName,
            "API test network");

        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.Created));

        Assert.That(response.Content, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public async Task CreateNetwork_WithEmptyName_ShouldReturnBadRequest()
    {
        var response = await _networksClient.CreateAsync(
            string.Empty,
            "API test network");

        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task CreateNetwork_WithDuplicateName_ShouldReturnConflict()
    {
        var networkName =
            $"api-test-network-{Guid.NewGuid():N}";

        var firstResponse = await _networksClient.CreateAsync(
            networkName,
            "API test network");

        Assert.That(
            firstResponse.StatusCode,
            Is.EqualTo(HttpStatusCode.Created));

        var secondResponse = await _networksClient.CreateAsync(
            networkName,
            "API test network");

        Assert.That(
            secondResponse.StatusCode,
            Is.EqualTo(HttpStatusCode.Conflict));
    }

    [Test]
    public async Task GetAllNetworks_ShouldReturnOk()
    {
        var response = await _networksClient.GetAllAsync();

        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task GetNetworkById_WhenNetworkDoesNotExist_ShouldReturnNotFound()
    {
        var networkId = Guid.NewGuid();

        var response = await _networksClient.GetByIdAsync(
            networkId);

        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task GetNetworkById_WhenNetworkExists_ShouldReturnNetwork()
    {
        var networkName =
            $"api-test-network-{Guid.NewGuid():N}";

        var createResponse = await _networksClient.CreateAsync(
            networkName,
            "API test network");

        Assert.That(
            createResponse.StatusCode,
            Is.EqualTo(HttpStatusCode.Created));

        Assert.That(
            createResponse.Content,
            Is.Not.Null.And.Not.Empty);

        var createdJson =
            JsonSerializer.Deserialize<JsonElement>(
                createResponse.Content!);

        var createdNetworkId =
            createdJson
                .GetProperty("id")
                .GetGuid();

        Assert.That(
            createdNetworkId,
            Is.Not.EqualTo(Guid.Empty));

        var response = await _networksClient.GetByIdAsync(
            createdNetworkId);

        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.OK));

        Assert.That(
            response.Content,
            Is.Not.Null.And.Not.Empty);

        var networkJson =
            JsonSerializer.Deserialize<JsonElement>(
                response.Content!);

        var returnedNetworkId =
            networkJson
                .GetProperty("id")
                .GetGuid();

        var returnedName =
            networkJson
                .GetProperty("name")
                .GetString();

        var returnedDescription =
            networkJson
                .GetProperty("description")
                .GetString();

        Assert.That(
            returnedNetworkId,
            Is.EqualTo(createdNetworkId));

        Assert.That(
            returnedName,
            Is.EqualTo(networkName));

        Assert.That(
            returnedDescription,
            Is.EqualTo("API test network"));
    }
}