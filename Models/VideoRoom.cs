using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Zela.Models;

/// <summary>
/// Phòng video call.
/// </summary>
public class VideoRoom
{
    [Key]
    public int RoomId { get; set; }                // PK: INT IDENTITY(1,1)

    public int CreatorId { get; set; }             // FK -> User
    public bool IsOpen { get; set; }               // BIT
    public DateTime CreatedAt { get; set; }        // DATETIME2

    [MaxLength(50)]
    public string Name { get; set; }               // NVARCHAR(50) NULL
    [MaxLength(50)]
    public string Password { get; set; }           // NVARCHAR(50) NULL

    [ForeignKey(nameof(CreatorId))]
    public User Creator { get; set; }

    public ICollection<RoomParticipant> Participants { get; set; }
    public ICollection<CallSession> CallSessions { get; set; }
}