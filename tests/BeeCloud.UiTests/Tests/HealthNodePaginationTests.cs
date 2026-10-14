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
[Category("Health")]
[Category("Pagination")]
public sealed class HealthNodePaginationTests : UiTestBase
{
    private const int HealthPageSize = 10;

    [Test]
    [Timeout(300_000)]
    public async Task HealthNodeList_ShouldPaginateWith10PerPage()
    {
        // Arrange: Create 11 nodes to span 2 pages
        var dashboard = new DashboardPage(Page);
        await dashboard.OpenAsync();

        var nodeNames = new List<string>();
        for (var i = 0; i < HealthPageSize + 1; i++)
        {
            var nodeName = await NodeTestHelper.CreateNodeAsync(Page);
            await NodeTestHelper.WaitForNodeStatusAsync(
                Page,
                nodeName,
                "Available",
                UiTestSettings.ProvisioningTimeoutMs);
            nodeNames.Add(nodeName);
        }

        // Act: Navigate to Health Overview
        var health = new HealthPage(Page);
        await health.NavigationLink.ClickAsync();
        await health.Heading.WaitForAsync();
        await health.NodeHealthHeading.WaitForAsync();

        // Assert: Verify pagination controls exist
        var pagination = Page.GetByRole(AriaRole.Navigation, new()
        {
            Name = "Health node pages",
            Exact = true
        });

        await Expect(pagination).ToBeVisibleAsync(
            new() { Timeout = TestConstants.DefaultWaitTimeoutMs });

        await Expect(pagination).ToContainTextAsync("Page 1 of");

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

        await Expect(previousButton).ToBeDisabledAsync();
        await Expect(nextButton).ToBeEnabledAsync();

        // Verify we have 10 rows on page 1
        var healthTable = Page.GetByRole(AriaRole.Table, new()
        {
            Name = "Node health status",
            Exact = true
        });

        var tableRows = healthTable.Locator("tbody tr");
        await Expect(tableRows).ToHaveCountAsync(HealthPageSize);

        // Navigate to page 2
        await nextButton.ClickAsync();

        await Expect(pagination).ToContainTextAsync("Page 2 of");
        await Expect(previousButton).ToBeEnabledAsync();

        // Verify we have at least 1 row on page 2 (11th node)
        await Expect(tableRows).ToHaveCountAsync(1, new() { Timeout = TestConstants.DefaultWaitTimeoutMs });

        // Navigate back to page 1
        await previousButton.ClickAsync();

        await Expect(pagination).ToContainTextAsync("Page 1 of");
        await Expect(previousButton).ToBeDisabledAsync();
        await Expect(nextButton).ToBeEnabledAsync();

        // Verify back to 10 rows
        await Expect(tableRows).ToHaveCountAsync(HealthPageSize);
    }
}
