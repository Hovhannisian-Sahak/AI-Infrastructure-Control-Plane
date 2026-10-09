using Microsoft.Playwright;

namespace BeeCloud.UiTests.Pages;

public sealed class NodeDetailPage(IPage page)
{
    public IPage Page { get; } = page;

    public ILocator Heading(string nodeName) =>
        Page.GetByRole(AriaRole.Heading, new() { Name = nodeName, Exact = true });

    public ILocator Status(string status) =>
        Page.GetByText(status, new() { Exact = true });

    public ILocator BackToNodes =>
        Page.GetByRole(AriaRole.Link, new() { Name = "← Back to nodes", Exact = true });

    public ILocator HealthHeading =>
        Page.GetByTestId("node-detail-page-header").GetByRole(AriaRole.Heading, new() { Name = "Health", Exact = true });
  
    public ILocator ActiveFaultLabel =>
        Page.GetByText("Active Fault", new() { Exact = true });
}
