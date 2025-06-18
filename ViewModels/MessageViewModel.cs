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
}

public class MediaViewModel
{
    public string Url { get; set; }
    public string MediaType { get; set; }
}