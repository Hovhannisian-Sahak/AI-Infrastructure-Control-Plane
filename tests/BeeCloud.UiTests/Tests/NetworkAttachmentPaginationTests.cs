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
[Category("Pagination")]
public sealed class NetworkAttachmentPaginationTests : UiTestBase
{
    private const int AttachmentPageSize = 3;

    [Test]
    [Timeout(300_000)]
    public async Task NetworkAttachments_ShouldPaginateWith3PerPage()
    {
        // Arrange: Create network and 4 available nodes
        var dashboard = new DashboardPage(Page);
        await dashboard.OpenAsync();

        var networkName = await NetworkTestHelper.CreateNetworkAsync(
            Page,
            description: "Network for attachment pagination test");

        var nodeNames = new List<string>();
        for (var i = 0; i < AttachmentPageSize + 1; i++)
        {
            var nodeName = await NodeTestHelper.CreateNodeAsync(Page);
            await NodeTestHelper.WaitForNodeStatusAsync(
                Page,
                nodeName,
                "Available",
                UiTestSettings.ProvisioningTimeoutMs);
            nodeNames.Add(nodeName);
        }

        // Act: Attach all nodes to the network via API for faster setup
        using var api = ApiTestHelper.CreateApiClient();
        var network = await ApiTestHelper.FindNetworkByNameAsync<NetworkSnapshot>(api, networkName);

        Assert.That(network, Is.Not.Null, $"Network '{networkName}' must exist");

        foreach (var nodeName in nodeNames)
        {
            var nodeId = await ApiTestHelper.FindNodeIdByNameAsync(api, nodeName);
            await ApiTestHelper.AttachNodeToNetworkAsync(api, network!.Id, nodeId);
        }

        // Assert: Verify pagination in UI
        await Page.ReloadAsync();
        await dashboard.Heading.WaitForAsync();

        var networkCard = dashboard.NetworkCard(networkName);
        await Expect(networkCard).ToBeVisibleAsync(
            new() { Timeout = TestConstants.DefaultWaitTimeoutMs });

        var showAttachmentsButton = networkCard.GetByRole(AriaRole.Button, new()
        {
            Name = "Show Attachments",
            Exact = true
        });

        await showAttachmentsButton.ClickAsync();

        // Verify pagination controls
        var pagination = networkCard.GetByRole(AriaRole.Navigation, new()
        {
            Name = "Attachment pages",
            Exact = true
        });

        await Expect(pagination).ToBeVisibleAsync(
            new() { Timeout = TestConstants.DefaultWaitTimeoutMs });

        await Expect(pagination).ToContainTextAsync("Page 1 of 2");

        var previousButton = pagination.GetByRole(AriaRole.Button, new()
        {
            Name = "Previous",
            Exact = true
        });

        var nextButton = pagination.GetByRole(AriaRole.Button, new()
        {
            Name = "Next",
            Exact = true
        });

        await Expect(previousButton).ToBeDisabledAsync();
        await Expect(nextButton).ToBeEnabledAsync();

        // Navigate to page 2
        await nextButton.ClickAsync();

        await Expect(pagination).ToContainTextAsync("Page 2 of 2");
        await Expect(previousButton).ToBeEnabledAsync();
        await Expect(nextButton).ToBeDisabledAsync();

        // Verify only 1 attachment on page 2 (4 total, 3 per page)
        var attachmentItems = networkCard.Locator("[data-testid='attachment-item']");
        await Expect(attachmentItems).ToHaveCountAsync(1);

        // Navigate back to page 1
        await previousButton.ClickAsync();

        await Expect(pagination).ToContainTextAsync("Page 1 of 2");
        await Expect(previousButton).ToBeDisabledAsync();
        await Expect(nextButton).ToBeEnabledAsync();
    }

    private sealed record NetworkSnapshot(Guid Id, string Name) : INodeIdentifiable;
}
