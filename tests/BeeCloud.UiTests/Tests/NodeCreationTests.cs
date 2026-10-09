using BeeCloud.UiTests.Fixtures;
using BeeCloud.UiTests.Helpers;
using BeeCloud.UiTests.Pages;
using Microsoft.Playwright;
using NUnit.Framework;

namespace BeeCloud.UiTests.Tests;

[TestFixture]
public class NodeCreationTests : UiTestBase
{
    [Test]
    public async Task CreateNode_ShouldShowProvisioningAndThenAvailable()
    {
        await new DashboardPage(Page).OpenAsync();

        var name = await NodeTestHelper.CreateNodeAsync(Page);
        await NodeTestHelper.WaitForNodeStatusAsync(
            Page, name, "Available", 90_000);

        Assert.That(await new DashboardPage(Page).NodeLink(name).IsVisibleAsync(), Is.True);
    }

    [Test]
    public async Task CreateNode_ShouldRejectGpuCountOutsideAllowedRange()
    {
        var dashboard = new DashboardPage(Page);
        await dashboard.OpenAsync();

        await dashboard.NodeNameInput.FillAsync(TestData.NodeName());
        await dashboard.GpuModelInput.FillAsync("NVIDIA A100");
        await dashboard.GpuCountInput.FillAsync("17");

        Assert.That(await dashboard.CreateNodeButton.IsDisabledAsync(), Is.True);
    }

    [Test]
    public async Task CreateNode_ShouldRequireNodeNameAndGpuModel()
    {
        var dashboard = new DashboardPage(Page);
        await dashboard.OpenAsync();

        Assert.That(await dashboard.CreateNodeButton.IsDisabledAsync(), Is.True);

        await dashboard.NodeNameInput.FillAsync(TestData.NodeName());
        Assert.That(await dashboard.CreateNodeButton.IsDisabledAsync(), Is.True);

        await dashboard.GpuModelInput.FillAsync("NVIDIA A100");
        Assert.That(await dashboard.CreateNodeButton.IsEnabledAsync(), Is.True);
    }
}
