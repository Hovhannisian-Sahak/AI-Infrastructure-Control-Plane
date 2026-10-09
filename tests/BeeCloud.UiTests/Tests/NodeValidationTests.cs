using BeeCloud.UiTests.Fixtures;
using BeeCloud.UiTests.Helpers;
using BeeCloud.UiTests.Pages;
using Microsoft.Playwright;
using NUnit.Framework;

namespace BeeCloud.UiTests.Tests;

[TestFixture]
[Category("E2E")]
[Category("Validation")]
public sealed class NodeValidationTests : UiTestBase
{
    [Test]
    public async Task CreateNode_ShouldDisableSubmitForFractionalGpuCount()
    {
        var dashboard = new DashboardPage(Page);
        await dashboard.OpenAsync();
        await dashboard.NodeNameInput.FillAsync(TestData.NodeName());
        await dashboard.GpuModelInput.FillAsync("NVIDIA A100");
        await dashboard.GpuCountInput.FillAsync("1.5");

        Assert.That(await dashboard.CreateNodeButton.IsDisabledAsync(), Is.True);
    }

    [Test]
    public async Task CreateNode_ShouldTrimNodeNameAndGpuModel()
    {
        var dashboard = new DashboardPage(Page);
        await dashboard.OpenAsync();

        var expectedName = TestData.NodeName();
        await dashboard.FillNodeFormAsync($"  {expectedName}  ", "  NVIDIA A100  ", "1");
        await dashboard.CreateNodeButton.ClickAsync();

        var nodeLink = dashboard.NodeLink(expectedName);
        await nodeLink.WaitForAsync(new() { State = WaitForSelectorState.Visible });

        var card = dashboard.NodeCard(expectedName);
        await NodeTestHelper.WaitForNodeStatusAsync(
            Page, expectedName, "Available", BeeCloud.UiTests.Configuration.UiTestSettings.ProvisioningTimeoutMs);

        Assert.That(
            await card.GetByText("NVIDIA A100", new() { Exact = true }).IsVisibleAsync(),
            Is.True,
            "The submitted GPU model should be trimmed before it is sent to the API.");
    }

    [Test]
    public async Task CreateNode_ShouldExposeDocumentedInputLengthLimits()
    {
        var dashboard = new DashboardPage(Page);
        await dashboard.OpenAsync();

        Assert.That(await dashboard.NodeNameInput.GetAttributeAsync("maxlength"), Is.EqualTo("100"));
        Assert.That(await dashboard.GpuModelInput.GetAttributeAsync("maxlength"), Is.EqualTo("100"));
        Assert.That(await dashboard.GpuCountInput.GetAttributeAsync("min"), Is.EqualTo("1"));
        Assert.That(await dashboard.GpuCountInput.GetAttributeAsync("max"), Is.EqualTo("16"));
        Assert.That(await dashboard.GpuCountInput.GetAttributeAsync("step"), Is.EqualTo("1"));
    }
}