using System.Net.Http.Json;
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
[Category("Pagination")]
public sealed class IncidentPaginationWorkflowTests : UiTestBase
{
    private const int IncidentPageSize = 12;
    private const int MaxVerificationAttempts = 10;
    private const int VerificationDelayMs = 500;

    [Test]
    [Timeout(180_000)]
    public async Task IncidentPagination_ShouldNavigateAndResetWhenFiltersChange()
    {
        // Arrange: Create test node via API
        var dashboard = new DashboardPage(Page);
        await dashboard.OpenAsync();

        using var api = ApiTestHelper.CreateApiClient();
        var nodeName = TestData.NodeName();

        var createdNode = await ApiTestHelper.CreateNodeAsync<NodeSnapshot>(
            api,
            new
            {
                name = nodeName,
                gpuModel = "NVIDIA A100",
                gpuCount = 1
            });

        var nodeId = createdNode.Id;

        // Arrange: Create 13 incidents to span 2 pages (12 per page)
        var expectedIncidentCount = IncidentPageSize + 1;
        for (var index = 1; index <= expectedIncidentCount; index++)
        {
            await ApiTestHelper.CreateIncidentAsync<IncidentSnapshot>(
                api,
                new
                {
                    computeNodeId = nodeId,
                    severity = "Medium",
                    title = $"pagination-e2e-{Guid.NewGuid():N}-{index}",
                    description = "Created by IncidentPaginationWorkflowTests"
                });
        }

        // Arrange: Wait for all incidents to be persisted
        await ApiTestHelper.WaitForCountAsync<IncidentSnapshot>(
            async () =>
            {
                using var response = await api.GetAsync(
                    $"api/v1/incidents/search?computeNodeId={nodeId}&page=1&pageSize=100");
                response.EnsureSuccessStatusCode();
                var page = await response.Content.ReadFromJsonAsync<IncidentPageSnapshot>();
                return page?.TotalCount ?? 0;
            },
            expectedIncidentCount,
            TimeSpan.FromSeconds(30),
            "incidents");

        await Page.ReloadAsync();
        await dashboard.Heading.WaitForAsync();

        var pagination = Page.GetByRole(AriaRole.Navigation, new()
        {
            Name = "Incident pages",
            Exact = true
        });
        var previousButton = pagination.GetByRole(AriaRole.Button, new()
        {
            Name = "Previous",
            Exact = true
        });
        var nextButton = pagination.GetByRole(AriaRole.Button, new()
        {
            Name = "Next",
            Exact = true
        });

        // Scope the test to its own node so unrelated historical incidents cannot
        // change the expected number of pages.
        await Page.GetByLabel("Node", new() { Exact = true })
            .SelectOptionAsync(new SelectOptionValue { Label = nodeName });

        await Expect(pagination).ToBeVisibleAsync(new() { Timeout = 15_000 });
        await Expect(pagination).ToContainTextAsync("Page 1 of 2", new() { Timeout = 15_000 });
        await Expect(previousButton).ToBeDisabledAsync();
        await Expect(nextButton).ToBeEnabledAsync();

        await nextButton.ClickAsync();

        await Expect(pagination).ToContainTextAsync("Page 2 of 2", new() { Timeout = 15_000 });
        await Expect(previousButton).ToBeEnabledAsync();
        await Expect(nextButton).ToBeDisabledAsync();

        // The status filter produces no matches for these newly created Open incidents.
        // Changing a filter must return the user to page one instead of leaving them on page two.
        await Page.GetByLabel("Status", new() { Exact = true }).SelectOptionAsync("Resolved");

        await Expect(Page.GetByRole(AriaRole.Heading, new()
        {
            Name = "No matching incidents",
            Exact = true
        })).ToBeVisibleAsync();

        await Expect(pagination).ToHaveCountAsync(0);

        var filters = Page.GetByTestId("incident-filters");
        await filters.GetByRole(AriaRole.Button, new()
        {
            Name = "Clear filters",
            Exact = true
        }).ClickAsync();

        await Expect(pagination).ToBeVisibleAsync(new() { Timeout = 15_000 });
        await Expect(pagination).ToContainTextAsync("Page 1 of", new() { Timeout = 15_000 });
        await Expect(previousButton).ToBeDisabledAsync();
    }

    private sealed record NodeSnapshot(Guid Id, string Name) : INodeIdentifiable;

    private sealed record IncidentPageSnapshot(
        List<IncidentSnapshot> Items,
        int Page,
        int PageSize,
        int TotalCount);

    private sealed record IncidentSnapshot(
        Guid Id,
        Guid ComputeNodeId,
        string Status,
        string Title,
        DateTime CreatedAt) : IIncidentIdentifiable;
}
