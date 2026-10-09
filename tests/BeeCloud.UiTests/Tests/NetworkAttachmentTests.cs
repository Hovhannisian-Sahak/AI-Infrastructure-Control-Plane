using BeeCloud.UiTests.Configuration;
using BeeCloud.UiTests.Fixtures;
using BeeCloud.UiTests.Helpers;
using BeeCloud.UiTests.Pages;
using Microsoft.Playwright;
using NUnit.Framework;

namespace BeeCloud.UiTests.Tests;

[TestFixture]
[Category("E2E")]
[Category("Networks")]
public sealed class NetworkAttachmentTests : UiTestBase
{
    [Test]
    public async Task AvailableNode_CanBeAttachedAndDetachedFromNetwork()
    {
        var dashboard = new DashboardPage(Page);
        await dashboard.OpenAsync();

        var nodeName = await NodeTestHelper.CreateNodeAsync(Page);
        await NodeTestHelper.WaitForNodeStatusAsync(
            Page,
            nodeName,
            "Available",
            UiTestSettings.ProvisioningTimeoutMs);

        var networkName = await NetworkTestHelper.CreateNetworkAsync(
            Page,
            description: "Created by network attachment E2E test");

        var networkCard = dashboard.NetworkCard(networkName);
        await networkCard.WaitForAsync();

        await networkCard.GetByRole(
            AriaRole.Button,
            new() { Name = "Show Attachments", Exact = true }).ClickAsync();

        var nodeSelector = networkCard.Locator("select");
        await nodeSelector.WaitForAsync();
        await nodeSelector.SelectOptionAsync(new SelectOptionValue { Label = nodeName });

        await networkCard.GetByRole(
            AriaRole.Button,
            new() { Name = "Attach Node", Exact = true }).ClickAsync();

        await Expect(networkCard.GetByRole(
            AriaRole.Status).GetByText(
            "Node attached successfully.", new() { Exact = true }))
            .ToBeVisibleAsync();

        await Expect(networkCard.GetByText(nodeName, new() { Exact = true }))
            .ToBeVisibleAsync();

        await Expect(networkCard.GetByText("1 /", new() { Exact = false }))
            .ToBeVisibleAsync();

        await networkCard.GetByRole(
            AriaRole.Button,
            new() { Name = "Detach", Exact = true }).ClickAsync();

        await Expect(networkCard.GetByText("No nodes attached.", new() { Exact = true }))
            .ToBeVisibleAsync();

        // Detaching must make the node eligible for attachment again.
        await Expect(nodeSelector.Locator("option", new() { HasText = nodeName }))
            .ToHaveCountAsync(1);
    }

    [Test]
    public async Task InactiveNetwork_ShouldNotExposeAttachControls()
    {
        var dashboard = new DashboardPage(Page);
        await dashboard.OpenAsync();

        await NodeTestHelper.CreateAndWaitUntilAvailableAsync(Page);
        var networkName = await NetworkTestHelper.CreateNetworkAsync(Page);
        var card = dashboard.NetworkCard(networkName);

        await card.GetByRole(
            AriaRole.Button,
            new() { Name = "Deactivate", Exact = true }).ClickAsync();

        await Expect(card.GetByText("Inactive", new() { Exact = true }))
            .ToBeVisibleAsync();

        await Expect(card.GetByText(
            "Activate this network before attaching nodes.",
            new() { Exact = true })).ToBeVisibleAsync();

        await Expect(card.GetByRole(
            AriaRole.Button,
            new() { Name = "Attach Node", Exact = true })).ToHaveCountAsync(0);
    }
}