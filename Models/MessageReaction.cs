using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Zela.Models;

/// <summary>
/// Biểu tượng cảm xúc cho tin nhắn
/// </summary>
public class MessageReaction
{
    [Key]
    public long ReactionId { get; set; }

    public long MessageId { get; set; }             // FK -> Message
    public int UserId { get; set; }                 // FK -> User
    public string ReactionType { get; set; }        // Like, Love, Haha, Wow, Sad, Angry
    public DateTime CreatedAt { get; set; }

    [ForeignKey(nameof(MessageId))]
    public Message Message { get; set; }
    [ForeignKey(nameof(UserId))]
    public User User { get; set; }
} 