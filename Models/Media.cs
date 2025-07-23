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
    public string MediaType { get; set; }          // NVARCHAR(255)

    [MaxLength(255)]
    public string? FileName { get; set; }          // NVARCHAR(255) - Tên file gốc

    [ForeignKey(nameof(MessageId))]
    public Message Message { get; set; }
}