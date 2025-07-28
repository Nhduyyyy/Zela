using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Zela.Models;

/// <summary>
/// Phiên làm việc trên bảng trắng - lưu trữ dữ liệu vẽ và thao tác
/// </summary>
public class WhiteboardSession
{
    [Key]
    public int SessionId { get; set; }

    public int WhiteboardId { get; set; }
    public int? RoomId { get; set; } // Nếu được tạo từ video room
    public DateTime CreatedAt { get; set; }
    public DateTime? LastModifiedAt { get; set; }
    public bool IsActive { get; set; } = true;

    // Dữ liệu canvas (JSON format)
    [Column(TypeName = "nvarchar(max)")]
    public string CanvasData { get; set; } // Lưu trữ tất cả các đối tượng vẽ

    // Thumbnail preview
    [MaxLength(500)]
    public string ThumbnailUrl { get; set; } = string.Empty;

    // Navigation properties
    [ForeignKey(nameof(WhiteboardId))]
    public virtual Whiteboard Whiteboard { get; set; }

    [ForeignKey(nameof(RoomId))]
    public virtual VideoRoom Room { get; set; }
} 