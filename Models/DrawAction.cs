using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Zela.Models;

/// <summary>
/// Hành động vẽ trên whiteboard.
/// </summary>
public class DrawAction
{
    [Key]
    public long ActionId { get; set; }             // PK: BIGINT

    public Guid WbSessionId { get; set; }          // FK -> WhiteboardSession
    public int UserId { get; set; }                // FK -> User

    [MaxLength(50)]
    public string ActionType { get; set; }         // NVARCHAR(50)
    public string Payload { get; set; }            // NVARCHAR(MAX)
    public DateTime Timestamp { get; set; }        // DATETIME2

    [ForeignKey(nameof(WbSessionId))]
    public WhiteboardSession WhiteboardSession { get; set; }
    [ForeignKey(nameof(UserId))]
    public User User { get; set; }
}