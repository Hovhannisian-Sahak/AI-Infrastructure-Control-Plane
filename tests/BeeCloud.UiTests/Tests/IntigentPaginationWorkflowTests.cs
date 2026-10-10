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

    [Test]
    [Timeout(180_000)]
    public async Task IncidentPagination_ShouldNavigateAndResetWhenFiltersChange()
    {
        var dashboard = new DashboardPage(Page);
        await dashboard.OpenAsync();

        var nodeName = await NodeTestHelper.CreateNodeAsync(Page);
        await NodeTestHelper.WaitForNodeStatusAsync(
            Page, nodeName, "Available", UiTestSettings.ProvisioningTimeoutMs);

        using var api = new HttpClient
        {
            BaseAddress = new Uri(UiTestSettings.ApiBaseUrl.TrimEnd('/') + "/")
        };

        var nodeId = await FindNodeIdByNameAsync(api, nodeName);
        Assert.That(nodeId, Is.Not.EqualTo(Guid.Empty), "The test node must exist through the API.");

        // Create 13 incidents through the API to exercise the real server-paginated UI
        // without relying on historical incidents in the shared test database.
        for (var index = 1; index <= IncidentPageSize + 1; index++)
        {
            using var response = await api.PostAsJsonAsync(
                "api/v1/incidents",
                new
                {
                    computeNodeId = nodeId,
                    severity = "Medium",
                    title = $"pagination-e2e-{Guid.NewGuid():N}-{index}",
                    description = "Created by IncidentPaginationWorkflowTests."
                });

            Assert.That(
                response.IsSuccessStatusCode,
                Is.True,
                $"Creating test incident {index} failed with HTTP {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
        }

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

        await Expect(pagination).ToBeVisibleAsync();
        await Expect(pagination).ToContainTextAsync("Page 1 of 2");
        await Expect(previousButton).ToBeDisabledAsync();
        await Expect(nextButton).ToBeEnabledAsync();

        await nextButton.ClickAsync();

        await Expect(pagination).ToContainTextAsync("Page 2 of 2");
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

        await Expect(pagination).ToBeVisibleAsync();
        await Expect(pagination).ToContainTextAsync("Page 1 of");
        await Expect(previousButton).ToBeDisabledAsync();
    }

    private static async Task<Guid> FindNodeIdByNameAsync(HttpClient api, string nodeName)
    {
        using var response = await api.GetAsync("api/v1/nodes");
        response.EnsureSuccessStatusCode();

        var nodes = await response.Content.ReadFromJsonAsync<List<NodeSnapshot>>();
        return nodes?.SingleOrDefault(node =>
            string.Equals(node.Name, nodeName, StringComparison.Ordinal))?.Id ?? Guid.Empty;
    }

    private sealed record NodeSnapshot(Guid Id, string Name);
}