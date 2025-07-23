using System.ComponentModel.DataAnnotations;
using Zela.Enum;

namespace Zela.ViewModels;

public class MeetingRoomMessageViewModel
{
    public long MessageId { get; set; }
    
    [Required(ErrorMessage = "Nội dung tin nhắn không được để trống")]
    [StringLength(1000, ErrorMessage = "Tin nhắn không được quá 1000 ký tự")]
    public string Content { get; set; } = string.Empty;
    
    public int SenderId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public string? SenderAvatar { get; set; }
    
    public int RoomId { get; set; }
    public Guid SessionId { get; set; }
    
    public MessageType MessageType { get; set; }
    public bool IsPrivate { get; set; }
    public int? RecipientId { get; set; }
    public string? RecipientName { get; set; }
    
    public DateTime SentAt { get; set; }
    public bool IsEdited { get; set; }
    public DateTime? EditedAt { get; set; }
    public string? EditReason { get; set; }
    
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeleteReason { get; set; }
    
    // UI Properties
    public bool IsOwnMessage { get; set; }
    public string FormattedTime => SentAt.ToString("HH:mm");
    public string FormattedDate => SentAt.ToString("dd/MM/yyyy");
    public string DisplayContent => IsDeleted ? "[Tin nhắn đã bị xóa]" : Content;
    public bool CanEdit => IsOwnMessage && !IsDeleted && !IsPrivate;
    public bool CanDelete => IsOwnMessage && !IsDeleted;
}

public class MeetingSendMessageViewModel
{
    [Required(ErrorMessage = "Nội dung tin nhắn không được để trống")]
    [StringLength(1000, ErrorMessage = "Tin nhắn không được quá 1000 ký tự")]
    public string Content { get; set; } = string.Empty;
    
    public int RoomId { get; set; }
    public Guid SessionId { get; set; }
    public MessageType MessageType { get; set; } = MessageType.Text;
    public bool IsPrivate { get; set; }
    public int? RecipientId { get; set; }
}

public class MeetingEditMessageViewModel
{
    public long MessageId { get; set; }
    
    [Required(ErrorMessage = "Nội dung tin nhắn không được để trống")]
    [StringLength(1000, ErrorMessage = "Tin nhắn không được quá 1000 ký tự")]
    public string Content { get; set; } = string.Empty;
    
    public string? EditReason { get; set; }
}

public class MeetingChatPanelViewModel
{
    public int RoomId { get; set; }
    public Guid SessionId { get; set; }
    public List<MeetingRoomMessageViewModel> Messages { get; set; } = new();
    public List<UserViewModel> Participants { get; set; } = new();
    public bool AllowChat { get; set; } = true;
    public int CurrentUserId { get; set; }
} 