using System.Text.Json;
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

    public async Task<RestResponse> GetByIdAsync(Guid nodeId)
    {
        var request = new RestRequest(
            $"/api/v1/nodes/{nodeId}",
            Method.Get);

        return await _client.ExecuteAsync(request);
    }
    public async Task WaitForAvailableAsync(
        Guid nodeId,
        TimeSpan? timeout = null)
    {
        var maxWait =
            timeout ?? TimeSpan.FromSeconds(15);

        var deadline = DateTime.UtcNow.Add(maxWait);

        while (DateTime.UtcNow < deadline)
        {
            var response = await GetByIdAsync(nodeId);

            if (!response.IsSuccessful)
            {
                throw new AssertionException(
                    $"Failed to get node '{nodeId}'. " +
                    $"Status: {response.StatusCode}. " +
                    $"Response: {response.Content}");
            }

            using var document =
                JsonDocument.Parse(response.Content!);

            var status =
                document.RootElement
                    .GetProperty("status")
                    .GetString();

            if (string.Equals(
                    status,
                    "Available",
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            await Task.Delay(
                TimeSpan.FromMilliseconds(500));
        }

        throw new AssertionException(
            $"Node '{nodeId}' did not become Available " +
            $"within {maxWait.TotalSeconds} seconds.");
    }
}