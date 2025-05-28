using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Zela.Models;

/// <summary>
/// Bản ghi tham gia (Attendance) của User vào CallSession.
/// </summary>
public class Attendance
{
    [Key]
    public int RecordId { get; set; }              // PK: INT IDENTITY(1,1)

    public Guid SessionId { get; set; }            // FK -> CallSession
    public int UserId { get; set; }                // FK -> User
    public DateTime JoinTime { get; set; }         // DATETIME2
    public DateTime? LeaveTime { get; set; }       // DATETIME2 NULL

    [ForeignKey(nameof(SessionId))]
    public CallSession CallSession { get; set; }
    [ForeignKey(nameof(UserId))]
    public User User { get; set; }
}