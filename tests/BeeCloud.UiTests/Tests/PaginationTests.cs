using BeeCloud.UiTests.Fixtures;
using BeeCloud.UiTests.Helpers;
using BeeCloud.UiTests.Pages;
using Microsoft.Playwright;
using NUnit.Framework;
using System.Text.RegularExpressions;

namespace BeeCloud.UiTests.Tests;

[TestFixture]
[Category("E2E")]
[Category("Pagination")]
public sealed class PaginationTests : UiTestBase
{
    [Test]
    public async Task DashboardNodePagination_ShouldNavigateForwardAndBack()
    {
        var dashboard = new DashboardPage(Page);
        await dashboard.OpenAsync();

        // Create enough records to force the dashboard's eight-node page size.
        // New nodes reset the list to page one, so each new card can be observed.
        var createdNames = new List<string>();
        for (var i = 0; i < 9; i++)
        {
            var name = await NodeTestHelper.CreateNodeAsync(
                Page,
                $"e2e-page-node-{Guid.NewGuid():N}"[..20]);
            createdNames.Add(name);
        }

        var pagination = Page.GetByRole(
            AriaRole.Navigation,
            new() { Name = "Compute node pages", Exact = true });

        await pagination.WaitForAsync();
        await Expect(pagination.GetByText(
            new Regex(@"Page 1 of \d+"))).ToBeVisibleAsync();
        await Expect(pagination.GetByRole(
            AriaRole.Button,
            new() { Name = "Previous", Exact = true })).ToBeDisabledAsync();

        await pagination.GetByRole(
            AriaRole.Button,
            new() { Name = "Next", Exact = true }).ClickAsync();

        await Expect(pagination.GetByText(
            new Regex(@"Page 2 of \d+"))).ToBeVisibleAsync();
        await Expect(pagination.GetByRole(
            AriaRole.Button,
            new() { Name = "Previous", Exact = true })).ToBeEnabledAsync();

        await pagination.GetByRole(
            AriaRole.Button,
            new() { Name = "Previous", Exact = true }).ClickAsync();

        await Expect(pagination.GetByText(
            new Regex(@"Page 1 of \d+"))).ToBeVisibleAsync();
    }

    [Test]
    public async Task NetworkPagination_ShouldNavigateForwardAndBack()
    {
        var dashboard = new DashboardPage(Page);
        await dashboard.OpenAsync();

        for (var i = 0; i < 7; i++)
        {
            await NetworkTestHelper.CreateNetworkAsync(
                Page,
                $"e2e-page-net-{Guid.NewGuid():N}"[..20]);
        }

        var pagination = Page.GetByRole(
            AriaRole.Navigation,
            new() { Name = "Network pages", Exact = true });

        await pagination.WaitForAsync();
        await Expect(pagination.GetByText(
            new Regex(@"Page 1 of \d+"))).ToBeVisibleAsync();
        await Expect(pagination.GetByRole(
            AriaRole.Button,
            new() { Name = "Previous", Exact = true })).ToBeDisabledAsync();

        await pagination.GetByRole(
            AriaRole.Button,
            new() { Name = "Next", Exact = true }).ClickAsync();

        await Expect(pagination.GetByText(
            new Regex(@"Page 2 of \d+"))).ToBeVisibleAsync();

        await pagination.GetByRole(
            AriaRole.Button,
            new() { Name = "Previous", Exact = true }).ClickAsync();

        await Expect(pagination.GetByText(
            new Regex(@"Page 1 of \d+"))).ToBeVisibleAsync();
    }
}