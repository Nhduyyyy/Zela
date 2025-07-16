using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Zela.Enum;

namespace Zela.Models;

/// <summary>
/// Phiên gọi (CallSession) trong VideoRoom.
/// </summary>
public class CallSession
{
    [Key]
    public Guid SessionId { get; set; }            // PK: UNIQUEIDENTIFIER

    public int RoomId { get; set; }                // FK -> VideoRoom
    public DateTime StartedAt { get; set; }        // DATETIME2
    public DateTime? EndedAt { get; set; }         // DATETIME2 NULL

    [MaxLength(500)]
    public string RecordingUrl { get; set; }       // NVARCHAR(500) NULL

    // ======== NEW FIELDS FOR ADVANCED FEATURES ========

    /// <summary>
    /// Loại phiên gọi
    /// </summary>
    public SessionType SessionType { get; set; } = SessionType.Normal;

    /// <summary>
    /// Phiên cha (cho breakout rooms)
    /// </summary>
    public Guid? ParentSessionId { get; set; }

    /// <summary>
    /// Cài đặt phiên (JSON)
    /// </summary>
    [MaxLength(2000)]
    public string? Settings { get; set; }

    /// <summary>
    /// Thông số chất lượng (JSON)
    /// </summary>
    [MaxLength(1000)]
    public string? QualityMetrics { get; set; }

    /// <summary>
    /// Số người tham gia
    /// </summary>
    public int ParticipantCount { get; set; } = 0;

    /// <summary>
    /// Số tin nhắn
    /// </summary>
    public int MessageCount { get; set; } = 0;

    /// <summary>
    /// Số bình chọn
    /// </summary>
    public int PollCount { get; set; } = 0;

    /// <summary>
    /// Số lần giơ tay
    /// </summary>
    public int HandRaiseCount { get; set; } = 0;

    /// <summary>
    /// Người tạo phiên
    /// </summary>
    public int? CreatedBy { get; set; }

    /// <summary>
    /// Thời gian cập nhật
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    [ForeignKey(nameof(RoomId))]
    public VideoRoom VideoRoom { get; set; }

    [ForeignKey(nameof(ParentSessionId))]
    public CallSession? ParentSession { get; set; }

    [ForeignKey(nameof(CreatedBy))]
    public User? CreatedByUser { get; set; }

    public ICollection<Attendance> Attendances { get; set; }
    public ICollection<CallTranscript> Transcripts { get; set; }
    public ICollection<Recording> Recordings { get; set; } = new List<Recording>();

    /// <summary>
    /// Danh sách phiên con (breakout rooms)
    /// </summary>
    public ICollection<CallSession> ChildSessions { get; set; } = new List<CallSession>();

    /// <summary>
    /// Danh sách tin nhắn trong phiên
    /// </summary>
    public ICollection<RoomMessage> Messages { get; set; } = new List<RoomMessage>();

    /// <summary>
    /// Danh sách bình chọn trong phiên
    /// </summary>
    public ICollection<RoomPoll> Polls { get; set; } = new List<RoomPoll>();
}