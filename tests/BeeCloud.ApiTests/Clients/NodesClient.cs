using BeeCloud.ApiTests.Models;
using BeeCloud.ApiTests.Models.Requests;
using BeeCloud.Domain.Enums;
using RestSharp;

namespace BeeCloud.ApiTests.Clients;

public class NodesClient
{
    private readonly RestClient _client;

    public NodesClient(string baseUrl)
    {
        _client = new RestClient(baseUrl);
    }

    public async Task<RestResponse<ComputeNodeResponseModel>> CreateAsync(
        CreateNodeRequestModel requestModel)
    {
        var request = new RestRequest(
            "/api/v1/nodes",
            Method.Post);

        request.AddJsonBody(requestModel);

        return await _client.ExecuteAsync<ComputeNodeResponseModel>(request);
    }
    
    public async Task<RestResponse<List<ComputeNodeResponseModel>>> GetAllAsync()
    {
        var request = new RestRequest(
            "/api/v1/nodes",
            Method.Get);

        return await _client.ExecuteAsync<List<ComputeNodeResponseModel>>(request);
    }

    public async Task<RestResponse<ComputeNodeResponseModel>> GetByIdAsync(
        Guid nodeId)
    {
        var request = new RestRequest(
            $"/api/v1/nodes/{nodeId}",
            Method.Get);

        return await _client.ExecuteAsync<ComputeNodeResponseModel>(request);
    }
    
    public async Task<RestResponse> DeleteAsync(
        Guid nodeId)
    {
        var request = new RestRequest(
            $"/api/v1/nodes/{nodeId}",
            Method.Delete);

        return await _client.ExecuteAsync(request);
    }

    public async Task<RestResponse<ComputeNodeResponseModel>> StartAsync(
        Guid nodeId)
    {
        var request = new RestRequest(
            $"/api/v1/nodes/{nodeId}/start",
            Method.Post);

        return await _client.ExecuteAsync<ComputeNodeResponseModel>(request);
    }

    public async Task<RestResponse<ComputeNodeResponseModel>> StopAsync(
        Guid nodeId)
    {
        var request = new RestRequest(
            $"/api/v1/nodes/{nodeId}/stop",
            Method.Post);

        return await _client.ExecuteAsync<ComputeNodeResponseModel>(request);
    }
    public async Task<RestResponse<ComputeNodeResponseModel>> RestartAsync(
        Guid nodeId)
    {
        var request = new RestRequest(
            $"/api/v1/nodes/{nodeId}/restart",
            Method.Post);

        return await _client.ExecuteAsync<ComputeNodeResponseModel>(request);
    }
    public async Task<RestResponse<ComputeNodeResponseModel>> SimulateFaultAsync(
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

        return await _client.ExecuteAsync<ComputeNodeResponseModel>(request);
    }
    
    public async Task<RestResponse<ComputeNodeResponseModel>> ClearFaultAsync(
        Guid nodeId)
    {
        var request = new RestRequest(
            $"/api/v1/nodes/{nodeId}/simulate/fault",
            Method.Delete);

        return await _client.ExecuteAsync<ComputeNodeResponseModel>(request);
    }

    public async Task<ComputeNodeResponseModel> WaitForAvailableAsync(
        Guid nodeId,
        TimeSpan? timeout = null)
    {
        return await WaitForStatusAsync(
            nodeId,
            NodeStatus.Available,
            timeout);
    }
    
    public async Task<ComputeNodeResponseModel> WaitForStoppingAsync(
        Guid nodeId,
        TimeSpan? timeout = null)
    {
        return await WaitForStatusAsync(
            nodeId,
            NodeStatus.Stopping,
            timeout);
    }

    public async Task<ComputeNodeResponseModel> WaitForRunningAsync(
        Guid nodeId,
        TimeSpan? timeout = null)
    {
        return await WaitForStatusAsync(
            nodeId,
            NodeStatus.Running,
            timeout);
    }

    private async Task<ComputeNodeResponseModel> WaitForStatusAsync(
        Guid nodeId,
        NodeStatus expectedStatus,
        TimeSpan? timeout)
    {
        var maxWait = timeout ?? TimeSpan.FromSeconds(15);
        var deadline = DateTime.UtcNow.Add(maxWait);

        ComputeNodeResponseModel? lastNode = null;

        while (DateTime.UtcNow < deadline)
        {
            var response = await GetByIdAsync(nodeId);

            if (!response.IsSuccessful)
            {
                throw new InvalidOperationException(
                    $"Failed to get node '{nodeId}'. " +
                    $"Status: {response.StatusCode}. " +
                    $"Response: {response.Content}");
            }

            lastNode = response.Data;

            if (lastNode is not null &&
                string.Equals(
                    lastNode.Status,
                    expectedStatus.ToString(),
                    StringComparison.OrdinalIgnoreCase))
            {
                return lastNode;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500));
        }

        throw new TimeoutException(
            $"Node '{nodeId}' did not become {expectedStatus} " +
            $"within {maxWait.TotalSeconds} seconds. " +
            $"Last observed status: {lastNode?.Status}.");
    }
}