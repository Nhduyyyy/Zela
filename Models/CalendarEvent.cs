using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Zela.Models;

/// <summary>
/// Sự kiện lịch (CalendarEvent) của User.
/// </summary>
public class CalendarEvent
{
    [Key]
    public int CalendarEventId { get; set; }       // PK: INT IDENTITY(1,1)

    public int UserId { get; set; }                // FK -> User
    public DateTime StartAt { get; set; }          // DATETIME
    public DateTime EndAt { get; set; }            // DATETIME

    [MaxLength(200)]
    public string Title { get; set; }              // NVARCHAR(200)

    [MaxLength(200)]
    public string Location { get; set; }           // NVARCHAR(200) NULL

    [ForeignKey(nameof(UserId))]
    public User User { get; set; }
}