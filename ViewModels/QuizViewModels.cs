using System.ComponentModel.DataAnnotations;

namespace Zela.ViewModels;

/// <summary>
/// ViewModel cho danh sách quiz
/// </summary>
public class QuizzesViewModel
{
    public int QuizId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int TermCount { get; set; }
    public string Author { get; set; } = string.Empty;
    public string IconUrl { get; set; } = string.Empty;
    public bool IsPublic { get; set; } = true;
    public string? Password { get; set; }
    public int CreatorId { get; set; } // Thêm dòng này để view so sánh
}

/// <summary>
/// ViewModel cho việc chỉnh sửa quiz
/// </summary>
public class QuizEditViewModel
{
    public int QuizId { get; set; }
    
    [Required(ErrorMessage = "Tiêu đề quiz là bắt buộc")]
    [MaxLength(200, ErrorMessage = "Tiêu đề không được vượt quá 200 ký tự")]
    public string Title { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Mô tả quiz là bắt buộc")]
    public string Description { get; set; } = string.Empty;
    
    [Range(0, 300, ErrorMessage = "Thời gian làm bài phải từ 0 đến 300 phút")]
    public int TimeLimit { get; set; }
    
    public int? CreatorId { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Display properties (read-only)
    public string? CreatorName { get; set; }
    public int QuestionCount { get; set; }
    public bool IsPublic { get; set; } = true;
    public string? Password { get; set; }
}

/// <summary>
/// ViewModel cho danh sách attempt của quiz
/// </summary>
public class QuizAttemptListItemViewModel
{
    public Guid AttemptId { get; set; }
    public int? UserId { get; set; }
    public string UserFullName { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime EndedAt { get; set; }
    public float Score { get; set; }
}

/// <summary>
/// ViewModel cho chi tiết attempt của quiz
/// </summary>
public class QuizAttemptDetailViewModel
{
    public string QuestionContent { get; set; } = string.Empty;
    public string QuestionType { get; set; } = string.Empty;
    public string Choices { get; set; } = string.Empty;
    public string CorrectAnswer { get; set; } = string.Empty;
    public string UserAnswer { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
}

/// <summary>
/// ViewModel cho thống kê quiz
/// </summary>
public class QuizStatisticsViewModel
{
    public int QuizId { get; set; }
    public string QuizTitle { get; set; } = string.Empty;
    public int TotalAttempts { get; set; }
    public int UniqueUsers { get; set; }
    public int MaxAttemptsPerUser { get; set; }
    public float AverageScore { get; set; }
    public float MaxScore { get; set; }
    public float MinScore { get; set; }
    public List<float> ScoreDistribution { get; set; } = new();
    public List<QuestionStatistics> QuestionStats { get; set; } = new();
}

/// <summary>
/// ViewModel cho thống kê câu hỏi
/// </summary>
public class QuestionStatistics
{
    public int QuestionId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string QuestionType { get; set; } = string.Empty;
    public float CorrectRate { get; set; } // Tỷ lệ đúng
    public int TotalAnswers { get; set; }
    public int CorrectAnswers { get; set; }
}

public class QuizResultViewModel
{
    public Guid AttemptId { get; set; }
    public int QuizId { get; set; }
    public string QuizTitle { get; set; } = string.Empty;
    public string QuizDescription { get; set; } = string.Empty;
    public float Score { get; set; }
    public int TotalQuestions { get; set; }
    public int CorrectAnswers { get; set; }
    public int Duration { get; set; } // Thời gian làm bài (giây)
    public DateTime StartedAt { get; set; }
    public DateTime EndedAt { get; set; }
    public string UserName { get; set; } = string.Empty;
    public List<QuizResultDetailViewModel> Details { get; set; } = new();
    public bool ShowScoreAfterSubmit { get; set; }
    public int EssayQuestionsCount { get; set; }
    public string Grade { get; set; } = string.Empty; // Xếp loại: Xuất sắc, Giỏi, Khá, Trung bình, Yếu
    public string GradeColor { get; set; } = string.Empty; // Màu sắc cho xếp loại
}

public class QuizResultDetailViewModel
{
    public int QuestionNumber { get; set; }
    public string QuestionContent { get; set; } = string.Empty;
    public string QuestionType { get; set; } = string.Empty;
    public string CorrectAnswer { get; set; } = string.Empty;
    public string UserAnswer { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public string Status { get; set; } = string.Empty; // "Đúng", "Sai", "Chưa trả lời"
    public string StatusColor { get; set; } = string.Empty;
} 