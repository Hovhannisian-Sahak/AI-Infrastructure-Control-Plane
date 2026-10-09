using Microsoft.Playwright;

namespace BeeCloud.UiTests.Helpers;

public static class NetworkTestHelper
{
    public static async Task<string> CreateNetworkAsync(
        IPage page,
        string? networkName = null,
        string? description = null)
    {
        var name = networkName ?? TestData.NetworkName();

        await page.GetByLabel("Network Name").FillAsync(name);

        if (!string.IsNullOrWhiteSpace(description))
            await page.GetByLabel("Description").FillAsync(description);

        await page.GetByRole(
            AriaRole.Button,
            new() { Name = "Create Network", Exact = true })
            .ClickAsync();

        var heading = page.GetByRole(
            AriaRole.Heading,
            new() { Name = name, Exact = true });

        await heading.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        return name;
    }
}
