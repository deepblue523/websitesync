using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        var argDict = args
            .Where(a => a.StartsWith("--"))
            .Select(a => a.Split('=', 2))
            .ToDictionary(
                parts => parts[0].ToLowerInvariant(),
                parts => parts.Length > 1 ? parts[1] : string.Empty
            );

        var config = new SyncConfig
        {
            StartingUrl = argDict.TryGetValue("--starturl", out var url) ? url : throw new ArgumentException("Missing --startUrl"),
            MaxPages = argDict.TryGetValue("--maxpages", out var pages) && int.TryParse(pages, out var mp) ? mp : 100,
            MaxDepth = argDict.TryGetValue("--maxdepth", out var depth) && int.TryParse(depth, out var md) ? md : 3,
            UrlFilterRegex = argDict.TryGetValue("--filter", out var filter) ? filter : ".*",
            SkipHrefSubstrings = argDict.TryGetValue("--skiphrefs", out var skips)
                ? skips.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList()
                : new List<string>(),
            AllowedUrlPrefix = argDict.TryGetValue("--urlprefix", out var prefix) ? prefix : null,
            UseJavaScriptRendering = argDict.TryGetValue("--usejs", out var jsMode) && jsMode.Equals("true", StringComparison.OrdinalIgnoreCase)
        };

        IRenderingEngine renderer;
        if (config.UseJavaScriptRendering)
        {
            var playwrightRenderer = new PlaywrightRenderer();
            await playwrightRenderer.InitAsync();
            renderer = playwrightRenderer;
        }
        else
        {
            renderer = new StaticRenderer();
        }

        var crawler = new WebCrawler(config, renderer);
        var results = await crawler.CrawlAsync();

        if (renderer is IAsyncDisposable asyncDisposable)
            await asyncDisposable.DisposeAsync();

        Console.WriteLine($"\n--- Crawl Completed: {results.Count} Pages Imported ---\n");
        foreach (var page in results)
        {
            Console.WriteLine($"[✓] {page.Url} ({page.Title})");
        }
    }
}
