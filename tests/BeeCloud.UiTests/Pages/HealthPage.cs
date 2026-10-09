using Microsoft.Playwright;

namespace BeeCloud.UiTests.Pages;

public sealed class HealthPage(IPage page)
{
    public IPage Page { get; } = page;

    public ILocator Heading =>
        Page.GetByRole(AriaRole.Heading, new() { Name = "Health Overview", Exact = true });

    public ILocator NodeHealthHeading =>
        Page.GetByRole(AriaRole.Heading, new() { Name = "Node Health", Exact = true });

    public ILocator NavigationLink =>
        Page.GetByRole(AriaRole.Link, new() { Name = "Health", Exact = true });

    public ILocator TimeRange(string range) =>
        Page.GetByRole(AriaRole.Button, new() { Name = range, Exact = true });
}
