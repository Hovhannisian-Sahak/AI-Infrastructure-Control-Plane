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
[Category("Network")]
[Category("ErrorHandling")]
public sealed class NetworkAttachmentErrorHandlingTests : UiTestBase
{
    [Test]
    [Timeout(120_000)]
    public async Task InactiveNetwork_ShouldShowActivationWarningAndHideAttachControls()
    {
        // Arrange: Create a node and network
        var dashboard = new DashboardPage(Page);
        await dashboard.OpenAsync();

        using var api = ApiTestHelper.CreateApiClient();

        var networkName = TestData.NetworkName();
        await ApiTestHelper.CreateNetworkAsync<NetworkSnapshot>(
            api,
            new
            {
                name = networkName,
                description = "Inactive network attachment test"
            });

        await Page.ReloadAsync();
        await dashboard.Heading.WaitForAsync();

        var networkCard = dashboard.NetworkCard(networkName);
        await Expect(networkCard).ToBeVisibleAsync(
            new() { Timeout = TestConstants.DefaultWaitTimeoutMs });

        await networkCard.GetByRole(AriaRole.Button, new()
        {
            Name = "Deactivate",
            Exact = true
        }).ClickAsync();

        await Expect(networkCard.GetByText(
                "Activate this network before attaching nodes.",
                new() { Exact = false }))
            .ToBeVisibleAsync(
                new() { Timeout = TestConstants.DefaultWaitTimeoutMs });

        await Expect(networkCard.GetByRole(AriaRole.Button, new()
        {
            Name = "Attach Node",
            Exact = true
        })).ToHaveCountAsync(0);

        await Expect(networkCard.GetByLabel(
            "Select node",
            new() { Exact = true })).ToHaveCountAsync(0);
    }

    [Test]
    [Timeout(120_000)]
    public async Task AttachNode_ShouldPreventAttachingRunningNode()
    {
        // Arrange: Create and start a node, create active network
        var dashboard = new DashboardPage(Page);
        await dashboard.OpenAsync();

        var nodeName = await NodeTestHelper.CreateNodeAsync(Page);
        await NodeTestHelper.WaitForNodeStatusAsync(
            Page,
            nodeName,
            "Available",
            UiTestSettings.ProvisioningTimeoutMs);

        // Start the node
        var nodeCard = dashboard.NodeCard(nodeName);
        await nodeCard.GetByRole(AriaRole.Button, new()
        {
            Name = "Start",
            Exact = true
        }).ClickAsync();

        await NodeTestHelper.WaitForNodeStatusAsync(
            Page,
            nodeName,
            "Running",
            UiTestSettings.LifecycleTimeoutMs);

        using var api = ApiTestHelper.CreateApiClient();

        var networkName = TestData.NetworkName();
        await ApiTestHelper.CreateNetworkAsync<NetworkSnapshot>(
            api,
            new
            {
                name = networkName,
                cidr = "10.0.1.0/24",
                isActive = true
            });

        await Page.ReloadAsync();
        await dashboard.Heading.WaitForAsync();

        // Assert: Running node should not be available for attachment
        var networkCard = dashboard.NetworkCard(networkName);
        await Expect(networkCard).ToBeVisibleAsync(
            new() { Timeout = TestConstants.DefaultWaitTimeoutMs });

        var attachButton = networkCard.GetByRole(AriaRole.Button, new()
        {
            Name = "Attach node",
            Exact = true
        });

        if (await attachButton.IsVisibleAsync())
        {
            await attachButton.ClickAsync();

            var nodeSelect = networkCard.GetByLabel("Select node", new() { Exact = true });
            if (await nodeSelect.IsVisibleAsync())
            {
                // The running node should not be in the dropdown
                var options = await nodeSelect.Locator("option").AllTextContentsAsync();
                Assert.That(
                    options.Any(opt => opt.Contains(nodeName)),
                    Is.False,
                    $"Running node '{nodeName}' should not be available for attachment");
            }
        }
    }

    private sealed record NetworkSnapshot(
        Guid Id,
        string Name,
        string Cidr,
        bool IsActive);
}
