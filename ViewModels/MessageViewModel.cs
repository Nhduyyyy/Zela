using Zela.Enum;

namespace Zela.ViewModels;

public class MessageViewModel
{
    public long MessageId { get; set; }
    public int SenderId { get; set; }
    public int? RecipientId { get; set; }
    public int? GroupId { get; set; }
    public string SenderName { get; set; }
    public string AvatarUrl { get; set; }
    public string Content { get; set; }
    public DateTime SentAt { get; set; }
    public bool IsMine { get; set; }
    public bool IsEdited { get; set; }

    // Thêm thuộc tính này:
    public List<MediaViewModel> Media { get; set; } = new();
    // sticker
    public string StickerUrl { get; set; }
    public string StickerType { get; set; }
    // add MessageStatus
    public MessageStatus Status { get; set; }
    public string StatusText => Status switch
    {
        MessageStatus.Sent => "Đã gửi",
        MessageStatus.Delivered => "Đã nhận",
        MessageStatus.Seen => "Đã xem",
        _ => ""
    };
    // Reply support
    public long? ReplyToMessageId { get; set; }
    public string ReplyToMessageContent { get; set; }
    public string ReplyToMessageSenderName { get; set; }
}

public class MediaViewModel
{
    public string Url { get; set; }
    public string MediaType { get; set; }
    public string? FileName { get; set; }
}