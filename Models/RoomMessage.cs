using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Zela.Enum;

namespace Zela.Models;

/// <summary>
/// Tin nhắn trong phòng video call
/// </summary>
public class RoomMessage
{
    [Key]
    public long MessageId { get; set; }
    
    public int RoomId { get; set; }                // FK -> VideoRoom
    public int SenderId { get; set; }              // FK -> User
    public Guid SessionId { get; set; }            // FK -> CallSession
    
    [Required]
    [MaxLength(1000)]
    public string Content { get; set; }            // Nội dung tin nhắn
    
    public MessageType MessageType { get; set; } = MessageType.Text;
    
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    
    public bool IsPrivate { get; set; } = false;   // Tin nhắn riêng tư
    
    public int? RecipientId { get; set; }          // Người nhận (cho tin nhắn riêng tư)
    
    public bool IsEdited { get; set; } = false;
    
    public DateTime? EditedAt { get; set; }
    
    [MaxLength(500)]
    public string? EditReason { get; set; }
    
    public bool IsDeleted { get; set; } = false;
    
    public DateTime? DeletedAt { get; set; }
    
    [MaxLength(500)]
    public string? DeleteReason { get; set; }
    
    // Metadata cho các loại tin nhắn đặc biệt
    [MaxLength(2000)]
    public string? Metadata { get; set; }          // JSON data
    
    // Navigation properties
    [ForeignKey(nameof(RoomId))]
    public VideoRoom Room { get; set; }
    
    [ForeignKey(nameof(SenderId))]
    public User Sender { get; set; }
    
    [ForeignKey(nameof(SessionId))]
    public CallSession Session { get; set; }
    
    [ForeignKey(nameof(RecipientId))]
    public User? Recipient { get; set; }
}