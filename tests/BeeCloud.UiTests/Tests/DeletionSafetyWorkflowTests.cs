using BeeCloud.UiTests.Fixtures;
using BeeCloud.UiTests.Helpers;
using BeeCloud.UiTests.Pages;
using Microsoft.Playwright;
using NUnit.Framework;
using static Microsoft.Playwright.Assertions;

namespace BeeCloud.UiTests.Tests;

[TestFixture]
[Category("E2E")]
[Category("DeletionSafety")]
public sealed class DeletionSafetyWorkflowTests : UiTestBase
{
    [Test]
    public async Task CancelNetworkDelete_ShouldPreserveNetworkAndAttachment()
    {
        var dashboard = new DashboardPage(Page);
        await dashboard.OpenAsync();

        var nodeName = await NodeTestHelper.CreateNodeAsync(Page);
        await NodeTestHelper.WaitForNodeStatusAsync(Page, nodeName, "Available", 90_000);

        var networkName = await NetworkTestHelper.CreateNetworkAsync(Page);
        var card = dashboard.NetworkCard(networkName);

        await card.GetByRole(AriaRole.Button, new() { Name = "Show Attachments", Exact = true }).ClickAsync();
        await card.Locator("select").SelectOptionAsync(new SelectOptionValue { Label = nodeName });
        await card.GetByRole(AriaRole.Button, new() { Name = "Attach Node", Exact = true }).ClickAsync();
        await Expect(card.GetByText(nodeName, new() { Exact = true })).ToBeVisibleAsync();

        var dismissed = false;
        Page.Dialog += async (_, dialog) =>
        {
            Assert.That(dialog.Type, Is.EqualTo("confirm"));
            Assert.That(dialog.Message, Does.Contain(networkName));
            await dialog.DismissAsync();
            dismissed = true;
        };

        await card.GetByRole(AriaRole.Button, new() { Name = "Delete", Exact = true }).ClickAsync();

        Assert.That(dismissed, Is.True);
        await Expect(dashboard.NetworkCard(networkName)).ToBeVisibleAsync();
        await Expect(card.GetByText(nodeName, new() { Exact = true })).ToBeVisibleAsync();
        await Expect(card.GetByText("1 /", new() { Exact = false })).ToBeVisibleAsync();
    }

    [Test]
    public async Task NetworkWithAttachment_ShouldRemainWhenDeleteIsRejected()
    {
        var dashboard = new DashboardPage(Page);
        await dashboard.OpenAsync();

        var nodeName = await NodeTestHelper.CreateNodeAsync(Page);
        await NodeTestHelper.WaitForNodeStatusAsync(Page, nodeName, "Available", 90_000);

        var networkName = await NetworkTestHelper.CreateNetworkAsync(Page);
        var card = dashboard.NetworkCard(networkName);

        await card.GetByRole(AriaRole.Button, new() { Name = "Show Attachments", Exact = true }).ClickAsync();
        await card.Locator("select").SelectOptionAsync(new SelectOptionValue { Label = nodeName });
        await card.GetByRole(AriaRole.Button, new() { Name = "Attach Node", Exact = true }).ClickAsync();
        await Expect(card.GetByText(nodeName, new() { Exact = true })).ToBeVisibleAsync();

        Page.Dialog += async (_, dialog) => await dialog.AcceptAsync();
        await card.GetByRole(AriaRole.Button, new() { Name = "Delete", Exact = true }).ClickAsync();

        await Expect(card.GetByRole(AriaRole.Alert)).ToContainTextAsync(
            "The network cannot be deleted while nodes are attached.");
        await Expect(dashboard.NetworkCard(networkName)).ToBeVisibleAsync();
        await Expect(card.GetByText(nodeName, new() { Exact = true })).ToBeVisibleAsync();
        await Expect(card.GetByText("1 /", new() { Exact = false })).ToBeVisibleAsync();
        await Expect(dashboard.NodeLink(nodeName)).ToBeVisibleAsync();
        await Expect(dashboard.NodeCard(nodeName).GetByLabel(
            "Node status: Available", new() { Exact = true })).ToBeVisibleAsync();
    }
}
