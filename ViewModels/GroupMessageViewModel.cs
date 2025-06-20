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
}