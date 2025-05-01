using Microsoft.Playwright;
using System.Threading.Tasks;
using System;
using System.Threading;

public class PlaywrightRenderer : IRenderingEngine, IAsyncDisposable
{
    private IPlaywright _playwright;
    private IBrowser _browser;

    public async Task InitAsync()
    {
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
    }

    public async Task<string> GetPageContentAsync(string url)
    {
        using var page = await _browser.NewPageAsync();
        await page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        return await page.ContentAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser != null)
            await _browser.CloseAsync();
        _playwright?.Dispose();
    }
}
