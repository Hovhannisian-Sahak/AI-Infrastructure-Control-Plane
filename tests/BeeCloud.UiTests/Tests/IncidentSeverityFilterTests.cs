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

        var lowTitle = $"Low severity incident {Guid.NewGuid():N}";
        var mediumTitle = $"Medium severity incident {Guid.NewGuid():N}";
        var highTitle = $"High severity incident {Guid.NewGuid():N}";

        await ApiTestHelper.CreateIncidentAsync<IncidentSnapshot>(
            api,
            new
            {
                computeNodeId = createdNode.Id,
                severity = "Low",
                title = lowTitle,
                description = "Low severity test incident"
            });

        await ApiTestHelper.CreateIncidentAsync<IncidentSnapshot>(
            api,
            new
            {
                computeNodeId = createdNode.Id,
                severity = "Medium",
                title = mediumTitle,
                description = "Medium severity test incident"
            });

        await ApiTestHelper.CreateIncidentAsync<IncidentSnapshot>(
            api,
            new
            {
                computeNodeId = createdNode.Id,
                severity = "High",
                title = highTitle,
                description = "High severity test incident"
            });

        // Act: Reload page and filter by node to scope test data
        await Page.ReloadAsync();
        await dashboard.Heading.WaitForAsync();

        await Page.GetByLabel("Node", new() { Exact = true })
            .SelectOptionAsync(new SelectOptionValue { Label = nodeName });

        await Expect(Page.GetByRole(AriaRole.Heading, new()
        {
            Name = lowTitle,
            Exact = true
        })).ToBeVisibleAsync(new() { Timeout = TestConstants.DefaultWaitTimeoutMs });
        await Expect(Page.GetByRole(AriaRole.Heading, new()
        {
            Name = mediumTitle,
            Exact = true
        })).ToBeVisibleAsync(new() { Timeout = TestConstants.DefaultWaitTimeoutMs });

        // Assert: Filter by High severity
        await Page.GetByLabel("Severity", new() { Exact = true })
            .SelectOptionAsync("High");

        await Expect(Page.GetByRole(AriaRole.Heading, new()
        {
            Name = highTitle,
            Exact = true
        })).ToBeVisibleAsync(new() { Timeout = TestConstants.DefaultWaitTimeoutMs });

        // Assert: Low and Medium should not be visible
        await Expect(
                Page.GetByRole(AriaRole.Heading, new()
                {
                    Name = lowTitle,
                    Exact = true
                }))
            .ToHaveCountAsync(0);
        await Expect(
                Page.GetByRole(AriaRole.Heading, new()
                {
                    Name = mediumTitle,
                    Exact = true
                }))
            .ToHaveCountAsync(0);

        // Assert: Filter by Medium severity
        await Page.GetByLabel("Severity", new() { Exact = true })
            .SelectOptionAsync("Medium");

        await Expect(Page.GetByRole(AriaRole.Heading, new()
        {
            Name = mediumTitle,
            Exact = true
        })).ToBeVisibleAsync(new() { Timeout = TestConstants.DefaultWaitTimeoutMs });
        await Expect(
                Page.GetByRole(AriaRole.Heading, new()
                {
                    Name = highTitle,
                    Exact = true
                }))
            .ToHaveCountAsync(0);

        // Assert: Clear filters
        var filters = Page.GetByTestId("incident-filters");
        await filters.GetByRole(AriaRole.Button, new()
        {
            Name = "Clear filters",
            Exact = true
        }).ClickAsync();

        await Expect(Page.GetByRole(AriaRole.Heading, new()
        {
            Name = lowTitle,
            Exact = true
        })).ToBeVisibleAsync(new() { Timeout = TestConstants.DefaultWaitTimeoutMs });
        await Expect(Page.GetByRole(AriaRole.Heading, new()
        {
            Name = mediumTitle,
            Exact = true
        })).ToBeVisibleAsync(new() { Timeout = TestConstants.DefaultWaitTimeoutMs });
        await Expect(Page.GetByRole(AriaRole.Heading, new()
        {
            Name = highTitle,
            Exact = true
        })).ToBeVisibleAsync(new() { Timeout = TestConstants.DefaultWaitTimeoutMs });
    }

    private sealed record NodeSnapshot(Guid Id, string Name) : INodeIdentifiable;

    private sealed record IncidentSnapshot(
        Guid Id,
        Guid ComputeNodeId,
        string Status,
        string Title,
        DateTime CreatedAt) : IIncidentIdentifiable;
}
