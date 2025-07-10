using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Zela.Models
{
    public class QuizAttemptDetail
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public Guid AttemptId { get; set; } // FK -> QuizAttempt

        [Required]
        public int QuestionId { get; set; } // FK -> QuizQuestion

        [Required]
        public string Answer { get; set; } = string.Empty;
        public bool IsCorrect { get; set; } = false; // Lưu trạng thái đúng/sai
        public double TimeTaken { get; set; } = 0.0; // Lưu thời gian trả lời (giây)

        [ForeignKey(nameof(AttemptId))]
        public virtual QuizAttempt Attempt { get; set; }

        [ForeignKey(nameof(QuestionId))]
        public virtual QuizQuestion Question { get; set; }
    }
}