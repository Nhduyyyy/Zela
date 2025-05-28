using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Zela.Models;

/// <summary>
/// Câu hỏi trong Quiz.
/// </summary>
public class QuizQuestion
{
    [Key]
    public int QuestionId { get; set; }            // PK: INT

    public int QuizId { get; set; }                // FK -> Quiz
    public string Content { get; set; }            // NVARCHAR(MAX)

    [MaxLength(50)]
    public string QuestionType { get; set; }       // NVARCHAR(50)
    public string Choices { get; set; }            // NVARCHAR(MAX)
    public string AnswerKey { get; set; }          // NVARCHAR(MAX)

    [ForeignKey(nameof(QuizId))]
    public Quiz Quiz { get; set; }
}