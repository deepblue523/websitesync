using System.Threading.Tasks;

public interface IRenderingEngine
{
    Task<string> GetPageContentAsync(string url);
}
