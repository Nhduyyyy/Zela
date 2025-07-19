using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Zela.Models;

/// <summary>
/// Real-time subtitle cho video call
/// </summary>
public class RealTimeSubtitle
{
    [Key]
    public Guid Id { get; set; }
    
    public Guid SessionId { get; set; }           // FK -> CallSession
    public int SpeakerId { get; set; }            // FK -> User (người nói)
    public string OriginalText { get; set; }      // Text gốc được transcribe
    public string? TranslatedText { get; set; }   // Text dịch (nếu có)
    public string Language { get; set; } = "vi"; // Ngôn ngữ (ISO-639-1)
    public DateTime Timestamp { get; set; }       // Thời gian tạo
    public decimal StartTime { get; set; }        // Thời gian bắt đầu (giây)
    public decimal EndTime { get; set; }          // Thời gian kết thúc (giây)
    public bool IsFinal { get; set; } = false;    // Đã hoàn thành chưa
    public string AudioChunkId { get; set; }      // ID của audio chunk
    
    // Navigation properties
    [ForeignKey(nameof(SessionId))]
    public CallSession CallSession { get; set; }
    
    [ForeignKey(nameof(SpeakerId))]
    public User Speaker { get; set; }
} 