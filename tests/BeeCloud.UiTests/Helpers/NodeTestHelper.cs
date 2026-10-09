using BeeCloud.UiTests.Configuration;
using BeeCloud.UiTests.Pages;
using Microsoft.Playwright;
using NUnit.Framework;

namespace BeeCloud.UiTests.Helpers;

public static class NodeTestHelper
{
    public static async Task<string> CreateNodeAsync(
        IPage page,
        string? nodeName = null,
        string gpuModel = "NVIDIA A100",
        string gpuCount = "1")
    {
        var dashboard = new DashboardPage(page);
        var name = nodeName ?? TestData.NodeName();

        await dashboard.FillNodeFormAsync(name, gpuModel, gpuCount);
        await dashboard.CreateNodeButton.ClickAsync();

        var nodeLink = dashboard.NodeLink(name);
        await nodeLink.WaitForAsync(new() { State = WaitForSelectorState.Visible });

        var card = dashboard.NodeCard(name);
        await WaitForStatusAsync(card, "Provisioning", 30_000);

        return name;
    }

    public static async Task WaitForNodeStatusAsync(
        IPage page,
        string nodeName,
        string expectedStatus,
        float timeoutMs = 60_000)
    {
        var card = new DashboardPage(page).NodeCard(nodeName);
        await WaitForStatusAsync(card, expectedStatus, timeoutMs);
    }

    public static async Task WaitForStatusAsync(
        ILocator card,
        string status,
        float timeoutMs = 60_000)
    {
        await card.GetByLabel(
                $"Node status: {status}",
                new() { Exact = true })
            .WaitForAsync(new()
            {
                State = WaitForSelectorState.Visible,
                Timeout = timeoutMs
            });
    }

    public static async Task CreateAndWaitUntilAvailableAsync(
        IPage page,
        string? nodeName = null)
    {
        var name = await CreateNodeAsync(page, nodeName);
        await WaitForNodeStatusAsync(
            page,
            name,
            "Available",
            UiTestSettings.ProvisioningTimeoutMs);
    }
}
