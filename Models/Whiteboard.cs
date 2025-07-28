using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Zela.Models;

/// <summary>
/// Bảng trắng (Whiteboard) - nơi người dùng có thể vẽ và ghi chú
/// </summary>
public class Whiteboard
{
    [Key]
    public int WhiteboardId { get; set; }

    [Required, MaxLength(200)]
    public string Title { get; set; }

    [MaxLength(1000)]
    public string Description { get; set; }

    public int CreatorId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsPublic { get; set; } = false;
    public bool IsTemplate { get; set; } = false;

    // Navigation properties
    [ForeignKey(nameof(CreatorId))]
    public virtual User Creator { get; set; }
    public virtual ICollection<WhiteboardSession> Sessions { get; set; } = new List<WhiteboardSession>();
} 