using BeeCloud.ApiTests.Clients;
using BeeCloud.ApiTests.Models.Requests;
using NUnit.Framework;
using System.Net;

namespace BeeCloud.ApiTests.TestData;

public class ApiTestDataHelper
{
    private readonly NodesClient _nodesClient;

    public ApiTestDataHelper(NodesClient nodesClient)
    {
        _nodesClient = nodesClient;
    }

    public async Task<Guid> CreateNodeAsync()
    {
        var request = TestDataFactory.CreateNodeRequest();

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

    public async Task<Guid> CreateAvailableNodeAsync()
    {
        var nodeId = await CreateNodeAsync();

        await _nodesClient.WaitForAvailableAsync(nodeId);

        return nodeId;
    }
}