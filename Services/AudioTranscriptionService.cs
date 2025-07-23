using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Zela.DbContext;
using Zela.Models;
using Zela.Services.Interface;

namespace Zela.Services
{
    public class AudioTranscriptionService : IAudioTranscriptionService
    {
        private readonly HttpClient _httpClient;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AudioTranscriptionService> _logger;
        private readonly string _openAIApiKey;
        
        // Cache cho user preferences
        private readonly Dictionary<string, bool> _userPreferences = new();

        public AudioTranscriptionService(
            HttpClient httpClient,
            ApplicationDbContext context,
            ILogger<AudioTranscriptionService> logger,
            IConfiguration configuration)
        {
            _httpClient = httpClient;
            _context = context;
            _logger = logger;
            _openAIApiKey = configuration["OpenAI:ApiKey"];
        }

        public async Task<string> TranscribeAudioChunkAsync(byte[] audioData, string language, Guid sessionId, string speakerId)
        {
            try
            {
                _logger.LogInformation("🎤 Starting transcription for session {SessionId}, speaker {SpeakerId}", sessionId, speakerId);
                
                // Kiểm tra null và validation
                if (audioData == null || audioData.Length == 0)
                {
                    _logger.LogWarning("⚠️ Audio data is null or empty");
                    return "";
                }
                
                if (string.IsNullOrEmpty(_openAIApiKey))
                {
                    _logger.LogError("❌ OpenAI API key is not configured");
                    return "";
                }
                
                if (_httpClient == null)
                {
                    _logger.LogError("❌ HttpClient is not initialized");
                    return "";
                }
                
                // Thêm speaker identification vào prompt
                var speakerName = await GetSpeakerNameAsync(speakerId);
                var speakerContext = !string.IsNullOrEmpty(speakerName) 
                    ? $"Người nói: {speakerName}. " 
                    : "";

                // Tạo form data cho OpenAI Whisper API với cấu hình tối ưu
                var formData = new MultipartFormDataContent();
                formData.Add(new ByteArrayContent(audioData), "file", "audio.webm");
                formData.Add(new StringContent("whisper-1"), "model");
                formData.Add(new StringContent(language), "language");
                formData.Add(new StringContent("verbose_json"), "response_format");
                
                // Thêm prompt để cải thiện độ chính xác cho tiếng Việt
                var prompt = language == "vi" 
                    ? $"{speakerContext}Đây là cuộc hội thoại bằng tiếng Việt. Vui lòng transcribe chính xác với dấu câu và ngữ điệu tự nhiên."
                    : $"{speakerContext}This is a conversation. Please transcribe accurately with natural punctuation and intonation.";
                formData.Add(new StringContent(prompt), "prompt");
                
                // Tạo HTTP request
                var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/audio/transcriptions");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _openAIApiKey);
                request.Content = formData;
                
                _logger.LogInformation("📡 Sending request to OpenAI API...");
                
                // Gửi request đến OpenAI
                var response = await _httpClient.SendAsync(request);
                
                // Log response details
                _logger.LogInformation("📡 OpenAI response status: {StatusCode}", response.StatusCode);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("❌ OpenAI API error: {StatusCode} - {Error}", response.StatusCode, errorContent);
                    return "";
                }
                
                // Đọc response
                var jsonResponse = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("📄 OpenAI response: {Response}", jsonResponse);
                
                var result = JsonSerializer.Deserialize<WhisperResponse>(jsonResponse);
                
                var transcribedText = result?.text?.Trim() ?? "";
                
                // Post-processing để cải thiện text
                transcribedText = PostProcessTranscription(transcribedText, language);
                
                _logger.LogInformation("📝 Transcription completed: {Text}", transcribedText);
                
                return transcribedText;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error transcribing audio: {Message}", ex.Message);
                return "";
            }
        }

