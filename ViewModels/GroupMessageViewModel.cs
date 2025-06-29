using System.ComponentModel.DataAnnotations;

namespace Zela.ViewModels;

public class GroupMessageViewModel
{
    public int MessageId { get; set; }
    public int SenderId { get; set; }
    public int GroupId { get; set; }
    public string SenderName { get; set; }
    public string AvatarUrl { get; set; }
    public string Content { get; set; }
    public DateTime SentAt { get; set; }
    public bool IsMine { get; set; }
    public bool IsEdited { get; set; }
    
    // Thêm thuộc tính Media để hỗ trợ file và ảnh
    public List<MediaViewModel> Media { get; set; } = new();
    //Reactions
    public List<MessageReactionSummaryViewModel> Reactions { get; set; } = new List<MessageReactionSummaryViewModel>();

    // Reply support
    public long? ReplyToMessageId { get; set; }
    public string ReplyToMessageContent { get; set; }
    public string ReplyToMessageSenderName { get; set; }

    public string StickerUrl { get; set; }
    public string StickerType { get; set; }
}