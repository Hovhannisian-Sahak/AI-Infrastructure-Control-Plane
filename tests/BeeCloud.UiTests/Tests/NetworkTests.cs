using BeeCloud.UiTests.Fixtures;
using BeeCloud.UiTests.Helpers;
using BeeCloud.UiTests.Pages;
using Microsoft.Playwright;
using NUnit.Framework;

namespace BeeCloud.UiTests.Tests;

[TestFixture]
public class NetworkTests : UiTestBase
{
    [Test]
    public async Task CreateNetwork_ShouldShowNetworkCardAndDescription()
    {
        var dashboard = new DashboardPage(Page);
        await dashboard.OpenAsync();

        var name = TestData.NetworkName();
        const string description = "Created by automated UI test";

        await NetworkTestHelper.CreateNetworkAsync(Page, name, description);

        var card = dashboard.NetworkCard(name);
        await card.GetByText(description, new() { Exact = true }).WaitForAsync();
        await card.GetByText("Active", new() { Exact = true }).WaitForAsync();
    }

    [Test]
    public async Task Network_ShouldToggleActiveState()
    {
        var dashboard = new DashboardPage(Page);
        await dashboard.OpenAsync();

        var name = await NetworkTestHelper.CreateNetworkAsync(Page);
        var card = dashboard.NetworkCard(name);

        await card.GetByRole(
            AriaRole.Button,
            new() { Name = "Deactivate", Exact = true }).ClickAsync();

        await card.GetByText("Inactive", new() { Exact = true })
            .WaitForAsync(new() { State = WaitForSelectorState.Visible });

        await card.GetByRole(
            AriaRole.Button,
            new() { Name = "Activate", Exact = true }).ClickAsync();

        await card.GetByText("Active", new() { Exact = true })
            .WaitForAsync(new() { State = WaitForSelectorState.Visible });
    }

    [Test]
    public async Task Network_ShouldExpandAndCollapseAttachments()
    {
        var dashboard = new DashboardPage(Page);
        await dashboard.OpenAsync();

        var name = await NetworkTestHelper.CreateNetworkAsync(Page);
        var card = dashboard.NetworkCard(name);

        await card.GetByRole(
            AriaRole.Button,
            new() { Name = "Show Attachments", Exact = true }).ClickAsync();

        await card.GetByRole(
            AriaRole.Heading,
            new() { Name = "Attached Nodes", Exact = true }).WaitForAsync();

        await card.GetByRole(
            AriaRole.Button,
            new() { Name = "Hide Attachments", Exact = true }).ClickAsync();
    }

    [Test]
    public async Task ConfirmDeleteNetwork_ShouldRemoveNetworkCard()
    {
        var dashboard = new DashboardPage(Page);
        await dashboard.OpenAsync();

        var name = await NetworkTestHelper.CreateNetworkAsync(Page);
        var card = dashboard.NetworkCard(name);

        string? dialogType = null;
        string? dialogMessage = null;
        Page.Dialog += async (_, dialog) =>
        {
            dialogType = dialog.Type;
            dialogMessage = dialog.Message;
            await dialog.AcceptAsync();
        };

        await card.GetByRole(
            AriaRole.Button,
            new() { Name = "Delete", Exact = true }).ClickAsync();

        Assert.That(dialogType, Is.EqualTo("confirm"));
        Assert.That(dialogMessage, Does.Contain(name));

        await card.WaitForAsync(new() { State = WaitForSelectorState.Detached, Timeout = 30_000 });
    }

    [Test]
    public async Task NetworkName_ShouldBeRequired()
    {
        var dashboard = new DashboardPage(Page);
        await dashboard.OpenAsync();

        var nameInput = Page.GetByLabel("Network Name");
        var createButton = Page.GetByRole(
            AriaRole.Button,
            new() { Name = "Create Network", Exact = true });

        Assert.That(await createButton.IsDisabledAsync(), Is.True);

        await nameInput.FillAsync("   ");
        Assert.That(await createButton.IsDisabledAsync(), Is.True);
    }
}
