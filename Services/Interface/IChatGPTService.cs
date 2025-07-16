namespace Zela.Services.Interface
{
    public interface IChatGPTService
    {
        Task<string> SummarizeAsync(string content);
    }
} 