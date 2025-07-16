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

    public int? RoomId { get; set; }               // FK -> VideoRoom (nullable for standalone sessions)
    public int? CreatedByUserId { get; set; }      // FK -> User (who created the session)
    public string? SessionName { get; set; }       // Optional session name
    public DateTime CreatedAt { get; set; }        // DATETIME
    public DateTime? UpdatedAt { get; set; }       // Last save time
    public int? LastSavedBy { get; set; }          // FK -> User (who last saved)

    [ForeignKey(nameof(RoomId))]
    public VideoRoom? VideoRoom { get; set; }
    
    [ForeignKey(nameof(CreatedByUserId))]
    public User? CreatedByUser { get; set; }
    
    [ForeignKey(nameof(LastSavedBy))]
    public User? LastSavedByUser { get; set; }
    
    public ICollection<DrawAction> DrawActions { get; set; }
}