        public async Task<RealTimeSubtitle> ProcessRealTimeAudioAsync(Stream audioStream, Guid sessionId, int speakerId, string language = "vi")
        {
            try
            {
                // Chuyển stream thành bytes
                using var memoryStream = new MemoryStream();
                await audioStream.CopyToAsync(memoryStream);
                var audioBytes = memoryStream.ToArray();
                
                // Transcribe audio
                var transcribedText = await TranscribeAudioChunkAsync(audioBytes, language, sessionId, speakerId.ToString());
                
                if (string.IsNullOrWhiteSpace(transcribedText))
                {
                    _logger.LogWarning("⚠️ No text transcribed from audio");
                    return null;
                }
                
                // Tạo subtitle object
                var subtitle = new RealTimeSubtitle
                {
                    Id = Guid.NewGuid(),
                    SessionId = sessionId,
                    SpeakerId = speakerId,
                    OriginalText = transcribedText,
                    Language = language,
                    Timestamp = DateTime.UtcNow,
                    StartTime = GetCurrentSessionTime(sessionId),
                    EndTime = GetCurrentSessionTime(sessionId) + 3.0m, // 3 giây
                    IsFinal = true,
                    AudioChunkId = Guid.NewGuid().ToString()
                };
                
                // Lưu vào database (có thể fail nhưng vẫn return subtitle)
                try
                {
                    await SaveSubtitleAsync(subtitle);
                    _logger.LogInformation("✅ Real-time subtitle saved to database: {Text}", transcribedText);
                }
                catch (Exception dbEx)
                {
                    _logger.LogWarning("⚠️ Failed to save subtitle to database: {Error}", dbEx.Message);
                    // Vẫn return subtitle để hiển thị trên UI
                }
                
                _logger.LogInformation("✅ Real-time subtitle created: {Text}", transcribedText);
                
                return subtitle;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error processing real-time audio: {Message}", ex.Message);
                return null;
            }
        }

        public async Task<List<RealTimeSubtitle>> GetSessionSubtitlesAsync(Guid sessionId)
        {
            try
            {
                return await _context.RealTimeSubtitles
                    .Where(s => s.SessionId == sessionId)
                    .OrderBy(s => s.Timestamp)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error getting session subtitles: {Message}", ex.Message);
                return new List<RealTimeSubtitle>();
            }
        }

        public async Task SaveSubtitleAsync(RealTimeSubtitle subtitle)
        {
            try
            {
                await _context.RealTimeSubtitles.AddAsync(subtitle);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("💾 Subtitle saved: {Id}", subtitle.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error saving subtitle: {Message}", ex.Message);
                throw;
            }
        }

        public async Task<bool> EnableSubtitlesForUserAsync(Guid sessionId, int userId, bool enabled)
        {
            try
            {
                var key = $"{sessionId}_{userId}";
                _userPreferences[key] = enabled;
                
                _logger.LogInformation("🎛️ User {UserId} subtitle preference set to {Enabled} for session {SessionId}", 
                    userId, enabled, sessionId);
                
                return enabled;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error setting user subtitle preference: {Message}", ex.Message);
                return false;
            }
        }

        public async Task<bool> GetUserSubtitlePreferenceAsync(Guid sessionId, int userId)
        {
            try
            {
                var key = $"{sessionId}_{userId}";
                return _userPreferences.TryGetValue(key, out var enabled) && enabled;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error getting user subtitle preference: {Message}", ex.Message);
                return false;
            }
        }

        private decimal GetCurrentSessionTime(Guid sessionId)
        {
            // Tính thời gian từ khi session bắt đầu (giây)
            // Sử dụng thời gian tương đối thay vì epoch time
            var sessionStartTime = DateTime.UtcNow.AddMinutes(-5); // Giả sử session bắt đầu 5 phút trước
            return (decimal)(DateTime.UtcNow - sessionStartTime).TotalSeconds;
        }

        private string PostProcessTranscription(string text, string language)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            // Cải thiện text cho tiếng Việt
            if (language == "vi")
            {
                // Sửa các lỗi phổ biến
                text = text.Replace("chào tạm biệt", "chào tạm biệt")
                          .Replace("xin chào", "xin chào")
                          .Replace("cảm ơn", "cảm ơn")
                          .Replace("tạm biệt", "tạm biệt");
                
                // Thêm dấu câu nếu thiếu
                if (!text.EndsWith(".") && !text.EndsWith("!") && !text.EndsWith("?"))
                {
                    text += ".";
                }
                
                // Viết hoa chữ cái đầu
                if (text.Length > 0)
                {
                    text = char.ToUpper(text[0]) + text.Substring(1);
                }
            }

            return text;
        }

        private async Task<string> GetSpeakerNameAsync(string speakerId)
        {
            try
            {
                if (int.TryParse(speakerId, out var userId))
                {
                    var user = await _context.Users.FindAsync(userId);
                    return user?.FullName ?? user?.Email ?? $"User {userId}";
                }
                return speakerId;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("⚠️ Error getting speaker name: {Error}", ex.Message);
                return speakerId;
            }
        }
    }

    // Response model cho OpenAI Whisper
    public class WhisperResponse
    {
        public string text { get; set; }
    }
} 