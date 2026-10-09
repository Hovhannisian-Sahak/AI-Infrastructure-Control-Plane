using BeeCloud.UiTests.Configuration;
using BeeCloud.UiTests.Fixtures;
using BeeCloud.UiTests.Pages;
using Microsoft.Playwright;
using NUnit.Framework;

namespace BeeCloud.UiTests.Tests;

[TestFixture]
public class HealthOverviewTests : UiTestBase
{
    [Test]
    public async Task HealthNavigation_ShouldOpenHealthOverview()
    {
        var dashboard = new DashboardPage(Page);
        await dashboard.OpenAsync();

        var health = new HealthPage(Page);
        await health.NavigationLink.ClickAsync();

        await health.Heading.WaitForAsync();
        await health.NodeHealthHeading.WaitForAsync();

        Assert.That(Page.Url, Does.EndWith("/health"));
    }

    [Test]
    public async Task HealthOverview_ShouldReturnToNodesUsingNavigation()
    {
        var dashboard = new DashboardPage(Page);
        await dashboard.OpenAsync();

        var health = new HealthPage(Page);
        await health.NavigationLink.ClickAsync();
        await health.Heading.WaitForAsync();

        await Page.GetByRole(
            AriaRole.Link,
            new() { Name = "Nodes", Exact = true }).ClickAsync();

        await dashboard.Heading.WaitForAsync();
        Assert.That(Page.Url.TrimEnd('/'), Is.EqualTo(UiTestSettings.BaseUrl.TrimEnd('/')));
    }
}
