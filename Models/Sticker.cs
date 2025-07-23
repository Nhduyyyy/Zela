using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Zela.Models;

public class Sticker
{
    [Key]
    public int StickerId { get; set; }
    // public int SenderId { get; set; }
    // public int RecipientId { get; set; }
    public long MessageId { get; set; }                     // FK -> Message
    
    public DateTime SentAt { get; set; }                    // DATETIME2
    
    [MaxLength(500)]
    public string StickerUrl { get; set; }                  // NVARCHAR(500)
    
    [MaxLength(50)]
    public string StickerType { get; set; }                 // NVARCHAR(50)
    
    [ForeignKey(nameof(MessageId))]
    public Message Message { get; set; }
}