using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Zela.Enum;

namespace Zela.Models;

/// <summary>
/// Phòng nhóm nhỏ trong cuộc họp chính
/// </summary>
public class BreakoutRoom
{
    [Key]
    public int BreakoutRoomId { get; set; }
    
    public string MainRoomPassword { get; set; } = string.Empty;  // FK -> VideoRoom.Password
    public string Name { get; set; } = string.Empty;             // Tên phòng nhóm
    
    public int HostId { get; set; }                              // FK -> User (host của phòng nhóm)
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }                     // Thời gian bắt đầu
    public DateTime? EndedAt { get; set; }                       // Thời gian kết thúc
    
    public BreakoutRoomStatus Status { get; set; } = BreakoutRoomStatus.Created;
    
    public int TimeLimit { get; set; } = 0;                      // Thời gian giới hạn (phút), 0 = không giới hạn
    
    [MaxLength(1000)]
    public string? Instructions { get; set; }                    // Hướng dẫn cho phòng nhóm
    
    // Navigation properties
    [ForeignKey(nameof(MainRoomPassword))]
    public VideoRoom MainRoom { get; set; }
    
    [ForeignKey(nameof(HostId))]
    public User Host { get; set; }
    
    public ICollection<BreakoutRoomParticipant> Participants { get; set; } = new List<BreakoutRoomParticipant>();
}

/// <summary>
/// Người tham gia phòng nhóm nhỏ
/// </summary>
public class BreakoutRoomParticipant
{
    public int BreakoutRoomId { get; set; }                      // FK -> BreakoutRoom
    public int UserId { get; set; }                              // FK -> User
    
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;  // Thời gian được phân công
    public DateTime? JoinedAt { get; set; }                      // Thời gian thực sự tham gia
    public DateTime? LeftAt { get; set; }                        // Thời gian rời phòng nhóm
    
    // Navigation properties
    [ForeignKey(nameof(BreakoutRoomId))]
    public BreakoutRoom BreakoutRoom { get; set; }
    
    [ForeignKey(nameof(UserId))]
    public User User { get; set; }
}