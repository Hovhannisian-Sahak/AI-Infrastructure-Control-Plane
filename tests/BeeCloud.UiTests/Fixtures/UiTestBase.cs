using BeeCloud.UiTests.Configuration;
using Microsoft.Playwright;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace BeeCloud.UiTests.Fixtures;

public abstract class UiTestBase
{
    protected IPlaywright Playwright { get; private set; } = null!;
    protected IBrowser Browser { get; private set; } = null!;
    protected IBrowserContext Context { get; private set; } = null!;
    protected IPage Page { get; private set; } = null!;

    [SetUp]
    public async Task SetUpAsync()
    {
        Playwright = await Microsoft.Playwright.Playwright.CreateAsync();

        Browser = await Playwright.Chromium.LaunchAsync(
            new BrowserTypeLaunchOptions
            {
                Headless = UiTestSettings.Headless,
                SlowMo = UiTestSettings.Headless ? 0 : 150
            });

        Context = await Browser.NewContextAsync(
            new BrowserNewContextOptions
            {
                ViewportSize = new ViewportSize { Width = 1440, Height = 1000 }
            });

        Page = await Context.NewPageAsync();
        Page.SetDefaultTimeout(UiTestSettings.DefaultTimeoutMs);
        Page.SetDefaultNavigationTimeout(30_000);
    }

    protected async Task NavigateToDashboardAsync()
    {
        var response = await Page.GotoAsync(UiTestSettings.BaseUrl);
        Assert.That(response, Is.Not.Null, "Dashboard navigation should return a response.");
        Assert.That(response!.Ok, Is.True, $"Dashboard returned HTTP {response.Status}.");
    }

    [TearDown]
    public async Task TearDownAsync()
    {
        try
        {
            if (TestContext.CurrentContext.Result.Outcome.Status == TestStatus.Failed
                && Page is not null)
            {
                var screenshotDirectory = Path.Combine(
                    TestContext.CurrentContext.WorkDirectory,
                    "TestResults",
                    "Screenshots");

                Directory.CreateDirectory(screenshotDirectory);

                var safeTestName = string.Concat(
                    TestContext.CurrentContext.Test.Name.Select(
                        character => Path.GetInvalidFileNameChars().Contains(character)
                            ? '_'
                            : character));

                var screenshotPath = Path.Combine(
                    screenshotDirectory,
                    $"{safeTestName}-{DateTime.UtcNow:yyyyMMddHHmmssfff}.png");

                await Page.ScreenshotAsync(new PageScreenshotOptions
                {
                    Path = screenshotPath,
                    FullPage = true
                });

                TestContext.AddTestAttachment(screenshotPath, "Screenshot on failure");
            }
        }
        finally
        {
            if (Context is not null) await Context.CloseAsync();
            if (Browser is not null) await Browser.CloseAsync();
            Playwright?.Dispose();
        }
    }
}
