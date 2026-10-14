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
    public async Task AttachNode_ShouldShowErrorWhenNetworkIsInactive()
    {
        // Arrange: Create a node and network
        var dashboard = new DashboardPage(Page);
        await dashboard.OpenAsync();

        var nodeName = await NodeTestHelper.CreateNodeAsync(Page);
        await NodeTestHelper.WaitForNodeStatusAsync(
            Page,
            nodeName,
            "Available",
            UiTestSettings.ProvisioningTimeoutMs);

        using var api = ApiTestHelper.CreateApiClient();

        var networkName = TestData.NetworkName();
        var createdNetwork = await ApiTestHelper.CreateNetworkAsync<NetworkSnapshot>(
            api,
            new
            {
                name = networkName,
                cidr = "10.0.0.0/24",
                isActive = false // Inactive network
            });

        await Page.ReloadAsync();
        await dashboard.Heading.WaitForAsync();

        // Act: Try to attach node to inactive network
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

            // Select the node from dropdown
            var nodeSelect = networkCard.GetByLabel("Select node", new() { Exact = true });
            if (await nodeSelect.IsVisibleAsync())
            {
                await nodeSelect.SelectOptionAsync(new SelectOptionValue { Label = nodeName });

                var confirmButton = networkCard.GetByRole(AriaRole.Button, new()
                {
                    Name = "Confirm",
                    Exact = true
                });

                await confirmButton.ClickAsync();
            }
        }

        // Assert: Error message should be displayed
        await Expect(
                networkCard.GetByText("Cannot attach", new() { Exact = false }))
            .ToBeVisibleAsync(
                new() { Timeout = TestConstants.DefaultWaitTimeoutMs });
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
