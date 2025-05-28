using System.ComponentModel.DataAnnotations.Schema;

namespace Zela.Models;

/// <summary>
/// Liên kết User tham gia VideoRoom.
/// </summary>
public class RoomParticipant
{
    public int RoomId { get; set; }                // FK -> VideoRoom
    public int UserId { get; set; }                // FK -> User

    public bool IsModerator { get; set; }          // BIT
    public DateTime JoinedAt { get; set; }         // DATETIME2

    [ForeignKey(nameof(RoomId))]
    public VideoRoom VideoRoom { get; set; }
    [ForeignKey(nameof(UserId))]
    public User User { get; set; }
}