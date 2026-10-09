using BeeCloud.UiTests.Fixtures;
using BeeCloud.UiTests.Pages;
using Microsoft.Playwright;
using NUnit.Framework;

namespace BeeCloud.UiTests.Tests;

[TestFixture]
public class DashboardTests : UiTestBase
{
    [Test]
    public async Task Dashboard_ShouldRenderPrimarySections()
    {
        var dashboard = new DashboardPage(Page);
        await dashboard.OpenAsync();

        await dashboard.CreateNodeHeading.WaitForAsync();
        await dashboard.NetworksHeading.WaitForAsync();
        await dashboard.IncidentsHeading.WaitForAsync();

        Assert.That(await dashboard.Heading.IsVisibleAsync(), Is.True);
    }

    [Test]
    public async Task RefreshButton_ShouldBeAvailable()
    {
        var dashboard = new DashboardPage(Page);
        await dashboard.OpenAsync();

        await Page.GetByRole(
            AriaRole.Button,
            new() { Name = "Refresh", Exact = true })
            .WaitForAsync(new() { State = WaitForSelectorState.Visible });
    }
}
