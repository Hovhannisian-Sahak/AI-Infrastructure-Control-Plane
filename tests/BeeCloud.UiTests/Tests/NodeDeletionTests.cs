using BeeCloud.UiTests.Fixtures;
using BeeCloud.UiTests.Helpers;
using BeeCloud.UiTests.Pages;
using Microsoft.Playwright;
using NUnit.Framework;

namespace BeeCloud.UiTests.Tests;

[TestFixture]
public class NodeDeletionTests : UiTestBase
{
    [Test]
    public async Task ConfirmDelete_ShouldRemoveNodeFromDashboard()
    {
        var dashboard = new DashboardPage(Page);
        await dashboard.OpenAsync();

        var name = await NodeTestHelper.CreateNodeAsync(Page);
        await NodeTestHelper.WaitForNodeStatusAsync(Page, name, "Available", 90_000);

        string? dialogType = null;
        string? dialogMessage = null;
        Page.Dialog += async (_, dialog) =>
        {
            dialogType = dialog.Type;
            dialogMessage = dialog.Message;
            await dialog.AcceptAsync();
        };

        await dashboard.NodeCard(name)
            .GetByRole(AriaRole.Button, new() { Name = "Delete", Exact = true })
            .ClickAsync();

        Assert.That(dialogType, Is.EqualTo("confirm"));
        Assert.That(dialogMessage, Does.Contain(name));

        await dashboard.NodeLink(name).WaitForAsync(
            new() { State = WaitForSelectorState.Detached, Timeout = 30_000 });
    }

    [Test]
    public async Task CancelDelete_ShouldKeepNodeOnDashboard()
    {
        var dashboard = new DashboardPage(Page);
        await dashboard.OpenAsync();

        var name = await NodeTestHelper.CreateNodeAsync(Page);
        await NodeTestHelper.WaitForNodeStatusAsync(Page, name, "Available", 90_000);

        var dialogDismissed = false;
        Page.Dialog += async (_, dialog) =>
        {
            await dialog.DismissAsync();
            dialogDismissed = true;
        };

        await dashboard.NodeCard(name)
            .GetByRole(AriaRole.Button, new() { Name = "Delete", Exact = true })
            .ClickAsync();

        Assert.That(dialogDismissed, Is.True);

        await dashboard.NodeLink(name).WaitForAsync(
            new() { State = WaitForSelectorState.Visible });
    }
}
