using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Zela.Models;

/// <summary>
/// Bộ đề quiz.
/// </summary>
public class Quiz
{
    [Key]
    public int QuizId { get; set; }                // PK: INT

    [Required(ErrorMessage = "Tiêu đề quiz là bắt buộc")]
    [MaxLength(200, ErrorMessage = "Tiêu đề không được vượt quá 200 ký tự")]
    public string Title { get; set; }              // NVARCHAR(200)

    [Required(ErrorMessage = "Mô tả quiz là bắt buộc")]
    public string Description { get; set; }        // NVARCHAR(MAX)
    
    public int? CreatorId { get; set; }            // FK -> User (nullable)
    public DateTime CreatedAt { get; set; }        // DATETIME2
    
    [Range(0, 300, ErrorMessage = "Thời gian làm bài phải từ 0 đến 300 phút")]
    public int TimeLimit { get; set; }             // INT (phút giây)

    // Thêm các trường cho bài tập về nhà
    public bool IsHomework { get; set; } = false;
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }

    public bool IsPublic { get; set; } = true; // Quiz công khai hay riêng tư
    public string? Password { get; set; } // Mật khẩu nếu là private

    [ForeignKey(nameof(CreatorId))]
    public virtual User? Creator { get; set; }
    public virtual ICollection<QuizQuestion>? Questions { get; set; }
    public virtual ICollection<QuizAttempt>? Attempts { get; set; }
    
}