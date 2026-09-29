using BeeCloud.ApiTests.Models;
using BeeCloud.Application.DTOs.Incidents;
using BeeCloud.Domain.Enums;
using RestSharp;

namespace BeeCloud.ApiTests.Clients;

public class IncidentsClient
{
    private readonly RestClient _client;

    public IncidentsClient(string baseUrl)
    {
        _client = new RestClient(baseUrl);
    }

    public async Task<RestResponse<List<IncidentResponseModel>>> GetAllAsync(
        IncidentSeverity? severity = null,
        IncidentStatus? status = null)
    {
        var request = new RestRequest("/api/v1/incidents", Method.Get);

        if (severity.HasValue)
            request.AddQueryParameter("severity", severity.Value.ToString());

        if (status.HasValue)
            request.AddQueryParameter("status", status.Value.ToString());

        return await _client.ExecuteAsync<List<IncidentResponseModel>>(request);
    }

    public async Task<RestResponse<IncidentResponseModel>> GetByIdAsync(
        Guid incidentId)
    {
        var request = new RestRequest(
            $"/api/v1/incidents/{incidentId}",
            Method.Get);

        return await _client.ExecuteAsync<IncidentResponseModel>(request);
    }

    public async Task<IncidentResponseModel> WaitForIncidentAsync(
        Guid nodeId,
        TimeSpan? timeout = null)
    {
        var maxWait = timeout ?? TimeSpan.FromSeconds(15);
        var deadline = DateTime.UtcNow.Add(maxWait);

        while (DateTime.UtcNow < deadline)
        {
            var response = await GetAllAsync();

            if (!response.IsSuccessful)
            {
                throw new InvalidOperationException(
                    $"Failed to get incidents. " +
                    $"Status: {response.StatusCode}. " +
                    $"Response: {response.Content}");
            }

            var incident = response.Data?
                .Where(x => x.ComputeNodeId == nodeId)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefault();

            if (incident is not null)
            {
                TestContext.WriteLine(
                    $"Found incident for node {nodeId}: " +
                    $"Id={incident.Id}, " +
                    $"Status={incident.Status}, " +
                    $"Severity={incident.Severity}, " +
                    $"CreatedAt={incident.CreatedAt}");

                return incident;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500));
        }

        throw new TimeoutException(
            $"No incident was created for node '{nodeId}' " +
            $"within {maxWait.TotalSeconds} seconds.");
    }
    public async Task<IncidentResponseModel> WaitForResolvedIncidentAsync(
        Guid incidentId,
        TimeSpan? timeout = null)
    {
        var maxWait = timeout ?? TimeSpan.FromSeconds(30);
        var deadline = DateTime.UtcNow.Add(maxWait);

        while (DateTime.UtcNow < deadline)
        {
            var response = await GetByIdAsync(incidentId);

            if (!response.IsSuccessful)
            {
                throw new InvalidOperationException(
                    $"Failed to get incident '{incidentId}'. " +
                    $"Status: {response.StatusCode}. " +
                    $"Response: {response.Content}");
            }

            var incident = response.Data;

            if (incident is not null &&
                string.Equals(
                    incident.Status,
                    nameof(IncidentStatus.Resolved),
                    StringComparison.OrdinalIgnoreCase))
            {
                return incident;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500));
        }

        throw new TimeoutException(
            $"Incident '{incidentId}' was not resolved " +
            $"within {maxWait.TotalSeconds} seconds.");
    }
}