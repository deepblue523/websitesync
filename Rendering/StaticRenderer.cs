using System.Net.Http;
using System.Threading.Tasks;

public class StaticRenderer : IRenderingEngine
{
    private readonly HttpClient _client = new();

    public async Task<string> GetPageContentAsync(string url)
    {
        return await _client.GetStringAsync(url);
    }
}
