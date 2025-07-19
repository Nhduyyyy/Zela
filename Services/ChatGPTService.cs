using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Zela.Services.Interface;

namespace Zela.Services
{
    public class ChatGPTService : IChatGPTService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        public ChatGPTService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _apiKey = config["OpenAI:ApiKey"];
        }
        public async Task<string> SummarizeAsync(string content)
        {
            var requestBody = new
            {
                model = "gpt-4",
                messages = new[]
                {
                    new { role = "system", content = "Bạn là một trợ lý AI thông minh và giàu ngôn ngữ cảm xúc. " +
                                                     "Hãy đọc đoạn văn bản sau và tóm tắt nội dung một cách ngắn gọn, dễ hiểu, mạch lạc.\n\n" +
                                                     "🎯 **Yêu cầu tóm tắt:**\n" +
                                                     "- Tóm tắt rõ ràng, có thể chia đoạn nếu cần.\n" +
                                                     "- Dùng giọng văn mượt mà, truyền cảm, có chiều sâu.\n" +
                                                     "- **Bôi đậm các từ khóa chính hoặc cụm ý quan trọng** (sử dụng cú pháp Markdown: `**từ khóa**`).\n" +
                                                     "- Tránh đưa ra đánh giá chủ quan, tập trung vào việc truyền tải thông tin cốt lõi.\n\n" +
                                                     "📌 **Sau phần tóm tắt, hãy trình bày thêm:**\n- " +
                                                     "✅ Danh sách ngắn gọn (checklist) những điểm chính hoặc hành động nên thực hiện (nếu có).\n- " +
                                                     "🧠 Biểu tượng cảm xúc phù hợp giúp nội dung sinh động và dễ tiếp cận hơn.\n- " +
                                                     "📄 Tóm tắt nên trình bày dưới dạng Markdown để có thể hiển thị đẹp mắt trên web/chat." },
                    new { role = "user", content = $"Hãy tóm tắt nội dung sau:\n{content}" }
                },
                max_tokens = 650,
                temperature = 0.5
            };
            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            request.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var summary = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();
            return summary;
        }

        public async Task<string> TranslateTextAsync(string text, string sourceLanguage, string targetLanguage)
        {
            var languageNames = new Dictionary<string, string>
            {
                { "vi", "Vietnamese" },
                { "en", "English" },
                { "zh", "Chinese" },
                { "ja", "Japanese" },
                { "ko", "Korean" },
                { "fr", "French" },
                { "de", "German" },
                { "es", "Spanish" }
            };

            var sourceLangName = languageNames.GetValueOrDefault(sourceLanguage, sourceLanguage);
            var targetLangName = languageNames.GetValueOrDefault(targetLanguage, targetLanguage);

            var requestBody = new
            {
                model = "gpt-4",
                messages = new[]
                {
                    new { role = "system", content = $"You are a professional translator. Translate the following text from {sourceLangName} to {targetLangName}. Provide only the translated text without any explanations or additional content." },
                    new { role = "user", content = text }
                },
                max_tokens = 200,
                temperature = 0.3
            };

            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            request.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var translatedText = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();
                
            return translatedText ?? text;
        }
    }
} 