using BeeCloud.UiTests.Configuration;
using Microsoft.Playwright;
using NUnit.Framework;

namespace BeeCloud.UiTests.Pages;

public sealed class DashboardPage(IPage page)
{
    public IPage Page { get; } = page;

    public ILocator Heading =>
        Page.GetByRole(AriaRole.Heading, new() { Name = "BeeCloud Nodes", Exact = true });

    public ILocator CreateNodeHeading =>
        Page.GetByRole(AriaRole.Heading, new() { Name = "Create Compute Node", Exact = true });

    public ILocator NodeNameInput => Page.GetByLabel("Node Name");
    public ILocator GpuModelInput => Page.GetByLabel("GPU Model");
    public ILocator GpuCountInput => Page.GetByLabel("GPU Count");

    public ILocator CreateNodeButton =>
        Page.GetByRole(AriaRole.Button, new() { Name = "Create Node", Exact = true });

    public ILocator NetworksHeading =>
        Page.GetByRole(AriaRole.Heading, new() { Name = "Networks", Exact = true });

    public ILocator IncidentsHeading =>
        Page.GetByRole(AriaRole.Heading, new() { Name = "Incidents", Exact = true });

    public async Task OpenAsync()
    {
        var response = await Page.GotoAsync(UiTestSettings.BaseUrl);
        Assert.That(response, Is.Not.Null);
        Assert.That(response!.Ok, Is.True, $"Dashboard returned HTTP {response.Status}.");
        await Heading.WaitForAsync();
    }

  
    public async Task FillNodeFormAsync(
        string name,
        string gpuModel,
        string gpuCount)
    {
        await Page.GetByLabel("Node Name").FillAsync(name);

        await Page
            .GetByRole(AriaRole.Textbox, new()
            {
                Name = "GPU Model",
                Exact = true
            })
            .FillAsync(gpuModel);

        await Page.GetByLabel("GPU Count").FillAsync(gpuCount);
    }


    public ILocator SearchNodesInput =>
        Page.GetByRole(AriaRole.Searchbox, new() { Name = "Search nodes", Exact = true });

    public ILocator NodeStatusFilter =>
        Page.GetByLabel("Filter by status", new() { Exact = true });

    public ILocator GpuModelFilter =>
        Page.GetByLabel("Filter by GPU model", new() { Exact = true });

    public ILocator ClearFiltersButton =>
        Page.GetByRole(AriaRole.Button, new() { Name = "Clear filters", Exact = true });

    public ILocator NoMatchingNodesHeading =>
        Page.GetByRole(AriaRole.Heading, new() { Name = "No matching nodes", Exact = true });

    public ILocator NodeLink(string nodeName) =>
        Page.GetByRole(AriaRole.Link, new() { Name = nodeName, Exact = true });

    public ILocator NodeCard(string nodeName) =>
        Page.Locator("article").Filter(new() { Has = NodeLink(nodeName) });

    public ILocator NetworkCard(string networkName)
    {
        var heading = Page.GetByRole(AriaRole.Heading, new() { Name = networkName, Exact = true });
        return Page.Locator("article").Filter(new() { Has = heading });
    }
}
