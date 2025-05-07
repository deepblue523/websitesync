using System;
using System.Linq.Expressions;
using System.Net.Http;
using System.Threading.Tasks;

public class StaticRenderer : IRenderingEngine
{
    private readonly HttpClient _client = new();

    public async Task<string> GetPageContentAsync(string url)
    {
        try
        {
            return await _client.GetStringAsync(url);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[✘] Error fetching {url}: {ex.Message}");
            return string.Empty;
        }
    }
}
