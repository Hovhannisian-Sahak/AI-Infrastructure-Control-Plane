using BeeCloud.Application.DTOs.NodeMetrics;
using BeeCloud.Application.DTOs.Pagination;
using RestSharp;

namespace BeeCloud.ApiTests.Clients;

public class NodeMetricsClient
{
    private readonly RestClient _client;

    public NodeMetricsClient(string baseUrl)
    {
        _client = new RestClient(baseUrl);
    }

    public async Task<RestResponse<NodeMetricResponse>> GetByIdAsync(
        Guid metricId)
    {
        var request = new RestRequest(
            $"/api/v1/node-metrics/{metricId}",
            Method.Get);

        return await _client.ExecuteAsync<NodeMetricResponse>(request);
    }

    public async Task<RestResponse<List<NodeMetricResponse>>> GetHistoryAsync(
        Guid nodeId,
        DateTime? from = null,
        DateTime? to = null,
        int? limit = null)
    {
        var request = new RestRequest(
            $"/api/v1/nodes/{nodeId}/metrics",
            Method.Get);

        AddHistoryParameters(request, from, to, limit);

        return await _client.ExecuteAsync<List<NodeMetricResponse>>(request);
    }

    public async Task<RestResponse<CursorPageResponse<NodeMetricResponse>>>
        GetHistoryPageAsync(
            Guid nodeId,
            DateTime? from = null,
            DateTime? to = null,
            string? cursor = null,
            bool previous = false,
            int? limit = null)
    {
        var request = new RestRequest(
            $"/api/v1/nodes/{nodeId}/metrics/page",
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
            .ExecuteAsync<CursorPageResponse<NodeMetricResponse>>(request);
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
