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
[Category("Workflows")]
public sealed class CrossFeatureWorkflowTests : UiTestBase
{
    [Test]
    public async Task NodeLifecycle_ShouldPersistAcrossDetailNavigationAndReload()
    {
        var dashboard = new DashboardPage(Page);
        await dashboard.OpenAsync();

        var nodeName = await NodeTestHelper.CreateNodeAsync(Page);
        await NodeTestHelper.WaitForNodeStatusAsync(
            Page, nodeName, "Available", UiTestSettings.ProvisioningTimeoutMs);

        await dashboard.NodeLink(nodeName).ClickAsync();
        var details = new NodeDetailPage(Page);
        await details.Heading(nodeName).WaitForAsync();
        await details.HealthHeading.WaitForAsync();

        await details.BackToNodes.ClickAsync();
        await dashboard.Heading.WaitForAsync();

        var card = dashboard.NodeCard(nodeName);
        await card.GetByRole(
            AriaRole.Button,
            new() { Name = "Start", Exact = true }).ClickAsync();
        await NodeTestHelper.WaitForNodeStatusAsync(Page, nodeName, "Running", 45_000);

        await Page.ReloadAsync();
        await dashboard.Heading.WaitForAsync();
        await NodeTestHelper.WaitForNodeStatusAsync(Page, nodeName, "Running", 45_000);

        card = dashboard.NodeCard(nodeName);
        await card.GetByRole(
            AriaRole.Button,
            new() { Name = "Stop", Exact = true }).ClickAsync();
        await NodeTestHelper.WaitForNodeStatusAsync(Page, nodeName, "Stopped", 45_000);
    }

    [Test]
    public async Task NetworkWorkflow_ShouldAttachDetachAndPreserveOtherNetworkCards()
    {
        var dashboard = new DashboardPage(Page);
        await dashboard.OpenAsync();

        var nodeName = await NodeTestHelper.CreateNodeAsync(Page);
        await NodeTestHelper.WaitForNodeStatusAsync(
            Page, nodeName, "Available", UiTestSettings.ProvisioningTimeoutMs);

        var networkOne = await NetworkTestHelper.CreateNetworkAsync(
            Page, description: "Primary workflow network");
        var networkTwo = await NetworkTestHelper.CreateNetworkAsync(
            Page, description: "Control network");

        var primaryCard = dashboard.NetworkCard(networkOne);
        var controlCard = dashboard.NetworkCard(networkTwo);

        await primaryCard.GetByRole(
            AriaRole.Button,
            new() { Name = "Show Attachments", Exact = true }).ClickAsync();

        var selector = primaryCard.Locator("select");
        await selector.SelectOptionAsync(new SelectOptionValue { Label = nodeName });
        await primaryCard.GetByRole(
            AriaRole.Button,
            new() { Name = "Attach Node", Exact = true }).ClickAsync();

        // Verify the durable result of attaching the node.
        await Expect(
            primaryCard.GetByText(nodeName, new() { Exact = true })
        ).ToBeVisibleAsync(new() { Timeout = 10_000 });

        await Expect(
            primaryCard.GetByText("1 /", new() { Exact = false })
        ).ToBeVisibleAsync();

        await Page.ReloadAsync();
        await dashboard.Heading.WaitForAsync();

        primaryCard = dashboard.NetworkCard(networkOne);
        controlCard = dashboard.NetworkCard(networkTwo);
        await Expect(primaryCard).ToBeVisibleAsync();
        await Expect(controlCard).ToBeVisibleAsync();

        await primaryCard.GetByRole(
            AriaRole.Button,
            new() { Name = "Show Attachments", Exact = true }).ClickAsync();
        await Expect(primaryCard.GetByText(nodeName, new() { Exact = true }))
            .ToBeVisibleAsync();

        await primaryCard.GetByRole(
            AriaRole.Button,
            new() { Name = "Detach", Exact = true }).ClickAsync();
        await Expect(
            primaryCard.GetByText("No nodes attached.", new() { Exact = true })
        ).ToBeVisibleAsync(new() { Timeout = 10_000 });
    }
}