using System.Text.Json;
using BeeCloud.Domain.Enums;
using NUnit.Framework;
using RestSharp;

namespace BeeCloud.ApiTests.Clients;

public class NodesClient
{
    private readonly RestClient _client;

    public NodesClient(string baseUrl)
    {
        _client = new RestClient(baseUrl);
    }

    public async Task<RestResponse> CreateAsync(
        string name,
        string gpuModel,
        int gpuCount)
    {
        var request = new RestRequest(
            "/api/v1/nodes",
            Method.Post);

        request.AddJsonBody(new
        {
            name,
            gpuModel,
            gpuCount
        });

        return await _client.ExecuteAsync(request);
    }

    public async Task<RestResponse> GetByIdAsync(
        Guid nodeId)
    {
        var request = new RestRequest(
            $"/api/v1/nodes/{nodeId}",
            Method.Get);

        return await _client.ExecuteAsync(request);
    }

    public async Task<RestResponse> StartAsync(
        Guid nodeId)
    {
        var request = new RestRequest(
            $"/api/v1/nodes/{nodeId}/start",
            Method.Post);

        return await _client.ExecuteAsync(request);
    }

    public async Task<RestResponse> StopAsync(
        Guid nodeId)
    {
        var request = new RestRequest(
            $"/api/v1/nodes/{nodeId}/stop",
            Method.Post);

        return await _client.ExecuteAsync(request);
    }

    public async Task WaitForAvailableAsync(
        Guid nodeId,
        TimeSpan? timeout = null)
    {
        var maxWait =
            timeout ?? TimeSpan.FromSeconds(15);

        var deadline =
            DateTime.UtcNow.Add(maxWait);

        string? lastStatus = null;

        while (DateTime.UtcNow < deadline)
        {
            var response =
                await GetByIdAsync(nodeId);

            if (!response.IsSuccessful)
            {
                throw new AssertionException(
                    $"Failed to get node '{nodeId}'. " +
                    $"Status: {response.StatusCode}. " +
                    $"Response: {response.Content}");
            }

            using var document =
                JsonDocument.Parse(response.Content!);

            lastStatus =
                document.RootElement
                    .GetProperty("status")
                    .GetString();

            if (string.Equals(
                    lastStatus,
                    nameof(NodeStatus.Available),
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            await Task.Delay(
                TimeSpan.FromMilliseconds(500));
        }

        throw new AssertionException(
            $"Node '{nodeId}' did not become Available " +
            $"within {maxWait.TotalSeconds} seconds. " +
            $"Last observed status: {lastStatus}.");
    }

    public async Task<RestResponse> SimulateFaultAsync(
        Guid nodeId,
        NodeFault fault)
    {
        var request = new RestRequest(
            $"/api/v1/nodes/{nodeId}/simulate/fault",
            Method.Post);

        request.AddJsonBody(new
        {
            fault = fault.ToString()
        });

        return await _client.ExecuteAsync(request);
    }
}