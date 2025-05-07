using System.Collections.Generic;

public class SyncConfig
{
    public string StartingUrl { get; set; }
    public int MaxPages { get; set; }
    public int MaxDepth { get; set; }
    public string UrlFilterRegex { get; set; }
    public List<string> SkipHrefSubstrings { get; set; } = new();
    public List<string> AllowPagesWith { get; set; } = new();
    public string AllowedUrlPrefix { get; set; }
    public bool UseJavaScriptRendering { get; set; } = false;
}
