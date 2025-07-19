using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Zela.Services.Interface;

namespace Zela.Services
{
    public class VoiceToTextService : IVoiceToTextService
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public VoiceToTextService(IConfiguration configuration)
        {
            _configuration = configuration;
            _httpClient = new HttpClient();
        }

        public async Task<string> ConvertVoiceToTextAsync(Stream audioStream, string language)
        {
            var apiKey = _configuration["OpenAI:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
                return "Không tìm thấy API Key!";

            var requestContent = new MultipartFormDataContent();
            // Đặt tên file là audio.webm (hoặc .mp3/.wav nếu bạn muốn)
            var audioContent = new StreamContent(audioStream);
            audioContent.Headers.ContentType = new MediaTypeHeaderValue("audio/webm");
            requestContent.Add(audioContent, "file", "audio.webm");
            requestContent.Add(new StringContent("whisper-1"), "model");
            if (!string.IsNullOrEmpty(language))
                requestContent.Add(new StringContent(language), "language");

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            var response = await _httpClient.PostAsync("https://api.openai.com/v1/audio/transcriptions", requestContent);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return $"Lỗi OpenAI: {error}";
            }

            var result = await response.Content.ReadFromJsonAsync<OpenAIWhisperResponse>();
            return result?.text ?? "";
        }

        private class OpenAIWhisperResponse
        {
            public string text { get; set; }
        }
    }
} 