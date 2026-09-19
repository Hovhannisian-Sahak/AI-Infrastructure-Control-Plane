using RestSharp;

namespace BeeCloud.ApiTests.Clients;

public class NetworksClient
{
    private readonly RestClient _client;

    public NetworksClient(string baseUrl)
    {
        _client = new RestClient(baseUrl);
    }

    public async Task<RestResponse> CreateAsync(
        string name,
        string? description = null)
    {
        var request = new RestRequest(
            "/api/v1/networks",
            Method.Post);

        request.AddJsonBody(new
        {
            name,
            description
        });

        return await _client.ExecuteAsync(request);
    }

    public async Task<RestResponse> GetAllAsync()
    {
        var request = new RestRequest(
            "/api/v1/networks",
            Method.Get);

        return await _client.ExecuteAsync(request);
    }

    public async Task<RestResponse> GetByIdAsync(Guid networkId)
    {
        var request = new RestRequest(
            $"/api/v1/networks/{networkId}",
            Method.Get);

        return await _client.ExecuteAsync(request);
    }

    public async Task<RestResponse> AttachToNodeAsync(
        Guid nodeId,
        Guid networkId)
    {
        var request = new RestRequest(
            $"/api/v1/nodes/{nodeId}/networks/{networkId}",
            Method.Post);

        return await _client.ExecuteAsync(request);
    }

    public async Task<RestResponse> DetachFromNodeAsync(
        Guid nodeId,
        Guid networkId)
    {
        var request = new RestRequest(
            $"/api/v1/nodes/{nodeId}/networks/{networkId}",
            Method.Delete);

        return await _client.ExecuteAsync(request);
    }

    public async Task<RestResponse> GetByNodeIdAsync(Guid nodeId)
    {
        var request = new RestRequest(
            $"/api/v1/nodes/{nodeId}/networks",
            Method.Get);

        return await _client.ExecuteAsync(request);
    }

    public async Task<RestResponse> GetByNetworkIdAsync(Guid networkId)
    {
        var request = new RestRequest(
            $"/api/v1/networks/{networkId}/nodes",
            Method.Get);

        return await _client.ExecuteAsync(request);
    }
    public async Task<RestResponse> DeactivateAsync(
        Guid networkId)
    {
        var request = new RestRequest(
            $"/api/v1/networks/{networkId}/deactivate",
            Method.Post);

        return await _client.ExecuteAsync(request);
    }

    public async Task<RestResponse> ActivateAsync(
        Guid networkId)
    {
        var request = new RestRequest(
            $"/api/v1/networks/{networkId}/activate",
            Method.Post);

        return await _client.ExecuteAsync(request);
    }
}