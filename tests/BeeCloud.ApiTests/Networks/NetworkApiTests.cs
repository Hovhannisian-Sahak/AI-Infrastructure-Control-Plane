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

    // [Test]
    // public async Task GetNetworkById_WhenNetworkExists_ShouldReturnNetwork()
    // {
    //     var networkName =
    //         $"api-test-network-{Guid.NewGuid():N}";
    //
    //     var createResponse = await _networksClient.CreateAsync(
    //         networkName,
    //         "API test network");
    //
    //     Assert.That(
    //         createResponse.StatusCode,
    //         Is.EqualTo(HttpStatusCode.Created));
    //
    //     var createdNetwork =
    //         JsonSerializer.Deserialize<NetworkResponseModel>(
    //             createResponse.Content!);
    //
    //     Assert.That(createdNetwork, Is.Not.Null);
    //
    //     var response = await _networksClient.GetByIdAsync(
    //         createdNetwork!.Id);
    //
    //     Assert.That(
    //         response.StatusCode,
    //         Is.EqualTo(HttpStatusCode.OK));
    //
    //     var network =
    //         JsonSerializer.Deserialize<NetworkResponseModel>(
    //             response.Content!);
    //
    //     Assert.That(network, Is.Not.Null);
    //
    //     Assert.That(
    //         network!.Id,
    //         Is.EqualTo(createdNetwork.Id));
    //
    //     Assert.That(
    //         network.Name,
    //         Is.EqualTo(networkName));
    //
    //     Assert.That(
    //         network.Description,
    //         Is.EqualTo("API test network"));
    // }
}