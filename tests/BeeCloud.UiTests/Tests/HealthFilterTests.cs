using BeeCloud.UiTests.Fixtures;
using BeeCloud.UiTests.Helpers;
using BeeCloud.UiTests.Pages;
using Microsoft.Playwright;
using NUnit.Framework;
using static Microsoft.Playwright.Assertions;

namespace BeeCloud.UiTests.Tests;

[TestFixture]
[Category("E2E")]
[Category("Health")]
public sealed class HealthFilterTests : UiTestBase
{
    [Test]
    public async Task HealthOverview_ShouldChangeTimeRangeAndHealthFilter()
    {
        var dashboard = new DashboardPage(Page);
        await dashboard.OpenAsync();

        var health = new HealthPage(Page);
        await health.NavigationLink.ClickAsync();
        await health.Heading.WaitForAsync();
        await health.NodeHealthHeading.WaitForAsync();

        var oneHour = health.TimeRange("1 hour");
        var sevenDays = health.TimeRange("7 days");

        await oneHour.ClickAsync();
        await Expect(oneHour).ToHaveAttributeAsync("aria-pressed", "true");

        await sevenDays.ClickAsync();
        await Expect(sevenDays).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(oneHour).ToHaveAttributeAsync("aria-pressed", "false");

        var noDataFilter = Page.GetByRole(
            AriaRole.Button,
            new() { Name = "No Data", Exact = true });
        await noDataFilter.ClickAsync();
        await Expect(noDataFilter).ToHaveAttributeAsync("aria-pressed", "true");

        await Expect(Page.GetByRole(
            AriaRole.Heading,
            new() { Name = "Node Comparison", Exact = true })).ToBeVisibleAsync();
    }

    [Test]
    public async Task HealthOverview_ShouldAllowSelectingAndDeselectingNodeForComparison()
    {
        var dashboard = new DashboardPage(Page);
        await dashboard.OpenAsync();

        var nodeName = await NodeTestHelper.CreateNodeAsync(Page);
        await NodeTestHelper.WaitForNodeStatusAsync(Page, nodeName, "Available", 90_000);

        var health = new HealthPage(Page);
        await health.NavigationLink.ClickAsync();
        await health.Heading.WaitForAsync();

        var compareCheckbox = Page.GetByRole(
            AriaRole.Checkbox,
            new() { Name = $"Compare {nodeName}", Exact = true });
        await compareCheckbox.WaitForAsync();

        await compareCheckbox.CheckAsync();
        await Expect(Page.GetByRole(
            AriaRole.Table,
            new() { Name = "Node comparison", Exact = true }))
            .ToContainTextAsync(nodeName);

        await compareCheckbox.UncheckAsync();
        await Expect(Page.GetByText(
            "Select nodes in the health table to compare their latest readings.",
            new() { Exact = true })).ToBeVisibleAsync();
    }
}