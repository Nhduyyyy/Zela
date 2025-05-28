using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Zela.Models;

/// <summary>
/// Sự kiện phân tích (analytics) do người dùng tạo.
/// </summary>
public class AnalyticsEvent
{
    [Key]
    public int AnalyticsEventId { get; set; }      // PK: INT IDENTITY(1,1)

    public int UserId { get; set; }                // FK -> User
    public DateTime OccurredAt { get; set; }       // DATETIME

    [MaxLength(50)]
    public string EventType { get; set; }          // NVARCHAR(50)

    public string Metadata { get; set; }           // NVARCHAR(MAX)

    [ForeignKey(nameof(UserId))]
    public User User { get; set; }
}