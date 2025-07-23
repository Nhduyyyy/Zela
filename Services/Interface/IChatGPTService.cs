namespace Zela.Services.Interface
{
    public interface IChatGPTService
    {
        Task<string> SummarizeAsync(string content);
        
        Task<string> TranslateTextAsync(string text, string sourceLanguage, string targetLanguage);
    }
} 