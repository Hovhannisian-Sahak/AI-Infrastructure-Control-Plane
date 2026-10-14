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
[Category("Health")]
[Category("Remediation")]
public sealed class HealthFailureRecoveryWorkflowTests : UiTestBase
{
    private const int MaxRemediationTimeoutSeconds = 120;

    [Test]
    [Timeout(300_000)]
    public async Task GpuFault_ShouldCreateIncidentAndRecoverAfterFaultIsCleared()
    {
        // Arrange: Create and provision a node
        var dashboard = new DashboardPage(Page);
        await dashboard.OpenAsync();

        var nodeName = await NodeTestHelper.CreateNodeAsync(Page);
        await NodeTestHelper.WaitForNodeStatusAsync(
            Page,
            nodeName,
            "Available",
            UiTestSettings.ProvisioningTimeoutMs);

        using var api = ApiTestHelper.CreateApiClient();

        var nodeId = await ApiTestHelper.FindNodeIdByNameAsync(api, nodeName);
        Assert.That(
            nodeId,
            Is.Not.EqualTo(Guid.Empty),
            $"Created node '{nodeName}' must exist in the API");

        // Start the node before simulating a GPU fault
        await ApiTestHelper.StartNodeAsync(api, nodeId);
        await NodeTestHelper.WaitForNodeStatusAsync(
            Page,
            nodeName,
            "Running",
            UiTestSettings.LifecycleTimeoutMs);

        // Act: Inject the GPU-overheat fault
        await ApiTestHelper.SimulateFaultAsync(api, nodeId, new { fault = 1 });

        // Assert: Wait for incident creation
        var incident = await ApiTestHelper.WaitForIncidentAsync<IncidentSnapshot>(
            api,
            nodeId,
            inc => inc.Title.Contains("GPU", StringComparison.OrdinalIgnoreCase) ||
                   inc.Title.Contains("health check", StringComparison.OrdinalIgnoreCase),
            TimeSpan.FromSeconds(MaxRemediationTimeoutSeconds));

        Assert.That(
            incident,
            Is.Not.Null,
            "A health-related incident should be created after the GPU fault");

        // Assert: Wait for automatic remediation to Available status
        var recoveredNode = await ApiTestHelper.WaitForNodeStatusAsync<NodeSnapshot>(
            api,
            nodeId,
            "Available",
            TimeSpan.FromSeconds(MaxRemediationTimeoutSeconds));

        Assert.That(
            recoveredNode.Status,
            Is.EqualTo("Available"),
            "Node should return to Available after automatic remediation");

        // Clear the simulated fault
        await ApiTestHelper.ClearFaultAsync(api, nodeId);

        // Verify fault is cleared
        var clearedNode = await ApiTestHelper.WaitForNodeStatusAsync<NodeSnapshot>(
            api,
            nodeId,
            "Available",
            TimeSpan.FromSeconds(MaxRemediationTimeoutSeconds));

        Assert.That(
            clearedNode.ActiveFault,
            Is.EqualTo("None"),
            "Active fault should be 'None' after clearing");

        // Assert: Verify UI reflects the cleared state
        await Page.ReloadAsync();
        await dashboard.Heading.WaitForAsync();

        var nodeCard = dashboard.NodeCard(nodeName);
        await Expect(nodeCard).ToContainTextAsync("Available");
        await Expect(
                nodeCard.GetByText("None", new() { Exact = true }))
            .ToBeVisibleAsync();

        // Assert: Verify incident history is visible in the UI
        await VerifyIncidentHistoryAsync(nodeName, incident!.Title);
    }

    private async Task VerifyIncidentHistoryAsync(string nodeName, string incidentTitle)
    {
        var nodeIncidentHistory = Page
            .Locator("details")
            .Filter(new() { HasText = nodeName });

        if (await nodeIncidentHistory.CountAsync() == 0)
        {
            Assert.Fail(
                $"Could not find incident history for node '{nodeName}'. " +
                $"Expected incident: '{incidentTitle}'");
        }

        var summary = nodeIncidentHistory
            .GetByText("Incident history", new() { Exact = false });

        if (await summary.CountAsync() > 0)
        {
            await summary.First.ClickAsync();
        }

        await Expect(
                nodeIncidentHistory.GetByRole(AriaRole.Heading, new()
                {
                    Name = incidentTitle,
                    Exact = true
                }))
            .ToBeVisibleAsync(new() { Timeout = 15_000 });
    }

    private sealed record NodeSnapshot(
        Guid Id,
        string Name,
        string Status,
        string ActiveFault) : INodeWithStatus;

    private sealed record IncidentSnapshot(
        Guid Id,
        Guid ComputeNodeId,
        string Status,
        string Title,
        DateTime CreatedAt) : IIncidentIdentifiable;
}