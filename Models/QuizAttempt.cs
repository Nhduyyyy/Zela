using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Zela.Models;

/// <summary>
/// Lần thử làm quiz của User.
/// </summary>
public class QuizAttempt
{
    [Key]
    public Guid AttemptId { get; set; }            // PK: UNIQUEIDENTIFIER

    public int QuizId { get; set; }                // FK -> Quiz
    public int? UserId { get; set; }               // FK -> User (nullable)
    public DateTime StartedAt { get; set; }        // DATETIME2
    public DateTime EndedAt { get; set; }          // DATETIME2
    public float Score { get; set; }               // FLOAT
    public string DisplayName { get; set; } = string.Empty; // Lưu tên học sinh/sinh viên nếu không có UserId
    public string? Comment { get; set; } // Nhận xét/phản hồi của creator

    [ForeignKey(nameof(QuizId))]
    public Quiz Quiz { get; set; }
    [ForeignKey(nameof(UserId))]
    public User User { get; set; }

    // Navigation property tới chi tiết đáp án
    public virtual ICollection<QuizAttemptDetail> Details { get; set; }
}