
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
[Category("Reliability")]
public sealed class HealthFailureRecoveryWorkflowTests : UiTestBase
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    [Test]
    [Timeout(240_000)]
    public async Task GpuFault_ShouldCreateIncidentAndRecoverAfterFaultIsCleared()
    {
        var dashboard = new DashboardPage(Page);
        await dashboard.OpenAsync();

        var nodeName = await NodeTestHelper.CreateNodeAsync(Page);

        await NodeTestHelper.WaitForNodeStatusAsync(
            Page,
            nodeName,
            "Available",
            UiTestSettings.ProvisioningTimeoutMs);

        var card = dashboard.NodeCard(nodeName);

        await card.GetByRole(
            AriaRole.Button,
            new() { Name = "Start", Exact = true }
        ).ClickAsync();

        await NodeTestHelper.WaitForNodeStatusAsync(
            Page,
            nodeName,
            "Running",
            UiTestSettings.LifecycleTimeoutMs);

        using var api = new HttpClient
        {
            BaseAddress = new Uri(
                UiTestSettings.ApiBaseUrl.TrimEnd('/') + "/")
        };

        var nodeId = await FindNodeIdAsync(api, nodeName);

        // NodeFault.GpuOverheat has numeric enum value 1.
        using var faultResponse = await api.PostAsJsonAsync(
            $"api/v1/nodes/{nodeId}/simulate/fault",
            new { fault = 1 });

        Assert.That(
            (int)faultResponse.StatusCode,
            Is.EqualTo(200),
            await faultResponse.Content.ReadAsStringAsync());

        await WaitForApiStatusAsync(
            api, nodeId, "Quarantined", 120_000);

        const string incidentTitle = "Compute node health check failed";

        var incidentHeading = Page.GetByRole(
            AriaRole.Heading,
            new() { Name = incidentTitle, Exact = true });

        await Expect(incidentHeading).ToBeVisibleAsync(
            new() { Timeout = 90_000 });

        // Clear the simulated fault so remediation can recover the node.
        using var clearFaultResponse = await api.DeleteAsync(
            $"api/v1/nodes/{nodeId}/simulate/fault");

        Assert.That(
            (int)clearFaultResponse.StatusCode,
            Is.EqualTo(200),
            await clearFaultResponse.Content.ReadAsStringAsync());

        await WaitForApiStatusAsync(
            api, nodeId, "Available", 120_000);

        await Page.ReloadAsync();
        await dashboard.Heading.WaitForAsync();

        card = dashboard.NodeCard(nodeName);

        await NodeTestHelper.WaitForNodeStatusAsync(
            Page,
            nodeName,
            "Available",
            UiTestSettings.LifecycleTimeoutMs);

        await Expect(card.GetByText(
            "None",
            new() { Exact = true }
        )).ToBeVisibleAsync();

        // The incident should remain visible in the incident history.
        await Expect(incidentHeading).ToBeVisibleAsync(
            new() { Timeout = 30_000 });
    }

    private static async Task<Guid> FindNodeIdAsync(
        HttpClient api,
        string nodeName)
    {
        using var response = await api.GetAsync("api/v1/nodes");
        response.EnsureSuccessStatusCode();

        var nodes =
            await response.Content.ReadFromJsonAsync<List<NodeSnapshot>>(
                JsonOptions);

        var node = nodes?.SingleOrDefault(item =>
            string.Equals(item.Name, nodeName, StringComparison.Ordinal));

        Assert.That(node, Is.Not.Null,
            $"Node '{nodeName}' was not returned by the API.");

        return node!.Id;
    }

    private static async Task WaitForApiStatusAsync(
        HttpClient api,
        Guid nodeId,
        string expectedStatus,
        int timeoutMs)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        string? actualStatus = null;

        while (DateTime.UtcNow < deadline)
        {
            using var response = await api.GetAsync("api/v1/nodes");
            response.EnsureSuccessStatusCode();

            var nodes =
                await response.Content.ReadFromJsonAsync<List<NodeSnapshot>>(
                    JsonOptions);

            actualStatus = nodes?
                .SingleOrDefault(node => node.Id == nodeId)?
                .Status;

            if (actualStatus == expectedStatus)
            {
                return;
            }

            await Task.Delay(1_000);
        }

        Assert.Fail(
            $"Node '{nodeId}' did not reach '{expectedStatus}' " +
            $"within {timeoutMs} ms. Last status: '{actualStatus}'.");
    }

    private sealed record NodeSnapshot(
        Guid Id,
        string Name,
        string Status);
}
