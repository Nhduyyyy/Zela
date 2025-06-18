using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Zela.Models;

/// <summary>
/// Tập tin media kèm theo tin nhắn.
/// </summary>
public class Media
{
    [Key]
    public Guid MediaId { get; set; }              // PK: UNIQUEIDENTIFIER

    public long MessageId { get; set; }            // FK -> Message
    public DateTime UploadedAt { get; set; }       // DATETIME2

    [MaxLength(500)]
    public string Url { get; set; }                // NVARCHAR(500)

    [MaxLength(255)]
    public string MediaType { get; set; }          // NVARCHAR(50)

    [ForeignKey(nameof(MessageId))]
    public Message Message { get; set; }
}