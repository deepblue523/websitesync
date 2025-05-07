// File: WebCrawler.cs
using HtmlAgilityPack;
using RobotsTxt;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System.Xml;
using System;

public class WebCrawler
{
    private readonly SyncConfig _config;
    private readonly IRenderingEngine _renderer;
    private readonly ConcurrentQueue<(string url, int depth)> _queue = new();
    private readonly ConcurrentDictionary<string, bool> _visited = new();
    private readonly ConcurrentBag<SyncItem> _imported = new();
    private readonly Regex _hrefFilter;
    private Robots _robots;
    private SemaphoreSlim _semaphore = new(5);

    public WebCrawler(SyncConfig config, IRenderingEngine renderer)
    {
        _config = config;
        _renderer = renderer;
        _hrefFilter = new Regex(config.UrlFilterRegex, RegexOptions.IgnoreCase);
        _queue.Enqueue((config.StartingUrl, 0));
    }

    public async Task<List<SyncItem>> CrawlAsync()
    {
        var baseUri = new Uri(_config.StartingUrl);
        var client = new HttpClient();

        try
        {
            var robotsText = await client.GetStringAsync($"{baseUri.Scheme}://{baseUri.Host}/robots.txt");
            _robots = Robots.Load(robotsText);
        }
        catch
        {
            _robots = Robots.Load(""); // fallback to allow all
        }

        //var runningTasks = new List<Task>();

        while (_queue.TryDequeue(out var item) && _imported.Count < _config.MaxPages)
        {
            if (!_visited.TryAdd(item.url, true) || item.depth > _config.MaxDepth)
                continue;

            Uri pageUri = new Uri(item.url);
            String pageBaseUrl = pageUri.GetLeftPart(UriPartial.Authority);

            Console.WriteLine($"[>] Crawling: {item.url} (depth: {item.depth})");
            //await _semaphore.WaitAsync();

            //var task = Task.Run(async () =>
            //{
            try
            {
                if (!_robots.IsPathAllowed(item.url, "WebSiteSyncTool"))
                {
                    Console.WriteLine($"[✘] Blocked by robots.txt: {item.url}");
                    continue;
                    //return;
                }

                var html = await _renderer.GetPageContentAsync(item.url);
                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                var contentNode = doc.DocumentNode.SelectSingleNode("//main") ??
                                  doc.DocumentNode.SelectSingleNode("//article") ??
                                  doc.DocumentNode.SelectSingleNode("//body");

                //if (contentNode == null) return;
                if (contentNode == null) continue;

                var plainText = NormalizeText(contentNode.InnerText);
                var title = doc.DocumentNode.SelectSingleNode("//title")?.InnerText?.Trim() ?? "[No Title]";
                var titleAndText = $"{title} {plainText}";
                var titleAndTextNormalized = NormalizeText(titleAndText);

                bool isAllowed = (_config.AllowPagesWith.Count == 0) || _config.AllowPagesWith
                    .Any(allowed => titleAndTextNormalized.Contains(allowed, StringComparison.OrdinalIgnoreCase));

                if (!isAllowed)
                {
                    //Console.WriteLine($"[!] Skipping {item.url} (no required text)");
                    //return;
                    continue;
                }

                if (_hrefFilter.IsMatch(item.url))
                {
                    _imported.Add(new SyncItem
                    {
                        Url = item.url,
                        Title = title,
                        Content = plainText,
                        RetrievedAt = DateTime.UtcNow
                    });
                    Console.WriteLine($"[+] {item.url}");
                }
                else
                {
                    Console.WriteLine($"[-] {item.url} (filtered)");
                }

                //if (_imported.Count >= _config.MaxPages) return;
                if (_imported.Count >= _config.MaxPages) continue;

                IEnumerable<string> links =
                   doc.DocumentNode.SelectNodes("//a[@href]")
                    ?.Select(a => a.GetAttributeValue("href", null))
                    .Where(href => {
                        // HREF is empty, ignore.
                        if (string.IsNullOrWhiteSpace(href))
                        {
                            return false;
                        }

                        // HREF is an internal page link, ignore.
                        if (href.StartsWith("#"))
                        {
                            return false;
                        }

                        // HREF does not match link filter.
                        if (!_hrefFilter.IsMatch(href))
                        {
                            return false;
                        }

                        // HREF prefix filter is empty, meaning all are allowed.
                        if (string.IsNullOrEmpty(_config.AllowedUrlPrefix))
                        {
                            return true;
                        }

                        // HREF could be a relative link, convert to absolute.
                        // This will also handle links like /path/to/page.html
                        var linkAbsUri = new Uri(baseUri, new Uri(href));

                        // HREF is not in the skip list and matches the allowed URL prefix.
                        return !_config.SkipHrefSubstrings.Any(skip => href.Contains(skip, StringComparison.OrdinalIgnoreCase));
                    })
                    .Distinct();

                if (links != null)
                {
                    var linkList = new List<string>(links);
                    foreach (string linkUriStr in linkList)
                    {
                        Uri linkUri = new Uri(linkUriStr, UriKind.RelativeOrAbsolute);

                        // HREF could be a relative link, convert to absolute.
                        // This will also handle links like /path/to/page.html
                        try
                        {
                            Uri linkAbsUri;
                            if (linkUri.IsAbsoluteUri)
                                linkAbsUri = linkUri;
                            else
                            {
                                linkAbsUri = new Uri(new Uri(pageBaseUrl), linkUri);
                            }

                            if (IsWebLink(linkAbsUri))
                            {
                                _queue.Enqueue((linkAbsUri.ToString(), item.depth + 1));
                            }
                        }
                        catch (UriFormatException)
                        {
                            Console.WriteLine($"[!] Invalid URL format: {linkUriStr}");
                        }
                    }
                }
            }
            /*catch (Exception ex)
            {
                Console.WriteLine($"[!] Error: {item.url} - {ex.Message}");
            }*/
            finally
            {
                //_semaphore.Release();
            }
        }//);

        //runningTasks.Add(task);

        //if (runningTasks.Count == 0) break;

        //await Task.WhenAny(runningTasks);
        //runningTasks = runningTasks.Where(t => !t.IsCompleted).ToList();

        //await Task.WhenAll(runningTasks);
        return _imported.ToList();
    }

    private string NormalizeText(string rawText)
    {
        return HtmlEntity.DeEntitize(rawText)
            .Replace("_", " ")
            .Replace("-", " ")
            .Replace("\r", " ")
            .Replace("\n", " ")
            .Replace("\t", " ")
            .Replace("\u00A0", " ")
            .Trim();
    }

    private bool IsWebLink(Uri uri)
    {
        return uri.IsAbsoluteUri &&
               (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    private bool IsWebScheme(Uri uri)
    {
        return (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}
