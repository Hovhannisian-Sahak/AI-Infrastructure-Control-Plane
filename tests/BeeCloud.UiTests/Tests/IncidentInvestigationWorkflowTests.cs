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
    private const int MaxIncidentResolutionTimeoutSeconds = 120;

    [Test]
    [Timeout(180_000)]
    public async Task ResolvedGpuIncident_ShouldBeFilterableAndNavigateToAffectedNode()
    {
        // Arrange: Create, start node, and simulate GPU fault
        var dashboard = new DashboardPage(Page);
        await dashboard.OpenAsync();

        var nodeName = await NodeTestHelper.CreateNodeAsync(Page);
        await NodeTestHelper.WaitForNodeStatusAsync(
            Page, nodeName, "Available", UiTestSettings.ProvisioningTimeoutMs);

        await dashboard.NodeCard(nodeName).GetByRole(AriaRole.Button, new()
        {
            Name = "Start",
            Exact = true
        }).ClickAsync();

        await NodeTestHelper.WaitForNodeStatusAsync(
            Page, nodeName, "Running", UiTestSettings.LifecycleTimeoutMs);

        using var api = ApiTestHelper.CreateApiClient();

        var node = await ApiTestHelper.FindNodeByNameAsync<NodeSnapshot>(api, nodeName);
        Assert.That(node, Is.Not.Null,
            $"Created node '{nodeName}' was not found through the API");

        // Act: Simulate GPU fault
        await ApiTestHelper.SimulateFaultAsync(api, node!.Id, new { fault = "GpuFailure" });

        // Assert: Wait for resolved incident
        var incident = await ApiTestHelper.WaitForIncidentAsync<IncidentSnapshot>(
            api,
            node.Id,
            inc => string.Equals(inc.Title, "GPU failure detected", StringComparison.Ordinal) &&
                   string.Equals(inc.Status, "Resolved", StringComparison.OrdinalIgnoreCase),
            TimeSpan.FromSeconds(MaxIncidentResolutionTimeoutSeconds));

        Assert.That(
            incident,
            Is.Not.Null,
            "The GPU failure incident did not reach Resolved within the timeout");

        // Assert: Filter and verify incident in UI
        await Page.ReloadAsync();
        await dashboard.Heading.WaitForAsync();

        await Page.GetByLabel("Status", new() { Exact = true })
            .SelectOptionAsync("Resolved");

        await Page.GetByLabel("Node", new() { Exact = true })
            .SelectOptionAsync(new SelectOptionValue { Label = nodeName });

        var incidentHistory = Page.Locator("details")
            .Filter(new() { HasText = nodeName });

        await Expect(incidentHistory).ToBeVisibleAsync(
            new() { Timeout = 15_000 });

        await incidentHistory.Locator("summary").ClickAsync();

        var incidentCard = incidentHistory.Locator("article")
            .Filter(new()
            {
                Has = Page.GetByRole(AriaRole.Heading, new()
                {
                    Name = incident!.Title,
                    Exact = true
                })
            });

        await Expect(incidentCard).ToBeVisibleAsync(
            new() { Timeout = 15_000 });

        await Expect(incidentCard.GetByText("Resolved", new()
        {
            Exact = true
        })).ToBeVisibleAsync();

        var affectedNodeLink = incidentCard.GetByRole(AriaRole.Link, new()
        {
            Name = $"Affected node {nodeName}",
            Exact = true
        });

        await Expect(affectedNodeLink).ToBeVisibleAsync();
        await affectedNodeLink.ClickAsync();

        var detail = new NodeDetailPage(Page);

        await Expect(detail.Heading(nodeName)).ToBeVisibleAsync();

        var card = dashboard.NodeCard(nodeName);

        await Expect(
            card.GetByText("Active Fault", new() { Exact = true })
        ).ToBeVisibleAsync();
        await detail.BackToNodes.ClickAsync();
        await dashboard.Heading.WaitForAsync();

        await Page.GetByLabel("Status", new() { Exact = true })
            .SelectOptionAsync("Resolved");

        await Page.GetByLabel("Node", new() { Exact = true })
            .SelectOptionAsync(new SelectOptionValue { Label = nodeName });

        incidentHistory = Page.Locator("details")
            .Filter(new() { HasText = nodeName });

        await Expect(incidentHistory).ToBeVisibleAsync(
            new() { Timeout = 15_000 });

        await incidentHistory.Locator("summary").ClickAsync();

        incidentCard = incidentHistory.Locator("article")
            .Filter(new()
            {
                Has = Page.GetByRole(AriaRole.Heading, new()
                {
                    Name = incident.Title,
                    Exact = true
                })
            });

        await Expect(incidentCard).ToBeVisibleAsync(
            new() { Timeout = 15_000 });

        await Expect(incidentCard.GetByText("Resolved", new()
        {
            Exact = true
        })).ToBeVisibleAsync();
    }

    private sealed record NodeSnapshot(
        Guid Id,
        string Name,
        string Status,
        string ActiveFault) : INodeWithStatus;

    private sealed record IncidentSnapshot(
        Guid Id,
        Guid ComputeNodeId,
        string Status,
        string Title,
        DateTime CreatedAt) : IIncidentIdentifiable;
}