using BeeCloud.UiTests.Fixtures;
using BeeCloud.UiTests.Pages;
using Microsoft.Playwright;
using NUnit.Framework;

namespace BeeCloud.UiTests.Tests;

[TestFixture]
public class IncidentTests : UiTestBase
{
    [Test]
    public async Task IncidentSection_ShouldExposeSeverityStatusAndNodeFilters()
    {
        var dashboard = new DashboardPage(Page);
        await dashboard.OpenAsync();

        await dashboard.IncidentsHeading.WaitForAsync();

        await Page.GetByLabel("Severity", new() { Exact = true }).WaitForAsync();
        await Page.GetByLabel("Status", new() { Exact = true }).WaitForAsync();
        await Page.GetByLabel("Node", new() { Exact = true }).WaitForAsync();
    }

    [Test]
    public async Task IncidentFilters_ShouldBeChangeableAndClearable()
    {
        var dashboard = new DashboardPage(Page);
        await dashboard.OpenAsync();

        var severity = Page.GetByLabel("Severity", new() { Exact = true });
        var status = Page.GetByLabel("Status", new() { Exact = true });

        await severity.SelectOptionAsync("Critical");
        await status.SelectOptionAsync("Open");
        
        var filters = Page.GetByTestId("incident-filters");

        var clearFilters = filters.GetByRole(
            AriaRole.Button,
            new() { Name = "Clear filters", Exact = true }
        );
        await clearFilters.WaitForAsync();
        await clearFilters.First.ClickAsync();

        Assert.That(await severity.InputValueAsync(), Is.EqualTo("All"));
        Assert.That(await status.InputValueAsync(), Is.EqualTo("All"));
    }
}
