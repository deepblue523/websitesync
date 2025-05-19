// File: Program.cs
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;

class Program
{
    static async Task Main(string[] args)
    {
        if ((args.Length < 2) || (args.Length > 1 && args[1] == "/?"))
        {
            Console.WriteLine("Arguments:");
            Console.WriteLine("  --starturl=<url>       The starting URL for the crawl.");
            Console.WriteLine("  --outputpath=<path>    The output directory for the crawled pages.");
            Console.WriteLine("  --maxpages=<number>    The maximum number of pages to crawl (default: 100).");
            Console.WriteLine("  --maxdepth=<number>    The maximum depth to crawl (default: 3).");
            Console.WriteLine("  --filter=<regex>       Regex to filter URLs (default: '.*').");
            Console.WriteLine("  --textrequired=<text>  Comma-separated list of required text in page content.");
            Console.WriteLine("  --skiphrefs=<text>     Comma-separated list of substrings to skip in hrefs.");
            Console.WriteLine("  --urlprefix=<prefix>   URL prefix to restrict crawling.");
            Console.WriteLine("  --usejs=<true|false>   Use JavaScript rendering (default: false).");
            Console.WriteLine("  /?                    Show this help message.");
            return;
        }

        var argDict = args
            .Where(a => a.StartsWith("--"))
            .Select(a => a.Split('=', 2))
            .ToDictionary(
                parts => parts[0].ToLowerInvariant(),
                parts => parts.Length > 1 ? parts[1] : string.Empty
            );

        string outputPath = argDict.TryGetValue("--outputpath", out var path) ? path : throw new ArgumentException("Missing --outputpath");

        var config = new SyncConfig
        {
            StartingUrl = argDict.TryGetValue("--starturl", out var url) ? url : throw new ArgumentException("Missing --startUrl"),
            MaxPages = argDict.TryGetValue("--maxpages", out var pages) && int.TryParse(pages, out var mp) ? mp : 100,
            MaxDepth = argDict.TryGetValue("--maxdepth", out var depth) && int.TryParse(depth, out var md) ? md : 3,
            UrlFilterRegex = argDict.TryGetValue("--filter", out var filter) ? filter : ".*",
            AllowPagesWith = argDict.TryGetValue("--textrequired", out var required)
                ? required.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList()
                : new List<string>(),
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

            string cleanTitle = string.Join("_", page.Title.Split(Path.GetInvalidFileNameChars()));
            string relFilename = cleanTitle + ".txt";
            string fileFullPath = Path.Combine(outputPath, relFilename);

            try
            {
                File.WriteAllText(fileFullPath, page.Content);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[!] Error writing file {fileFullPath}: {ex.Message}");
            }
        }
    }
}
