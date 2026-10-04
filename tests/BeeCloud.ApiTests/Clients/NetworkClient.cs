using BeeCloud.ApiTests.Models;
using RestSharp;

namespace BeeCloud.ApiTests.Clients;

public class NetworksClient
{
    private readonly RestClient _client;

    public NetworksClient(string baseUrl)
    {
        _client = new RestClient(baseUrl);
    }

    public async Task<RestResponse<NetworkResponseModel>> CreateAsync(
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

        return await _client.ExecuteAsync<NetworkResponseModel>(request);
    }

    public async Task<RestResponse<List<NetworkResponseModel>>> GetAllAsync()
    {
        var request = new RestRequest(
            "/api/v1/networks",
            Method.Get);

        return await _client.ExecuteAsync<List<NetworkResponseModel>>(request);
    }

    public async Task<RestResponse<NetworkResponseModel>> GetByIdAsync(
        Guid networkId)
    {
        var request = new RestRequest(
            $"/api/v1/networks/{networkId}",
            Method.Get);

        return await _client.ExecuteAsync<NetworkResponseModel>(request);
    }
    
    public async Task<RestResponse> DeleteAsync(
        Guid networkId)
    {
        var request = new RestRequest(
            $"/api/v1/networks/{networkId}",
            Method.Delete);

        return await _client.ExecuteAsync(request);
    }

    public async Task<RestResponse<NetworkAttachmentResponseModel>> AttachToNodeAsync(
        Guid nodeId,
        Guid networkId)
    {
        var request = new RestRequest(
            $"/api/v1/nodes/{nodeId}/networks/{networkId}",
            Method.Post);

        return await _client.ExecuteAsync<NetworkAttachmentResponseModel>(request);
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

    public async Task<RestResponse<List<NetworkAttachmentResponseModel>>> GetByNodeIdAsync(
        Guid nodeId)
    {
        var request = new RestRequest(
            $"/api/v1/nodes/{nodeId}/networks",
            Method.Get);

        return await _client.ExecuteAsync<List<NetworkAttachmentResponseModel>>(request);
    }

    public async Task<RestResponse<List<NetworkAttachmentResponseModel>>> GetByNetworkIdAsync(
        Guid networkId)
    {
        var request = new RestRequest(
            $"/api/v1/networks/{networkId}/nodes",
            Method.Get);

        return await _client.ExecuteAsync<List<NetworkAttachmentResponseModel>>(request);
    }

    public async Task<RestResponse<NetworkResponseModel>> DeactivateAsync(
        Guid networkId)
    {
        var request = new RestRequest(
            $"/api/v1/networks/{networkId}/deactivate",
            Method.Post);

        return await _client.ExecuteAsync<NetworkResponseModel>(request);
    }

    public async Task<RestResponse<NetworkResponseModel>> ActivateAsync(
        Guid networkId)
    {
        var request = new RestRequest(
            $"/api/v1/networks/{networkId}/activate",
            Method.Post);

        return await _client.ExecuteAsync<NetworkResponseModel>(request);
    }
}