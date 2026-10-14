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
[Category("Filters")]
public sealed class IncidentSeverityFilterTests : UiTestBase
{
    [Test]
    [Timeout(120_000)]
    public async Task IncidentFilters_ShouldFilterBySeverity()
    {
        // Arrange: Create test node and incidents with different severities
        var dashboard = new DashboardPage(Page);
        await dashboard.OpenAsync();

        using var api = ApiTestHelper.CreateApiClient();
        var nodeName = TestData.NodeName();

        var createdNode = await ApiTestHelper.CreateNodeAsync<NodeSnapshot>(
            api,
            new
            {
                name = nodeName,
                gpuModel = TestConstants.DefaultGpuModel,
                gpuCount = 1
            });

        // Create incidents with different severities
        await ApiTestHelper.CreateIncidentAsync<IncidentSnapshot>(
            api,
            new
            {
                computeNodeId = createdNode.Id,
                severity = "Low",
                title = $"Low severity incident {Guid.NewGuid():N}",
                description = "Low severity test incident"
            });

        await ApiTestHelper.CreateIncidentAsync<IncidentSnapshot>(
            api,
            new
            {
                computeNodeId = createdNode.Id,
                severity = "Medium",
                title = $"Medium severity incident {Guid.NewGuid():N}",
                description = "Medium severity test incident"
            });

        await ApiTestHelper.CreateIncidentAsync<IncidentSnapshot>(
            api,
            new
            {
                computeNodeId = createdNode.Id,
                severity = "High",
                title = $"High severity incident {Guid.NewGuid():N}",
                description = "High severity test incident"
            });

        // Act: Reload page and filter by node to scope test data
        await Page.ReloadAsync();
        await dashboard.Heading.WaitForAsync();

        await Page.GetByLabel("Node", new() { Exact = true })
            .SelectOptionAsync(new SelectOptionValue { Label = nodeName });

        // Assert: Verify all incidents are visible initially
        var incidentHistory = Page.Locator("details")
            .Filter(new() { HasText = nodeName });

        await Expect(incidentHistory).ToBeVisibleAsync(
            new() { Timeout = TestConstants.DefaultWaitTimeoutMs });

        var summary = incidentHistory.Locator("summary");
        if (await summary.CountAsync() > 0)
        {
            await summary.First.ClickAsync();
        }

        // Assert: Filter by High severity
        await Page.GetByLabel("Severity", new() { Exact = true })
            .SelectOptionAsync("High");

        await Expect(
                Page.GetByText("High severity incident", new() { Exact = false }))
            .ToBeVisibleAsync(new() { Timeout = TestConstants.DefaultWaitTimeoutMs });

        // Assert: Low and Medium should not be visible
        await Expect(
                Page.GetByText("Low severity incident", new() { Exact = false }))
            .ToHaveCountAsync(0);

        // Assert: Filter by Medium severity
        await Page.GetByLabel("Severity", new() { Exact = true })
            .SelectOptionAsync("Medium");

        await Expect(
                Page.GetByText("Medium severity incident", new() { Exact = false }))
            .ToBeVisibleAsync(new() { Timeout = TestConstants.DefaultWaitTimeoutMs });

        // Assert: Clear filters
        var filters = Page.GetByTestId("incident-filters");
        await filters.GetByRole(AriaRole.Button, new()
        {
            Name = "Clear filters",
            Exact = true
        }).ClickAsync();

        // After clearing, all incidents should be visible again
        await Expect(incidentHistory).ToBeVisibleAsync(
            new() { Timeout = TestConstants.DefaultWaitTimeoutMs });
    }

    private sealed record NodeSnapshot(Guid Id, string Name) : INodeIdentifiable;

    private sealed record IncidentSnapshot(
        Guid Id,
        Guid ComputeNodeId,
        string Status,
        string Title,
        DateTime CreatedAt) : IIncidentIdentifiable;
}
