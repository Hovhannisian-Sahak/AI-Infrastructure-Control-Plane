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
[Category("Incidents")]
public sealed class IncidentInvestigationWorkflowTests : UiTestBase
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    [Test]
    [Timeout(180_000)]
    public async Task ResolvedGpuIncident_ShouldBeFilterableAndNavigateToAffectedNode()
    {
        var dashboard = new DashboardPage(Page);
        await dashboard.OpenAsync();

        var nodeName = await NodeTestHelper.CreateNodeAsync(Page);
        await NodeTestHelper.WaitForNodeStatusAsync(
            Page, nodeName, "Available", UiTestSettings.ProvisioningTimeoutMs);

        await dashboard.NodeCard(nodeName).GetByRole(
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

        var incident = await WaitForResolvedGpuIncidentAsync(api, node.Id, TimeSpan.FromSeconds(120));
        Assert.That(incident, Is.Not.Null, "The GPU failure incident did not resolve within the timeout.");

        await Page.ReloadAsync();
        await dashboard.Heading.WaitForAsync();

        var statusFilter = Page.GetByLabel("Status", new() { Exact = true });
        var nodeFilter = Page.GetByLabel("Node", new() { Exact = true });

        await statusFilter.SelectOptionAsync("Resolved");
        await nodeFilter.SelectOptionAsync(new SelectOptionValue { Label = nodeName });

        var incidentCard = Page.Locator("article").Filter(new()
        {
            Has = Page.GetByRole(
                AriaRole.Heading,
                new() { Name = "GPU failure detected", Exact = true })
        });

        await Expect(incidentCard).ToBeVisibleAsync();
        await Expect(incidentCard.GetByText("Resolved", new() { Exact = true }))
            .ToBeVisibleAsync();

        var affectedNodeLink = incidentCard.GetByRole(
            AriaRole.Link,
            new() { Name = $"Affected node {nodeName}", Exact = true });

        await Expect(affectedNodeLink).ToBeVisibleAsync();
        await affectedNodeLink.ClickAsync();

        var detail = new NodeDetailPage(Page);
        await Expect(detail.Heading(nodeName)).ToBeVisibleAsync();
        await Expect(detail.ActiveFaultLabel).ToBeVisibleAsync();

        await detail.BackToNodes.ClickAsync();
        await dashboard.Heading.WaitForAsync();

        await statusFilter.SelectOptionAsync("Resolved");
        await nodeFilter.SelectOptionAsync(new SelectOptionValue { Label = nodeName });
        await Expect(incidentCard).ToBeVisibleAsync();
        await Expect(incidentCard.GetByText("Resolved", new() { Exact = true }))
            .ToBeVisibleAsync();
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

    private static async Task<IncidentSnapshot?> WaitForResolvedGpuIncidentAsync(
        HttpClient api,
        Guid nodeId,
        TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;

        while (DateTime.UtcNow < deadline)
        {
            using var response = await api.GetAsync(
                $"api/v1/incidents/search?computeNodeId={nodeId}&page=1&pageSize=20");
            response.EnsureSuccessStatusCode();

            var page = await response.Content.ReadFromJsonAsync<IncidentPageSnapshot>(JsonOptions);
            var incident = page?.Items
                .Where(item => string.Equals(
                    item.Title, "GPU failure detected", StringComparison.Ordinal))
                .OrderByDescending(item => item.CreatedAt)
                .FirstOrDefault();

            if (incident is not null
                && string.Equals(incident.Status, "Resolved", StringComparison.OrdinalIgnoreCase))
            {
                return incident;
            }

            await Task.Delay(500);
        }

        return null;
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
