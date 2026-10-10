using System.Net.Http.Json;
using System.Text.Json;
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
[Category("NodeLifecycle")]
public sealed class NodeLifecycleWorkflowTests : UiTestBase
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    [Test]
    [Timeout(180_000)]
    public async Task NodeLifecycle_ShouldExposeOnlyValidActionsAndRejectStartingRunningNode()
    {
        var dashboard = new DashboardPage(Page);
        await dashboard.OpenAsync();

        var nodeName = await NodeTestHelper.CreateNodeAsync(Page);
        await NodeTestHelper.WaitForNodeStatusAsync(
            Page, nodeName, "Available", UiTestSettings.ProvisioningTimeoutMs);

        var card = dashboard.NodeCard(nodeName);
        await ExpectActionVisibleAsync(card, "Start");
        await ExpectActionHiddenAsync(card, "Stop");
        await ExpectActionHiddenAsync(card, "Restart");

        await card.GetByRole(
            AriaRole.Button, new() { Name = "Start", Exact = true }).ClickAsync();
        await NodeTestHelper.WaitForNodeStatusAsync(
            Page, nodeName, "Running", UiTestSettings.LifecycleTimeoutMs);

        await ExpectActionHiddenAsync(card, "Start");
        await ExpectActionVisibleAsync(card, "Stop");
        await ExpectActionVisibleAsync(card, "Restart");

        using var api = new HttpClient
        {
            BaseAddress = new Uri(UiTestSettings.ApiBaseUrl.TrimEnd('/') + "/")
        };

        var nodeId = await FindNodeIdAsync(api, nodeName);
        using var invalidStart = await api.PostAsync(
            $"api/v1/nodes/{nodeId}/start", content: null);

        Assert.That(
            (int)invalidStart.StatusCode,
            Is.EqualTo(409),
            $"Starting a Running node should return HTTP 409, but returned {(int)invalidStart.StatusCode}: {await invalidStart.Content.ReadAsStringAsync()}");

        await NodeTestHelper.WaitForNodeStatusAsync(
            Page, nodeName, "Running", UiTestSettings.LifecycleTimeoutMs);
        await ExpectActionHiddenAsync(card, "Start");
        await ExpectActionVisibleAsync(card, "Stop");
        await ExpectActionVisibleAsync(card, "Restart");

        await card.GetByRole(
            AriaRole.Button, new() { Name = "Stop", Exact = true }).ClickAsync();
        await NodeTestHelper.WaitForNodeStatusAsync(
            Page, nodeName, "Stopped", UiTestSettings.LifecycleTimeoutMs);

        await ExpectActionVisibleAsync(card, "Start");
        await ExpectActionHiddenAsync(card, "Stop");
        await ExpectActionHiddenAsync(card, "Restart");
    }

    [Test]
    [Timeout(120_000)]
    public async Task StoppedNode_ShouldStartAgainAndReturnToRunning()
    {
        var dashboard = new DashboardPage(Page);
        await dashboard.OpenAsync();

        var nodeName = await NodeTestHelper.CreateNodeAsync(Page);
        await NodeTestHelper.WaitForNodeStatusAsync(
            Page, nodeName, "Available", UiTestSettings.ProvisioningTimeoutMs);

        var card = dashboard.NodeCard(nodeName);
        await card.GetByRole(
            AriaRole.Button, new() { Name = "Start", Exact = true }).ClickAsync();
        await NodeTestHelper.WaitForNodeStatusAsync(
            Page, nodeName, "Running", UiTestSettings.LifecycleTimeoutMs);

        await card.GetByRole(
            AriaRole.Button, new() { Name = "Stop", Exact = true }).ClickAsync();
        await NodeTestHelper.WaitForNodeStatusAsync(
            Page, nodeName, "Stopped", UiTestSettings.LifecycleTimeoutMs);

        await card.GetByRole(
            AriaRole.Button, new() { Name = "Start", Exact = true }).ClickAsync();
        await NodeTestHelper.WaitForNodeStatusAsync(
            Page, nodeName, "Running", UiTestSettings.LifecycleTimeoutMs);

        await ExpectActionHiddenAsync(card, "Start");
        await ExpectActionVisibleAsync(card, "Stop");
        await ExpectActionVisibleAsync(card, "Restart");
    }

    private static async Task<Guid> FindNodeIdAsync(HttpClient api, string nodeName)
    {
        using var response = await api.GetAsync("api/v1/nodes");
        response.EnsureSuccessStatusCode();

        var nodes = await response.Content.ReadFromJsonAsync<List<NodeSnapshot>>(JsonOptions);
        var node = nodes?.SingleOrDefault(item =>
            string.Equals(item.Name, nodeName, StringComparison.Ordinal));

        Assert.That(node, Is.Not.Null, $"Node '{nodeName}' was not returned by the API.");
        return node!.Id;
    }

    private static Task ExpectActionVisibleAsync(ILocator card, string action) =>
        Expect(card.GetByRole(
            AriaRole.Button, new() { Name = action, Exact = true })).ToBeVisibleAsync();

    private static Task ExpectActionHiddenAsync(ILocator card, string action) =>
        Expect(card.GetByRole(
            AriaRole.Button, new() { Name = action, Exact = true })).ToHaveCountAsync(0);

    private sealed record NodeSnapshot(Guid Id, string Name, string Status);
}
