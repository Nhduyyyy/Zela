using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Zela.Enum;

namespace Zela.Models;

/// <summary>
/// Sự kiện trong phòng video call (audit trail)
/// </summary>
public class RoomEvent
{
    [Key]
    public long EventId { get; set; }
    
    public string RoomPassword { get; set; } = string.Empty;     // FK -> VideoRoom.Password
    public RoomEventType EventType { get; set; }                 // Loại sự kiện
    
    public int UserId { get; set; }                              // FK -> User (người thực hiện)
    
    [MaxLength(100)]
    public string? UserName { get; set; }                        // Tên người dùng (cache)
    
    [MaxLength(1000)]
    public string? Details { get; set; }                         // Chi tiết sự kiện
    
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;   // Thời gian xảy ra
    
    // Metadata cho sự kiện (JSON)
    [MaxLength(2000)]
    public string? Metadata { get; set; }                        // Dữ liệu bổ sung
    
    // Navigation properties
    [ForeignKey(nameof(RoomPassword))]
    public VideoRoom Room { get; set; }
    
    [ForeignKey(nameof(UserId))]
    public User User { get; set; }
}