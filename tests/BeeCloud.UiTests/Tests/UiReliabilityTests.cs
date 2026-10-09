using BeeCloud.UiTests.Fixtures;
using BeeCloud.UiTests.Pages;
using Microsoft.Playwright;
using NUnit.Framework;

namespace BeeCloud.UiTests.Tests;

[TestFixture]
[Category("E2E")]
[Category("Reliability")]
public sealed class UiReliabilityTests : UiTestBase
{
    [Test]
    public async Task Dashboard_ShouldShowErrorWhenNodeListRequestFails()
    {
        await Page.RouteAsync("**/api/v1/nodes", async route =>
        {
            if (route.Request.Method == "GET")
            {
                await route.FulfillAsync(new RouteFulfillOptions
                {
                    Status = 503,
                    ContentType = "application/json",
                    Body = "{\"title\":\"Service unavailable\"}"
                });
                return;
            }

            await route.ContinueAsync();
        });

        var dashboard = new DashboardPage(Page);
        await dashboard.OpenAsync();

        var alert = Page.GetByRole(AriaRole.Alert);
        await alert.First.WaitForAsync();

        Assert.That(await alert.First.IsVisibleAsync(), Is.True);
    }

    [Test]
    public async Task Dashboard_ShouldAllowManualRefreshAfterInitialLoad()
    {
        var dashboard = new DashboardPage(Page);
        await dashboard.OpenAsync();

        var refresh = Page.GetByRole(
            AriaRole.Button,
            new() { Name = "Refresh", Exact = true });

        await refresh.ClickAsync();

        // The button is intentionally disabled while the refresh is in progress,
        // then returns to its normal label after the request settles.
        await Page.GetByRole(
            AriaRole.Button,
            new() { Name = "Refresh", Exact = true })
            .WaitForAsync(new() { State = WaitForSelectorState.Visible });

        Assert.That(await refresh.IsEnabledAsync(), Is.True);
    }
}