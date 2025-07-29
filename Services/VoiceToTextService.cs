using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
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

            try
            {
                // Chuyển stream thành bytes để xử lý
                using var memoryStream = new MemoryStream();
                await audioStream.CopyToAsync(memoryStream);
                var audioBytes = memoryStream.ToArray();

                // Tạo form data cho OpenAI Whisper API với cấu hình tối ưu
                var formData = new MultipartFormDataContent();
                formData.Add(new ByteArrayContent(audioBytes), "file", "audio.webm");
                formData.Add(new StringContent("whisper-1"), "model");
                formData.Add(new StringContent(language), "language");
                formData.Add(new StringContent("verbose_json"), "response_format");
                
                // Thêm prompt để cải thiện độ chính xác cho từng ngôn ngữ
                var prompt = GetOptimizedPrompt(language);
                formData.Add(new StringContent(prompt), "prompt");

                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

                var response = await _httpClient.PostAsync("https://api.openai.com/v1/audio/transcriptions", formData);
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    return $"Lỗi OpenAI: {error}";
                }

                var jsonResponse = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<OpenAIWhisperResponse>(jsonResponse);
                
                var transcribedText = result?.text?.Trim() ?? "";
                
                // Post-processing để cải thiện text
                transcribedText = PostProcessTranscription(transcribedText, language);
                
                return transcribedText;
            }
            catch (Exception ex)
            {
                return $"Lỗi xử lý: {ex.Message}";
            }
        }

        private string GetOptimizedPrompt(string language)
        {
            return language switch
            {
                "vi" => "Đây là cuộc hội thoại bằng tiếng Việt. Vui lòng transcribe chính xác với dấu câu và ngữ điệu tự nhiên. Chú ý các từ có dấu tiếng Việt.",
                "en" => "This is a conversation in English. Please transcribe accurately with natural punctuation and intonation. Pay attention to context and grammar.",
                "ja" => "これは日本語の会話です。自然な句読点とイントネーションで正確に転写してください。敬語や丁寧語に注意してください。",
                "zh" => "这是中文对话。请准确转录，注意自然的标点符号和语调。注意中文的声调和语境。",
                "ko" => "이것은 한국어 대화입니다. 자연스러운 문장 부호와 억양으로 정확하게 전사해 주세요. 존댓말과 반말에 주의하세요.",
                "fr" => "Ceci est une conversation en français. Veuillez transcrire avec précision, en respectant la ponctuation et l'intonation naturelles. Attention à l'accent et aux liaisons.",
                "it" => "Questa è una conversazione in italiano. Trascrivi accuratamente con punteggiatura e intonazione naturali. Presta attenzione all'accento e al contesto.",
                "th" => "นี่คือการสนทนาเป็นภาษาไทย กรุณาเขียนตามคำพูดอย่างถูกต้องพร้อมเครื่องหมายวรรคตอนและน้ำเสียงที่เป็นธรรมชาติ",
                _ => "This is a conversation. Please transcribe accurately with natural punctuation and intonation."
            };
        }

        private string PostProcessTranscription(string text, string language)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            // Loại bỏ khoảng trắng thừa
            text = text.Trim();

            // Xử lý đặc biệt cho tiếng Việt
            if (language == "vi")
            {
                // Sửa lỗi dấu câu phổ biến
                text = text.Replace(" ,", ",")
                          .Replace(" .", ".")
                          .Replace(" !", "!")
                          .Replace(" ?", "?")
                          .Replace(" :", ":")
                          .Replace(" ;", ";");

                // Đảm bảo chữ cái đầu câu viết hoa
                if (text.Length > 0)
                {
                    text = char.ToUpper(text[0]) + text.Substring(1);
                }

                // Sửa lỗi khoảng trắng trước dấu câu
                text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+([,.!?:;])", "$1");
            }

            // Xử lý chung cho tất cả ngôn ngữ
            // Loại bỏ khoảng trắng liên tiếp
            text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ");

            return text;
        }

        private class OpenAIWhisperResponse
        {
            public string text { get; set; }
        }
    }
} 