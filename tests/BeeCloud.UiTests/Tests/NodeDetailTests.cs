using BeeCloud.UiTests.Fixtures;
using BeeCloud.UiTests.Helpers;
using BeeCloud.UiTests.Pages;
using Microsoft.Playwright;
using NUnit.Framework;

namespace BeeCloud.UiTests.Tests;

[TestFixture]
public class NodeDetailTests : UiTestBase
{
    [Test]
    public async Task NodeCardLink_ShouldOpenNodeDetailAndReturnToDashboard()
    {
        var dashboard = new DashboardPage(Page);
        await dashboard.OpenAsync();

        var nodeName = await NodeTestHelper.CreateNodeAsync(Page);
        await NodeTestHelper.WaitForNodeStatusAsync(Page, nodeName, "Available", 90_000);

        await dashboard.NodeLink(nodeName).ClickAsync();

        var detail = new NodeDetailPage(Page);
        await detail.Heading(nodeName).WaitForAsync();
        await detail.HealthHeading.WaitForAsync();
        await detail.ActiveFaultLabel.WaitForAsync();

        await detail.BackToNodes.ClickAsync();
        await dashboard.Heading.WaitForAsync();
        await dashboard.NodeLink(nodeName).WaitForAsync();
    }

    [Test]
    public async Task UnknownNodeRoute_ShouldShowNotFoundState()
    {
        var response = await Page.GotoAsync(
            $"{BeeCloud.UiTests.Configuration.UiTestSettings.BaseUrl}/nodes/{Guid.NewGuid()}");

        Assert.That(response, Is.Not.Null);
        await Page.GetByRole(
            AriaRole.Heading,
            new() { Name = "Node not found", Exact = true })
            .WaitForAsync();
    }
}
