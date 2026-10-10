using BeeCloud.UiTests.Fixtures;
using BeeCloud.UiTests.Helpers;
using BeeCloud.UiTests.Pages;
using Microsoft.Playwright;
using NUnit.Framework;
using static Microsoft.Playwright.Assertions;

namespace BeeCloud.UiTests.Tests;

[TestFixture]
[Category("E2E")]
[Category("Networks")]
[Category("Pagination")]
public sealed class NetworkPaginationWorkflowTests : UiTestBase
{
    private const int NetworkPageSize = 6;

    [Test]
    public async Task NetworkPagination_ShouldNavigateForwardAndBackwardAndRespectPageSize()
    {
        var dashboard = new DashboardPage(Page);
        await dashboard.OpenAsync();

        var prefix = $"network-pagination-{Guid.NewGuid():N}";

        // Seven uniquely named networks guarantee that the UI has more than one page,
        // regardless of how many networks already exist in the test environment.
        await NetworkTestHelper.CreateNetworkAsync(Page, $"{prefix}-1");
        await NetworkTestHelper.CreateNetworkAsync(Page, $"{prefix}-2");
        await NetworkTestHelper.CreateNetworkAsync(Page, $"{prefix}-3");
        await NetworkTestHelper.CreateNetworkAsync(Page, $"{prefix}-4");
        await NetworkTestHelper.CreateNetworkAsync(Page, $"{prefix}-5");
        await NetworkTestHelper.CreateNetworkAsync(Page, $"{prefix}-6");
        await NetworkTestHelper.CreateNetworkAsync(Page, $"{prefix}-7");

        var networksSection = Page.Locator("section").Filter(new()
        {
            Has = dashboard.NetworksHeading
        });
        var networkCards = networksSection.Locator("article");
        var pagination = Page.GetByRole(AriaRole.Navigation, new()
        {
            Name = "Network pages",
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

        await Expect(pagination).ToBeVisibleAsync();
        await Expect(pagination).ToContainTextAsync("Page 1 of");
        await Expect(previousButton).ToBeDisabledAsync();
        await Expect(nextButton).ToBeEnabledAsync();
        await Expect(networkCards).ToHaveCountAsync(NetworkPageSize);

        await nextButton.ClickAsync();

        await Expect(pagination).ToContainTextAsync("Page 2 of");
        await Expect(previousButton).ToBeEnabledAsync();
        var secondPageCardCount = await networkCards.CountAsync();
        Assert.That(
            secondPageCardCount,
            Is.InRange(1, NetworkPageSize),
            "The second page should show between one and six network cards.");

        await previousButton.ClickAsync();

        await Expect(pagination).ToContainTextAsync("Page 1 of");
        await Expect(previousButton).ToBeDisabledAsync();
        await Expect(nextButton).ToBeEnabledAsync();
        await Expect(networkCards).ToHaveCountAsync(NetworkPageSize);
    }
}
