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

    [Required(ErrorMessage = "Nội dung câu hỏi không được để trống")]
    public string Content { get; set; }            // NVARCHAR(MAX)

    [Required(ErrorMessage = "Loại câu hỏi không được để trống")]
    [MaxLength(50)]
    public string QuestionType { get; set; }       // NVARCHAR(50)

    // Choices chỉ bắt buộc với MultipleChoice, validate ở service/controller
    public string Choices { get; set; }            // NVARCHAR(MAX)

    // AnswerKey bắt buộc với các loại không phải Essay, validate ở service/controller
    public string AnswerKey { get; set; }          // NVARCHAR(MAX)

    [ForeignKey(nameof(QuizId))]
    public virtual Quiz Quiz { get; set; }
}