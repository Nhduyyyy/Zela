using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Zela.Models;

/// <summary>
/// Phiên whiteboard hợp tác.
/// </summary>
public class WhiteboardSession
{
    [Key]
    public Guid WbSessionId { get; set; }          // PK: UNIQUEIDENTIFIER

    public int RoomId { get; set; }                // FK -> VideoRoom (hoặc ChatGroup tùy thiết kế)
    public DateTime CreatedAt { get; set; }        // DATETIME

    [ForeignKey(nameof(RoomId))]
    public VideoRoom VideoRoom { get; set; }
    public ICollection<DrawAction> DrawActions { get; set; }
}