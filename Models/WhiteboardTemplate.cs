using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Zela.Models;

/// <summary>
/// Template whiteboard để tái sử dụng.
/// </summary>
public class WhiteboardTemplate
{
    [Key]
    public int Id { get; set; }                    // PK: INT

    [Required]
    [MaxLength(100)]
    public string Name { get; set; }               // NVARCHAR(100)

    [Required]
    public string Data { get; set; }               // NVARCHAR(MAX) - JSON data

    public int CreatedByUserId { get; set; }       // FK -> User
    public DateTime CreatedAt { get; set; }        // DATETIME2
    public bool IsPublic { get; set; } = false;    // BIT - Template công khai hay riêng tư
    public string? Description { get; set; }       // NVARCHAR(500) - Mô tả template
    public string? Thumbnail { get; set; }         // NVARCHAR(MAX) - Base64 thumbnail

    [ForeignKey(nameof(CreatedByUserId))]
    public User CreatedByUser { get; set; }
} 