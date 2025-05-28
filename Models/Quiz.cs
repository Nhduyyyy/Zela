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

    [MaxLength(200)]
    public string Title { get; set; }              // NVARCHAR(200)

    public string Description { get; set; }        // NVARCHAR(MAX)
    public int CreatorId { get; set; }             // FK -> User
    public DateTime CreatedAt { get; set; }        // DATETIME2
    public int TimeLimit { get; set; }             // INT (phút giây)

    [ForeignKey(nameof(CreatorId))]
    public User Creator { get; set; }
    public ICollection<QuizQuestion> Questions { get; set; }
    public ICollection<QuizAttempt> Attempts { get; set; }
}
