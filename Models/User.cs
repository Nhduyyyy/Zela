using System.ComponentModel.DataAnnotations;

namespace Zela.Models;

/// <summary>
/// Đại diện cho tài khoản người dùng.
/// </summary>
public class User
{
    [Key]
    public int UserId { get; set; }                // PK: UserId INT IDENTITY(1,1)

    public bool IsPremium { get; set; }            // BIT
    public DateTime CreatedAt { get; set; }        // DATETIME
    public DateTime LastLoginAt { get; set; }      // DATETIME

    [Required, MaxLength(255)]
    public string Email { get; set; }              // VARCHAR(255)

    [MaxLength(100)]
    public string FullName { get; set; }           // NVARCHAR(100)

    [MaxLength(500)]
    public string AvatarUrl { get; set; }          // NVARCHAR(500)

    // Navigation properties
    public ICollection<Role> Roles { get; set; }
    public ICollection<Friendship> SentFriendships { get; set; }
    public ICollection<Friendship> ReceivedFriendships { get; set; }
    public ICollection<GroupMember> GroupMemberships { get; set; }
    public ICollection<Message> SentMessages { get; set; }
    public ICollection<Message> ReceivedMessages { get; set; }
    public ICollection<CalendarEvent> CalendarEvents { get; set; }
    public ICollection<Notification> Notifications { get; set; }
    public ICollection<AnalyticsEvent> AnalyticsEvents { get; set; }
    public ICollection<RoomParticipant> RoomParticipations { get; set; }
    public ICollection<Attendance> Attendances { get; set; }
    public ICollection<QuizAttempt> QuizAttempts { get; set; }
    public ICollection<DrawAction> DrawActions { get; set; }
}