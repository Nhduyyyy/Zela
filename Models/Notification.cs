using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Zela.Models;

/// <summary>
/// Thông báo (notification) gửi đến User.
/// </summary>
public class Notification
{
    [Key]
    public int NotificationId { get; set; }        // PK: INT IDENTITY(1,1)

    public int UserId { get; set; }                // FK -> User
    public DateTime CreatedAt { get; set; }        // DATETIME
    public bool IsRead { get; set; }               // BIT

    [MaxLength(50)]
    public string Type { get; set; }               // NVARCHAR(50)

    public string Content { get; set; }            // NVARCHAR(MAX)

    [ForeignKey(nameof(UserId))]
    public User User { get; set; }
}