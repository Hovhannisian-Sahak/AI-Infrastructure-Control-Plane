using BeeCloud.UiTests.Fixtures;
using BeeCloud.UiTests.Helpers;
using BeeCloud.UiTests.Pages;
using Microsoft.Playwright;
using NUnit.Framework;

namespace BeeCloud.UiTests.Tests;

[TestFixture]
[Category("Pagination")]
public sealed class PaginationWorkflowTests : UiTestBase
{
    [Test]
    public async Task NodePagination_ShouldNavigateForwardAndBackwardAndDisableBoundaryActions()
    {
        await NavigateToDashboardAsync();

        var dashboard = new DashboardPage(Page);
        var prefix = $"pagination-{Guid.NewGuid():N}";
        var firstNode = $"{prefix}-1";
        var secondNode = $"{prefix}-2";

        await NodeTestHelper.CreateNodeAsync(Page, firstNode);
        await NodeTestHelper.CreateNodeAsync(Page, secondNode);
        for (var i = 3; i <= 9; i++)
        {
            await NodeTestHelper.CreateNodeAsync(Page, $"{prefix}-{i}");
        }

        var pagination = Page.GetByRole(AriaRole.Navigation, new()
        {
            Name = "Compute node pages",
            Exact = true
        });
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

        await Expect(pagination).ToContainTextAsync("Page 1 of 2");
        await Expect(previousButton).ToBeDisabledAsync();
        await Expect(nextButton).ToBeEnabledAsync();

        await nextButton.ClickAsync();

        await Expect(pagination).ToContainTextAsync("Page 2 of 2");
        await Expect(previousButton).ToBeEnabledAsync();
        await Expect(nextButton).ToBeDisabledAsync();

        await previousButton.ClickAsync();

        await Expect(pagination).ToContainTextAsync("Page 1 of 2");
        await Expect(previousButton).ToBeDisabledAsync();
        await Expect(nextButton).ToBeEnabledAsync();
    }

    [Test]
    public async Task SearchingFromSecondPage_ShouldResetPaginationAndShowMatchingNode()
    {
        await NavigateToDashboardAsync();

        var dashboard = new DashboardPage(Page);
        var prefix = $"pagination-search-{Guid.NewGuid():N}";
        var matchingNodeName = $"{prefix}-match";

        await NodeTestHelper.CreateNodeAsync(Page, matchingNodeName);
        for (var i = 1; i <= 8; i++)
        {
            await NodeTestHelper.CreateNodeAsync(Page, $"{prefix}-{i}");
        }

        var pagination = Page.GetByRole(AriaRole.Navigation, new()
        {
            Name = "Compute node pages",
            Exact = true
        });
        var nextButton = pagination.GetByRole(AriaRole.Button, new()
        {
            Name = "Next",
            Exact = true
        });

        await Expect(pagination).ToContainTextAsync("Page 1 of 2");
        await nextButton.ClickAsync();
        await Expect(pagination).ToContainTextAsync("Page 2 of 2");

        await dashboard.SearchNodesInput.FillAsync(matchingNodeName);

        await Expect(dashboard.NodeLink(matchingNodeName)).ToBeVisibleAsync();
        await Expect(pagination).ToContainTextAsync("Page 1 of 1");
        await Expect(pagination).ToBeHiddenAsync();
        await Expect(Page.GetByText("1 matching node", new() { Exact = true }))
            .ToBeVisibleAsync();
    }
}
