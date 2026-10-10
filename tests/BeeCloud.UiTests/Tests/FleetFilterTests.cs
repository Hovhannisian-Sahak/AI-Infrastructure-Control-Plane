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
[Category("FleetFilters")]
public sealed class FleetFilterTests : UiTestBase
{
    [Test]
    public async Task SearchNodes_ShouldShowOnlyMatchesAndAllowClearingFilters()
    {
        var dashboard = new DashboardPage(Page);
        await dashboard.OpenAsync();

        var runId = Guid.NewGuid().ToString("N")[..8];
        var prefix = $"e2e-filter-{runId}";
        var firstNode = await NodeTestHelper.CreateNodeAsync(
            Page, $"{prefix}-a", "NVIDIA A100");
        await NodeTestHelper.WaitForNodeStatusAsync(
            Page, firstNode, "Available", UiTestSettings.ProvisioningTimeoutMs);

        var secondNode = await NodeTestHelper.CreateNodeAsync(
            Page, $"{prefix}-b", "NVIDIA H100");
        await NodeTestHelper.WaitForNodeStatusAsync(
            Page, secondNode, "Available", UiTestSettings.ProvisioningTimeoutMs);

        await dashboard.SearchNodesInput.FillAsync(prefix);
        await Expect(dashboard.NodeLink(firstNode)).ToBeVisibleAsync();
        await Expect(dashboard.NodeLink(secondNode)).ToBeVisibleAsync();
        await Expect(Page.GetByText("2 matching nodes", new() { Exact = true }))
            .ToBeVisibleAsync();

        await dashboard.SearchNodesInput.FillAsync(secondNode);
        await Expect(dashboard.NodeLink(secondNode)).ToBeVisibleAsync();
        await Expect(dashboard.NodeLink(firstNode)).ToHaveCountAsync(0);
        await Expect(Page.GetByText("1 matching node", new() { Exact = true }))
            .ToBeVisibleAsync();

        await dashboard.SearchNodesInput.FillAsync("node-name-that-does-not-exist");
        await Expect(dashboard.NoMatchingNodesHeading).ToBeVisibleAsync();
        await Expect(dashboard.NodeLink(firstNode)).ToHaveCountAsync(0);
        await Expect(dashboard.NodeLink(secondNode)).ToHaveCountAsync(0);

        await dashboard.ClearFiltersButton.ClickAsync();
        await Expect(dashboard.SearchNodesInput).ToHaveValueAsync("");

        // Reapply the unique prefix so assertions don't depend on how many
        // unrelated nodes already exist in the shared test environment.
        await dashboard.SearchNodesInput.FillAsync(prefix);
        await Expect(dashboard.NodeLink(firstNode)).ToBeVisibleAsync();
        await Expect(dashboard.NodeLink(secondNode)).ToBeVisibleAsync();
        await Expect(Page.GetByText("2 matching nodes", new() { Exact = true }))
            .ToBeVisibleAsync();
    }

    [Test]
    public async Task StatusAndGpuModelFilters_ShouldShowOnlyNodesMatchingSelectedCriteria()
    {
        var dashboard = new DashboardPage(Page);
        await dashboard.OpenAsync();

        var runId = Guid.NewGuid().ToString("N")[..8];
        var prefix = $"e2e-filter-{runId}";
        var runningNode = await NodeTestHelper.CreateNodeAsync(
            Page, $"{prefix}-running", "NVIDIA A100");
        await NodeTestHelper.WaitForNodeStatusAsync(
            Page, runningNode, "Available", UiTestSettings.ProvisioningTimeoutMs);

        await dashboard.NodeCard(runningNode).GetByRole(
            AriaRole.Button, new() { Name = "Start", Exact = true }).ClickAsync();
        await NodeTestHelper.WaitForNodeStatusAsync(
            Page, runningNode, "Running", UiTestSettings.LifecycleTimeoutMs);

        var availableNode = await NodeTestHelper.CreateNodeAsync(
            Page, $"{prefix}-available", "NVIDIA H100");
        await NodeTestHelper.WaitForNodeStatusAsync(
            Page, availableNode, "Available", UiTestSettings.ProvisioningTimeoutMs);

        await dashboard.SearchNodesInput.FillAsync(prefix);
        await dashboard.NodeStatusFilter.SelectOptionAsync("Running");

        await Expect(dashboard.NodeLink(runningNode)).ToBeVisibleAsync();
        await Expect(dashboard.NodeLink(availableNode)).ToHaveCountAsync(0);
        await Expect(Page.GetByText("1 matching node", new() { Exact = true }))
            .ToBeVisibleAsync();

        await dashboard.NodeStatusFilter.SelectOptionAsync("all");
        await dashboard.GpuModelFilter.SelectOptionAsync("NVIDIA H100");

        await Expect(dashboard.NodeLink(availableNode)).ToBeVisibleAsync();
        await Expect(dashboard.NodeLink(runningNode)).ToHaveCountAsync(0);
        await Expect(Page.GetByText("1 matching node", new() { Exact = true }))
            .ToBeVisibleAsync();
    }

    [Test]
    public async Task ChangingFiltersFromSecondPage_ShouldResetPaginationForFilteredResults()
    {
        var dashboard = new DashboardPage(Page);
        await dashboard.OpenAsync();

        var runId = Guid.NewGuid().ToString("N")[..8];
        var prefix = $"e2e-filter-{runId}";
        string? h100NodeName = null;

        for (var index = 1; index <= 9; index++)
        {
            var gpuModel = index == 9 ? "NVIDIA H100" : "NVIDIA A100";
            var name = $"{prefix}-{index}";

            await NodeTestHelper.CreateNodeAsync(Page, name, gpuModel);
            if (index == 9)
            {
                h100NodeName = name;
            }
        }

        await dashboard.SearchNodesInput.FillAsync(prefix);

        var pagination = Page.GetByRole(
            AriaRole.Navigation,
            new() { Name = "Compute node pages", Exact = true });

        await Expect(pagination.GetByText("Page 1 of 2", new() { Exact = true }))
            .ToBeVisibleAsync();

        await pagination.GetByRole(
            AriaRole.Button,
            new() { Name = "Next", Exact = true }).ClickAsync();

        await Expect(pagination.GetByText("Page 2 of 2", new() { Exact = true }))
            .ToBeVisibleAsync();

        await dashboard.GpuModelFilter.SelectOptionAsync("NVIDIA H100");

        await Expect(dashboard.NodeLink(h100NodeName!)).ToBeVisibleAsync();
        await Expect(Page.GetByText("1 matching node", new() { Exact = true }))
            .ToBeVisibleAsync();
        await Expect(pagination).ToHaveCountAsync(0);
        await Expect(dashboard.NoMatchingNodesHeading).ToHaveCountAsync(0);
    }
}
