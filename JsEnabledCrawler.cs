using Microsoft.Playwright;
using System.Threading.Tasks;

public class JsEnabledCrawler
{
    private IPlaywright _playwright;
    private IBrowser _browser;

    public async Task InitAsync()
    {
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
    }

    public async Task<string> GetRenderedHtmlAsync(string url)
    {
        var page = await _browser.NewPageAsync();
        await page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        return await page.ContentAsync(); // fully rendered HTML
    }

    public async Task DisposeAsync()
    {
        await _browser.CloseAsync();
        _playwright.Dispose();
    }
}
