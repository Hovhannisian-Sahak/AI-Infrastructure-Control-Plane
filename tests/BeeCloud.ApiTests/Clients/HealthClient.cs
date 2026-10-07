using BeeCloud.Application.DTOs.Health;
using BeeCloud.Application.DTOs.Pagination;
using RestSharp;

namespace BeeCloud.ApiTests.Clients;

public class HealthClient
{
    private readonly RestClient _client;

    public HealthClient(string baseUrl)
    {
        _client = new RestClient(baseUrl);
    }

    public async Task<RestResponse<HealthCheckResponse>> CreateAsync(
        Guid nodeId,
        HealthCheckRequest requestModel)
    {
        var request = new RestRequest(
            $"/api/v1/nodes/{nodeId}/health",
            Method.Post);

        request.AddJsonBody(requestModel);

        return await _client.ExecuteAsync<HealthCheckResponse>(request);
    }

    public async Task<RestResponse<HealthCheckResponse>> GetLatestAsync(
        Guid nodeId)
    {
        var request = new RestRequest(
            $"/api/v1/nodes/{nodeId}/health",
            Method.Get);

        return await _client.ExecuteAsync<HealthCheckResponse>(request);
    }

    public async Task<RestResponse<List<HealthCheckResponse>>> GetHistoryAsync(
        Guid nodeId,
        DateTime? from = null,
        DateTime? to = null,
        int? limit = null)
    {
        var request = new RestRequest(
            $"/api/v1/nodes/{nodeId}/health/history",
            Method.Get);

        AddHistoryParameters(request, from, to, limit);

        return await _client.ExecuteAsync<List<HealthCheckResponse>>(request);
    }

    public async Task<RestResponse<CursorPageResponse<HealthCheckResponse>>>
        GetHistoryPageAsync(
            Guid nodeId,
            DateTime? from = null,
            DateTime? to = null,
            string? cursor = null,
            bool previous = false,
            int? limit = null)
    {
        var request = new RestRequest(
            $"/api/v1/nodes/{nodeId}/health/history/page",
            Method.Get);

        AddHistoryParameters(request, from, to, limit);
        if (cursor is not null)
        {
            request.AddQueryParameter("cursor", cursor);
        }

        if (previous)
        {
            request.AddQueryParameter("previous", true);
        }

        return await _client
            .ExecuteAsync<CursorPageResponse<HealthCheckResponse>>(request);
    }

    private static void AddHistoryParameters(
        RestRequest request,
        DateTime? from,
        DateTime? to,
        int? limit)
    {
        if (from.HasValue)
        {
            request.AddQueryParameter(
                "from",
                from.Value.ToString("O"));
        }

        if (to.HasValue)
        {
            request.AddQueryParameter(
                "to",
                to.Value.ToString("O"));
        }

        if (limit.HasValue)
        {
            request.AddQueryParameter("limit", limit.Value);
        }
    }
}
