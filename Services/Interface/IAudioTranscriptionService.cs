using Zela.Models;

namespace Zela.Services.Interface
{
    public interface IAudioTranscriptionService
    {
        /// <summary>
        /// Transcribe audio chunk thành text
        /// </summary>
        Task<string> TranscribeAudioChunkAsync(byte[] audioData, string language = "vi", Guid sessionId = default, string speakerId = "");
        
        /// <summary>
        /// Xử lý audio real-time và tạo subtitle
        /// </summary>
        Task<RealTimeSubtitle> ProcessRealTimeAudioAsync(Stream audioStream, Guid sessionId, int speakerId, string language = "vi-VN");
        
        /// <summary>
        /// Lấy danh sách subtitle của session
        /// </summary>
        Task<List<RealTimeSubtitle>> GetSessionSubtitlesAsync(Guid sessionId);
        
        /// <summary>
        /// Lưu subtitle vào database
        /// </summary>
        Task SaveSubtitleAsync(RealTimeSubtitle subtitle);
        
        /// <summary>
        /// Bật/tắt subtitle cho user
        /// </summary>
        Task<bool> EnableSubtitlesForUserAsync(Guid sessionId, int userId, bool enabled);
        
        /// <summary>
        /// Lấy preference subtitle của user
        /// </summary>
        Task<bool> GetUserSubtitlePreferenceAsync(Guid sessionId, int userId);
    }
} 