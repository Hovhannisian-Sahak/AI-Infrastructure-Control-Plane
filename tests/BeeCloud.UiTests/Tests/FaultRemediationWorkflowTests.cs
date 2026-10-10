using System.Net.Http.Json;
using System.Text.Json;
using BeeCloud.UiTests.Configuration;
using BeeCloud.UiTests.Fixtures;
using BeeCloud.UiTests.Helpers;
using BeeCloud.UiTests.Pages;
using Microsoft.Playwright;
using NUnit.Framework;
using static Microsoft.Playwright.Assertions;

namespace BeeCloud.UiTests.Tests;

[TestFixture]
[Category("E2E")]
[Category("Workflows")]
public sealed class FaultRemediationWorkflowTests : UiTestBase
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    [Test]
    [Timeout(180_000)]
    public async Task GpuFailure_ShouldCreateIncidentQuarantineRemediateAndRecoverNode()
    {
        var dashboard = new DashboardPage(Page);
        await dashboard.OpenAsync();

        var nodeName = await NodeTestHelper.CreateNodeAsync(Page);
        await NodeTestHelper.WaitForNodeStatusAsync(
            Page, nodeName, "Available", UiTestSettings.ProvisioningTimeoutMs);

        var nodeCard = dashboard.NodeCard(nodeName);
        await nodeCard.GetByRole(
            AriaRole.Button,
            new() { Name = "Start", Exact = true }).ClickAsync();
        await NodeTestHelper.WaitForNodeStatusAsync(
            Page, nodeName, "Running", UiTestSettings.LifecycleTimeoutMs);

        using var api = new HttpClient
        {
            BaseAddress = new Uri(UiTestSettings.ApiBaseUrl.TrimEnd('/') + "/")
        };

        var node = await FindNodeByNameAsync(api, nodeName);
        Assert.That(node, Is.Not.Null, $"Created node '{nodeName}' was not found through the API.");

        using var faultResponse = await api.PostAsJsonAsync(
            $"api/v1/nodes/{node!.Id}/simulate/fault",
            new { fault = "GpuFailure" });
        Assert.That(
            faultResponse.IsSuccessStatusCode,
            Is.True,
            $"Fault simulation failed with HTTP {(int)faultResponse.StatusCode}: {await faultResponse.Content.ReadAsStringAsync()}");

        var observedStatuses = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        IncidentSnapshot? incident = null;
        NodeSnapshot? recoveredNode = null;
        var deadline = DateTime.UtcNow.AddSeconds(120);

        while (DateTime.UtcNow < deadline)
        {
            var currentNode = await GetNodeAsync(api, node.Id);
            observedStatuses.Add(currentNode.Status);

            var incidents = await GetIncidentsForNodeAsync(api, node.Id);
            incident = incidents
                .Where(item => string.Equals(
                    item.Title,
                    "GPU failure detected",
                    StringComparison.Ordinal))
                .OrderByDescending(item => item.CreatedAt)
                .FirstOrDefault() ?? incident;

            if (currentNode.Status == "Available"
                && currentNode.ActiveFault == "None"
                && incident is not null
                && string.Equals(incident.Status, "Resolved", StringComparison.OrdinalIgnoreCase))
            {
                recoveredNode = currentNode;
                break;
            }

            await Task.Delay(200);
        }

        Assert.That(
            incident,
            Is.Not.Null,
            "Health monitoring did not create the expected GPU failure incident.");
        Assert.That(
            observedStatuses,
            Does.Contain("Unhealthy").Or.Contain("Remediating"),
            $"The node never entered an unhealthy/remediation state. Observed: {string.Join(", ", observedStatuses)}");
        Assert.That(
            observedStatuses,
            Does.Contain("Remediating"),
            $"The remediation worker state was not observed. Observed: {string.Join(", ", observedStatuses)}");
        Assert.That(
            recoveredNode,
            Is.Not.Null,
            $"The node did not recover and the incident did not resolve. Observed statuses: {string.Join(", ", observedStatuses)}");
        Assert.That(recoveredNode!.Status, Is.EqualTo("Available"));
        Assert.That(recoveredNode.ActiveFault, Is.EqualTo("None"));
        Assert.That(incident!.Status, Is.EqualTo("Resolved").IgnoreCase);

        TestContext.WriteLine(
            $"Observed node lifecycle states: {string.Join(" -> ", observedStatuses)}");
        TestContext.WriteLine(
            $"GPU failure incident {incident.Id} finished with status {incident.Status}.");

        await Page.ReloadAsync();
        await dashboard.Heading.WaitForAsync();
        await NodeTestHelper.WaitForNodeStatusAsync(
            Page, nodeName, "Available", UiTestSettings.LifecycleTimeoutMs);

        nodeCard = dashboard.NodeCard(nodeName);
        await Expect(nodeCard.GetByLabel(
            "Node status: Available",
            new() { Exact = true })).ToBeVisibleAsync();
        await Expect(nodeCard.GetByText(
            "None",
            new() { Exact = true })).ToBeVisibleAsync();

        var incidentCard = Page.Locator("article").Filter(new()
        {
            Has = Page.GetByRole(
                AriaRole.Heading,
                new() { Name = "GPU failure detected", Exact = true })
        });

        await Expect(incidentCard).ToBeVisibleAsync();
        await Expect(incidentCard.GetByText(
            "Resolved",
            new() { Exact = true })).ToBeVisibleAsync();
        await Expect(incidentCard.GetByRole(
            AriaRole.Link,
            new() { Name = nodeName, Exact = true })).ToBeVisibleAsync();
    }

    private static async Task<NodeSnapshot?> FindNodeByNameAsync(
        HttpClient api,
        string nodeName)
    {
        using var response = await api.GetAsync("api/v1/nodes");
        response.EnsureSuccessStatusCode();

        var nodes = await response.Content.ReadFromJsonAsync<List<NodeSnapshot>>(JsonOptions);
        return nodes?.SingleOrDefault(node =>
            string.Equals(node.Name, nodeName, StringComparison.Ordinal));
    }

    private static async Task<NodeSnapshot> GetNodeAsync(
        HttpClient api,
        Guid nodeId)
    {
        using var response = await api.GetAsync($"api/v1/nodes/{nodeId}");
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<NodeSnapshot>(JsonOptions)
            ?? throw new InvalidOperationException($"Node '{nodeId}' API response was empty.");
    }

    private static async Task<List<IncidentSnapshot>> GetIncidentsForNodeAsync(
        HttpClient api,
        Guid nodeId)
    {
        var path = $"api/v1/incidents/search?computeNodeId={nodeId}&page=1&pageSize=20";
        using var response = await api.GetAsync(path);
        response.EnsureSuccessStatusCode();

        var page = await response.Content.ReadFromJsonAsync<IncidentPageSnapshot>(JsonOptions);
        return page?.Items ?? [];
    }

    private sealed record NodeSnapshot(
        Guid Id,
        string Name,
        string Status,
        string ActiveFault);

    private sealed record IncidentSnapshot(
        Guid Id,
        Guid ComputeNodeId,
        string Status,
        string Title,
        DateTime CreatedAt);

    private sealed record IncidentPageSnapshot(
        List<IncidentSnapshot> Items,
        int Page,
        int PageSize,
        int TotalCount);
}
