using BeeCloud.UiTests.Fixtures;
using BeeCloud.UiTests.Helpers;
using BeeCloud.UiTests.Pages;
using Microsoft.Playwright;
using NUnit.Framework;

namespace BeeCloud.UiTests.Tests;

[TestFixture]
public class NodeActionTests : UiTestBase
{
    [Test]
    public async Task AvailableNode_ShouldStartRestartAndStop()
    {
        var dashboard = new DashboardPage(Page);
        await dashboard.OpenAsync();

        var nodeName = await NodeTestHelper.CreateNodeAsync(Page);
        await NodeTestHelper.WaitForNodeStatusAsync(Page, nodeName, "Available", 90_000);

        var card = dashboard.NodeCard(nodeName);

        await card.GetByRole(AriaRole.Button, new() { Name = "Start", Exact = true }).ClickAsync();
        await NodeTestHelper.WaitForNodeStatusAsync(Page, nodeName, "Running", 45_000);

        await card.GetByRole(AriaRole.Button, new() { Name = "Restart", Exact = true }).ClickAsync();
        await NodeTestHelper.WaitForNodeStatusAsync(Page, nodeName, "Running", 45_000);

        await card.GetByRole(AriaRole.Button, new() { Name = "Stop", Exact = true }).ClickAsync();
        await NodeTestHelper.WaitForNodeStatusAsync(Page, nodeName, "Stopped", 45_000);
    }
}
