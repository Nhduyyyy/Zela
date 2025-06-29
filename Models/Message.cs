using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Zela.Models;

/// <summary>
/// Tin nhắn (có thể là nhóm hoặc riêng tư).
/// </summary>
public class Message
{
    [Key]
    public long MessageId { get; set; }            // PK: BIGINT IDENTITY(1,1)

    public int SenderId { get; set; }              // FK -> User gửi
    public int? GroupId { get; set; }              // FK -> ChatGroup (nếu chat nhóm)
    public int? RecipientId { get; set; }          // FK -> User nhận (nếu chat riêng)

    public DateTime SentAt { get; set; }           // DATETIME2
    public bool IsEdited { get; set; }             // BIT

    public string Content { get; set; }            // NVARCHAR(MAX)

    // Thêm thuộc tính reply
    public long? ReplyToMessageId { get; set; }   // FK -> Message được reply (nullable)
    [ForeignKey(nameof(ReplyToMessageId))]
    public Message ReplyToMessage { get; set; }

    [ForeignKey(nameof(SenderId))]
    public User Sender { get; set; }
    [ForeignKey(nameof(RecipientId))]
    public User Recipient { get; set; }
    [ForeignKey(nameof(GroupId))]
    public ChatGroup Group { get; set; }

    public ICollection<Media> Media { get; set; }
    public ICollection<Sticker> Sticker { get; set; }
    public ICollection<MessageReaction> Reactions { get; set; }
}