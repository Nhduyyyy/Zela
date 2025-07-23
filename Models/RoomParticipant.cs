using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Zela.Enum;

namespace Zela.Models;

/// <summary>
/// Liên kết User tham gia VideoRoom.
/// </summary>
public class RoomParticipant
{
    public int RoomId { get; set; }                // FK -> VideoRoom
    public int UserId { get; set; }                // FK -> User

    public bool IsModerator { get; set; }          // BIT
    public bool IsHost { get; set; }              // BIT - true nếu là host của phòng
    public DateTime JoinedAt { get; set; }         // DATETIME2
    
    // ======== NEW FIELDS FOR ADVANCED FEATURES ========
    
    /// <summary>
    /// Trạng thái tham gia
    /// </summary>
    public ParticipantStatus Status { get; set; } = ParticipantStatus.Joined;
    
    /// <summary>
    /// Thời gian rời phòng
    /// </summary>
    public DateTime? LeftAt { get; set; }
    
    /// <summary>
    /// Lý do rời phòng
    /// </summary>
    [MaxLength(200)]
    public string? LeaveReason { get; set; }
    
    /// <summary>
    /// Chất lượng video hiện tại
    /// </summary>
    public VideoQuality CurrentVideoQuality { get; set; } = VideoQuality.Medium;
    
    /// <summary>
    /// Trạng thái camera
    /// </summary>
    public bool IsVideoEnabled { get; set; } = true;
    
    /// <summary>
    /// Trạng thái microphone
    /// </summary>
    public bool IsAudioEnabled { get; set; } = true;
    
    /// <summary>
    /// Trạng thái chia sẻ màn hình
    /// </summary>
    public bool IsScreenSharing { get; set; } = false;
    
    /// <summary>
    /// Trạng thái giơ tay
    /// </summary>
    public bool IsHandRaised { get; set; } = false;
    
    /// <summary>
    /// Thời gian giơ tay
    /// </summary>
    public DateTime? HandRaisedAt { get; set; }
    
    /// <summary>
    /// Trạng thái bị tắt tiếng bởi host
    /// </summary>
    public bool IsMutedByHost { get; set; } = false;
    
    /// <summary>
    /// Trạng thái bị tắt camera bởi host
    /// </summary>
    public bool IsVideoDisabledByHost { get; set; } = false;
    
    /// <summary>
    /// Thời gian bị tắt tiếng/camera
    /// </summary>
    public DateTime? MutedAt { get; set; }
    
    /// <summary>
    /// Lý do bị tắt tiếng/camera
    /// </summary>
    [MaxLength(200)]
    public string? MuteReason { get; set; }
    
    /// <summary>
    /// Thông tin kết nối (JSON)
    /// </summary>
    [MaxLength(1000)]
    public string? ConnectionInfo { get; set; }
    
    /// <summary>
    /// Thời gian cuối cùng hoạt động
    /// </summary>
    public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(RoomId))]
    public VideoRoom VideoRoom { get; set; }
    [ForeignKey(nameof(UserId))]
    public User User { get; set; }
}