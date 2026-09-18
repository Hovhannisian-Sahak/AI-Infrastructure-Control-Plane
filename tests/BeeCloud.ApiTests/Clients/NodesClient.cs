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
}