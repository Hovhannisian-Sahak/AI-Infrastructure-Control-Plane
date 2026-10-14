using System.Net.Http.Json;
using System.Text.Json;
using BeeCloud.UiTests.Configuration;

namespace BeeCloud.UiTests.Helpers;

/// <summary>
/// Provides reusable API operations for E2E tests
/// </summary>
public static class ApiTestHelper
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Creates an HttpClient configured for the BeeCloud API
    /// </summary>
    public static HttpClient CreateApiClient()
    {
        return new HttpClient
        {
            BaseAddress = new Uri(UiTestSettings.ApiBaseUrl.TrimEnd('/') + "/"),
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    /// <summary>
    /// Finds a node by name through the API
    /// </summary>
    public static async Task<TNode?> FindNodeByNameAsync<TNode>(
        HttpClient api,
        string nodeName,
        CancellationToken cancellationToken = default)
        where TNode : class, INodeIdentifiable
    {
        using var response = await api.GetAsync("api/v1/nodes", cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Failed to retrieve nodes: HTTP {(int)response.StatusCode} - {body}");
        }

        var nodes = JsonSerializer.Deserialize<List<TNode>>(body, JsonOptions);
        return nodes?.SingleOrDefault(node =>
            string.Equals(node.Name, nodeName, StringComparison.Ordinal));
    }

    /// <summary>
    /// Finds a node ID by name through the API
    /// </summary>
    public static async Task<Guid> FindNodeIdByNameAsync(
        HttpClient api,
        string nodeName,
        CancellationToken cancellationToken = default)
    {
        var node = await FindNodeByNameAsync<BasicNodeSnapshot>(
            api,
            nodeName,
            cancellationToken);

        return node?.Id ?? Guid.Empty;
    }

    /// <summary>
    /// Waits for a node to reach the expected status
    /// </summary>
    public static async Task<TNode> WaitForNodeStatusAsync<TNode>(
        HttpClient api,
        Guid nodeId,
        string expectedStatus,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
        where TNode : class, INodeWithStatus
    {
        var deadline = DateTime.UtcNow + timeout;
        TNode? lastNode = null;
        var delay = 500;

        while (DateTime.UtcNow < deadline)
        {
            using var response = await api.GetAsync("api/v1/nodes", cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                await Task.Delay(delay, cancellationToken);
                delay = Math.Min(delay * 2, 5000); // Exponential backoff
                continue;
            }

            var nodes = JsonSerializer.Deserialize<List<TNode>>(body, JsonOptions);
            lastNode = nodes?.SingleOrDefault(node => node.Id == nodeId);

            if (lastNode is not null &&
                string.Equals(
                    lastNode.Status,
                    expectedStatus,
                    StringComparison.OrdinalIgnoreCase))
            {
                return lastNode;
            }

            await Task.Delay(delay, cancellationToken);
        }

        throw new TimeoutException(
            $"Node {nodeId} did not reach status '{expectedStatus}' within " +
            $"{timeout.TotalSeconds:0}s. Last status: " +
            $"{lastNode?.Status ?? "<not found>"}");
    }

    /// <summary>
    /// Waits for an incident matching the predicate
    /// </summary>
    public static async Task<TIncident?> WaitForIncidentAsync<TIncident>(
        HttpClient api,
        Guid nodeId,
        Func<TIncident, bool> predicate,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
        where TIncident : class, IIncidentIdentifiable
    {
        var deadline = DateTime.UtcNow + timeout;
        var delay = 500;

        while (DateTime.UtcNow < deadline)
        {
            using var response = await api.GetAsync(
                $"api/v1/incidents/search?computeNodeId={nodeId}&page=1&pageSize=100",
                cancellationToken);

            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                await Task.Delay(delay, cancellationToken);
                delay = Math.Min(delay * 2, 5000);
                continue;
            }

            var page = JsonSerializer.Deserialize<IncidentPageSnapshot<TIncident>>(
                body,
                JsonOptions);

            var incident = page?.Items
                .Where(item => item.ComputeNodeId == nodeId)
                .OrderByDescending(item => item.CreatedAt)
                .FirstOrDefault(predicate);

            if (incident is not null)
            {
                return incident;
            }

            await Task.Delay(delay, cancellationToken);
        }

        return null;
    }

    /// <summary>
    /// Simulates a fault on a node via API
    /// </summary>
    public static async Task SimulateFaultAsync(
        HttpClient api,
        Guid nodeId,
        object faultPayload,
        CancellationToken cancellationToken = default)
    {
        using var response = await api.PostAsJsonAsync(
            $"api/v1/nodes/{nodeId}/simulate/fault",
            faultPayload,
            cancellationToken);

        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Fault simulation failed: HTTP {(int)response.StatusCode} - {body}");
        }
    }

    /// <summary>
    /// Clears a simulated fault from a node via API
    /// </summary>
    public static async Task ClearFaultAsync(
        HttpClient api,
        Guid nodeId,
        CancellationToken cancellationToken = default)
    {
        using var response = await api.DeleteAsync(
            $"api/v1/nodes/{nodeId}/simulate/fault",
            cancellationToken);

        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Clearing fault failed: HTTP {(int)response.StatusCode} - {body}");
        }
    }

    /// <summary>
    /// Starts a node via API
    /// </summary>
    public static async Task StartNodeAsync(
        HttpClient api,
        Guid nodeId,
        CancellationToken cancellationToken = default)
    {
        using var response = await api.PostAsync(
            $"api/v1/nodes/{nodeId}/start",
            content: null,
            cancellationToken);

        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Starting node failed: HTTP {(int)response.StatusCode} - {body}");
        }
    }

    /// <summary>
    /// Creates a node via API with retry logic
    /// </summary>
    public static async Task<TNode> CreateNodeAsync<TNode>(
        HttpClient api,
        object nodePayload,
        int maxRetries = 3,
        CancellationToken cancellationToken = default)
    {
        Exception? lastException = null;

        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                using var response = await api.PostAsJsonAsync(
                    "api/v1/nodes",
                    nodePayload,
                    cancellationToken);

                var body = await response.Content.ReadAsStringAsync(cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var node = JsonSerializer.Deserialize<TNode>(body, JsonOptions);
                    if (node is null)
                    {
                        throw new InvalidOperationException(
                            "Failed to deserialize created node response");
                    }
                    return node;
                }

                if (attempt == maxRetries)
                {
                    throw new InvalidOperationException(
                        $"Creating node failed after {maxRetries} attempts: " +
                        $"HTTP {(int)response.StatusCode} - {body}");
                }
            }
            catch (Exception ex) when (attempt < maxRetries)
            {
                lastException = ex;
                await Task.Delay(1000 * attempt, cancellationToken);
            }
        }

        throw lastException ?? new InvalidOperationException("Failed to create node");
    }

    /// <summary>
    /// Creates an incident via API with retry logic
    /// </summary>
    public static async Task<TIncident> CreateIncidentAsync<TIncident>(
        HttpClient api,
        object incidentPayload,
        int maxRetries = 3,
        CancellationToken cancellationToken = default)
    {
        Exception? lastException = null;

        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                using var response = await api.PostAsJsonAsync(
                    "api/v1/incidents",
                    incidentPayload,
                    cancellationToken);

                var body = await response.Content.ReadAsStringAsync(cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var incident = JsonSerializer.Deserialize<TIncident>(body, JsonOptions);
                    if (incident is null)
                    {
                        throw new InvalidOperationException(
                            "Failed to deserialize created incident response");
                    }
                    return incident;
                }

                if (attempt == maxRetries)
                {
                    throw new InvalidOperationException(
                        $"Creating incident failed after {maxRetries} attempts: " +
                        $"HTTP {(int)response.StatusCode} - {body}");
                }
            }
            catch (Exception ex) when (attempt < maxRetries)
            {
                lastException = ex;
                await Task.Delay(1000 * attempt, cancellationToken);
            }
        }

        throw lastException ?? new InvalidOperationException("Failed to create incident");
    }

    /// <summary>
    /// Creates a network via API with retry logic
    /// </summary>
    public static async Task<TNetwork> CreateNetworkAsync<TNetwork>(
        HttpClient api,
        object networkPayload,
        int maxRetries = 3,
        CancellationToken cancellationToken = default)
    {
        Exception? lastException = null;

        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                using var response = await api.PostAsJsonAsync(
                    "api/v1/networks",
                    networkPayload,
                    cancellationToken);

                var body = await response.Content.ReadAsStringAsync(cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var network = JsonSerializer.Deserialize<TNetwork>(body, JsonOptions);
                    if (network is null)
                    {
                        throw new InvalidOperationException(
                            "Failed to deserialize created network response");
                    }
                    return network;
                }

                if (attempt == maxRetries)
                {
                    throw new InvalidOperationException(
                        $"Creating network failed after {maxRetries} attempts: " +
                        $"HTTP {(int)response.StatusCode} - {body}");
                }
            }
            catch (Exception ex) when (attempt < maxRetries)
            {
                lastException = ex;
                await Task.Delay(1000 * attempt, cancellationToken);
            }
        }

        throw lastException ?? new InvalidOperationException("Failed to create network");
    }

    /// <summary>
    /// Finds a network by name through the API
    /// </summary>
    public static async Task<TNetwork?> FindNetworkByNameAsync<TNetwork>(
        HttpClient api,
        string networkName,
        CancellationToken cancellationToken = default)
        where TNetwork : class, INodeIdentifiable
    {
        using var response = await api.GetAsync("api/v1/networks", cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Failed to retrieve networks: HTTP {(int)response.StatusCode} - {body}");
        }

        var networks = JsonSerializer.Deserialize<List<TNetwork>>(body, JsonOptions);
        return networks?.SingleOrDefault(network =>
            string.Equals(network.Name, networkName, StringComparison.Ordinal));
    }

    /// <summary>
    /// Attaches a node to a network via API
    /// </summary>
    public static async Task AttachNodeToNetworkAsync(
        HttpClient api,
        Guid networkId,
        Guid nodeId,
        CancellationToken cancellationToken = default)
    {
        using var response = await api.PostAsJsonAsync(
            $"api/v1/networks/{networkId}/attachments",
            new { nodeId },
            cancellationToken);

        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Attaching node to network failed: HTTP {(int)response.StatusCode} - {body}");
        }
    }

    /// <summary>
    /// Waits for a specific count of items to be available
    /// </summary>
    public static async Task WaitForCountAsync<T>(
        Func<Task<int>> countProvider,
        int expectedCount,
        TimeSpan timeout,
        string itemDescription = "items",
        CancellationToken cancellationToken = default)
    {
        var deadline = DateTime.UtcNow + timeout;
        var delay = 500;
        var lastCount = 0;

        while (DateTime.UtcNow < deadline)
        {
            lastCount = await countProvider();

            if (lastCount == expectedCount)
            {
                return;
            }

            await Task.Delay(delay, cancellationToken);
        }

        throw new TimeoutException(
            $"Expected {expectedCount} {itemDescription}, but found {lastCount} " +
            $"after {timeout.TotalSeconds:0}s");
    }
}

/// <summary>
/// Marker interface for entities with Id and Name
/// </summary>
public interface INodeIdentifiable
{
    Guid Id { get; }
    string Name { get; }
}

/// <summary>
/// Marker interface for entities with Status
/// </summary>
public interface INodeWithStatus : INodeIdentifiable
{
    string Status { get; }
}

/// <summary>
/// Marker interface for incidents
/// </summary>
public interface IIncidentIdentifiable
{
    Guid Id { get; }
    Guid ComputeNodeId { get; }
    string Status { get; }
    string Title { get; }
    DateTime CreatedAt { get; }
}

/// <summary>
/// Basic node snapshot
/// </summary>
public sealed record BasicNodeSnapshot(Guid Id, string Name) : INodeIdentifiable;

/// <summary>
/// Generic incident page response
/// </summary>
public sealed record IncidentPageSnapshot<T>(
    List<T> Items,
    int Page,
    int PageSize,
    int TotalCount);
