using Zela.DbContext;
using Zela.Models;
using Microsoft.EntityFrameworkCore;
using Zela.ViewModels;
using System.Text.Json;
using Xceed.Words.NET;
namespace Zela.Services;

public class QuizService : IQuizService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<QuizService> _logger;

    // Memory lưu trạng thái phòng quiz real-time
    private static readonly Dictionary<string, RoomState> _quizRooms = new();
    private class RoomState
    {
        public Dictionary<string, PlayerState> Players { get; set; } = new(); // key: displayName
        public Dictionary<int, QuestionState> Questions { get; set; } = new(); // key: questionId
        public int CurrentQuestionIndex { get; set; } = 0;
        public DateTime? QuestionStartTime { get; set; }
    }
    private class PlayerState
    {
        public string DisplayName { get; set; }
        public int CorrectCount { get; set; } = 0;
        public double TotalTime { get; set; } = 0;
        public Dictionary<int, AnswerState> Answers { get; set; } = new(); // key: questionId
    }
    private class AnswerState
    {
        public string Answer { get; set; }
        public bool IsCorrect { get; set; }
        public double TimeTaken { get; set; }
    }
    private class QuestionState
    {
        public int QuestionId { get; set; }
        public DateTime? StartTime { get; set; }
    }

    public QuizService(ApplicationDbContext dbContext, IHttpContextAccessor httpContextAccessor, ILogger<QuizService> logger)
    {
        _dbContext = dbContext;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    // Helper method để lấy UserId từ session
    public int? GetCurrentUserId()
    {
        return _httpContextAccessor.HttpContext?.Session.GetInt32("UserId");
    }

    // Helper method để chuyển đổi Quiz sang QuizEditViewModel
    public QuizEditViewModel ConvertToEditViewModel(Quiz quiz)
    {
        return new QuizEditViewModel
        {
            QuizId = quiz.QuizId,
            Title = quiz.Title,
            Description = quiz.Description,
            TimeLimit = quiz.TimeLimit,
            CreatorId = quiz.CreatorId,
            CreatedAt = quiz.CreatedAt,
            CreatorName = quiz.Creator?.FullName,
            QuestionCount = quiz.Questions?.Count ?? 0
        };
    }

    // Helper method để chuyển đổi QuizEditViewModel sang Quiz
    public Quiz ConvertFromEditViewModel(QuizEditViewModel viewModel)
    {
        return new Quiz
        {
            QuizId = viewModel.QuizId,
            Title = viewModel.Title,
            Description = viewModel.Description,
            TimeLimit = viewModel.TimeLimit,
            CreatorId = viewModel.CreatorId,
            CreatedAt = viewModel.CreatedAt
        };
    }

    public List<QuizzesViewModel> GetAll()
    {
        try
        {
        return _dbContext.Quizzes
            .Include(q => q.Creator)
            .Include(q => q.Questions)
            .OrderByDescending(q => q.CreatedAt)
            .Select(q => new QuizzesViewModel
            {
                QuizId = q.QuizId,
                Title = q.Title,
                Description = q.Description ?? "",
                TermCount = q.Questions.Count,
                Author = q.Creator != null ? q.Creator.FullName : "Anonymous",
                IconUrl = "/images/default-quiz-icon.png"
            })
            .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all quizzes: {Message}", ex.Message);
            throw new InvalidOperationException("Có lỗi xảy ra khi lấy danh sách quiz: " + ex.Message);
        }
    }

    public Quiz GetById(int id)
    {
        try
        {
            return _dbContext.Quizzes
                .Include(q => q.Creator)
                .Include(q => q.Questions)
                .AsNoTracking() // Prevent tracking to improve performance
                .FirstOrDefault(q => q.QuizId == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting quiz by ID {QuizId}: {Message}", id, ex.Message);
            throw new InvalidOperationException($"Có lỗi xảy ra khi lấy thông tin quiz: {ex.Message}");
        }
    }

    public int Create(Quiz quiz)
    {
        try
        {
            _logger.LogInformation("Creating quiz: Title={Title}, Description={Description}", quiz.Title, quiz.Description);
            // Validate input
            if (string.IsNullOrWhiteSpace(quiz.Title))
                throw new ArgumentException("Tiêu đề quiz không được để trống");
            if (string.IsNullOrWhiteSpace(quiz.Description))
                throw new ArgumentException("Mô tả quiz không được để trống");
            if (!quiz.IsPublic && string.IsNullOrWhiteSpace(quiz.Password))
                throw new ArgumentException("Quiz riêng tư phải có mật khẩu");
            quiz.CreatedAt = DateTime.UtcNow;
            var currentUserId = GetCurrentUserId();
            _logger.LogInformation("Current UserId from session: {UserId}", currentUserId);
            if (!currentUserId.HasValue)
            {
                _logger.LogError("No UserId found in session");
                throw new InvalidOperationException("Bạn phải đăng nhập để tạo quiz");
            }
            quiz.CreatorId = currentUserId.Value;
            _logger.LogInformation("Setting CreatorId to: {CreatorId}", quiz.CreatorId);
            // Lưu IsPublic và Password (nếu có)
            // (Đã có sẵn trong quiz object)
            _dbContext.Quizzes.Add(quiz);
            _dbContext.SaveChanges();
            _logger.LogInformation("Quiz saved to database with ID: {QuizId}", quiz.QuizId);
            return quiz.QuizId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating quiz: {Message}", ex.Message);
            throw new InvalidOperationException($"Có lỗi xảy ra khi tạo quiz: {ex.Message}");
        }
    }

    public void Update(Quiz quiz)
    {
        try
        {
            _logger.LogInformation("Updating quiz: QuizId={QuizId}, Title={Title}", quiz.QuizId, quiz.Title);
            if (quiz == null)
                throw new ArgumentNullException(nameof(quiz), "Quiz object cannot be null");
            if (string.IsNullOrWhiteSpace(quiz.Title))
                throw new ArgumentException("Tiêu đề quiz không được để trống");
            var existingQuiz = _dbContext.Quizzes.FirstOrDefault(q => q.QuizId == quiz.QuizId);
            if (existingQuiz == null)
                throw new InvalidOperationException($"Không tìm thấy quiz với ID: {quiz.QuizId}");
            existingQuiz.Title = quiz.Title?.Trim();
            existingQuiz.Description = quiz.Description?.Trim();
            existingQuiz.TimeLimit = quiz.TimeLimit;
            _dbContext.SaveChanges();
            _logger.LogInformation("Quiz {QuizId} updated successfully", quiz.QuizId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating quiz: {Message}", ex.Message);
            throw new InvalidOperationException($"Có lỗi xảy ra khi cập nhật quiz: {ex.Message}");
        }
    }

    public async Task UpdateAsync(Quiz quiz)
    {
        try
        {
            _logger.LogInformation("Updating quiz async: QuizId={QuizId}, Title={Title}", quiz.QuizId, quiz.Title);
        
        if (quiz == null)
        {
            throw new ArgumentNullException(nameof(quiz), "Quiz object cannot be null");
        }
        
            // Validate input
            if (string.IsNullOrWhiteSpace(quiz.Title))
        {
                throw new ArgumentException("Tiêu đề quiz không được để trống");
        }
            
            // Load the existing quiz from database
            var existingQuiz = await _dbContext.Quizzes
                .FirstOrDefaultAsync(q => q.QuizId == quiz.QuizId);
                
            if (existingQuiz == null)
            {
                _logger.LogError("Quiz not found with ID: {QuizId}", quiz.QuizId);
                throw new InvalidOperationException($"Không tìm thấy quiz với ID: {quiz.QuizId}");
            }
            
            // Kiểm tra quyền sở hữu
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                _logger.LogError("No UserId found in session");
                throw new InvalidOperationException("Bạn phải đăng nhập để cập nhật quiz");
            }
            
            if (existingQuiz.CreatorId != currentUserId.Value)
            {
                _logger.LogError("User {UserId} does not have permission to update quiz {QuizId} owned by {OwnerId}", 
                    currentUserId.Value, quiz.QuizId, existingQuiz.CreatorId);
                throw new InvalidOperationException("Bạn không có quyền cập nhật quiz này");
            }
            
            // Update the existing quiz
            existingQuiz.Title = quiz.Title?.Trim();
            existingQuiz.Description = quiz.Description?.Trim();
            existingQuiz.TimeLimit = quiz.TimeLimit;
            
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Quiz {QuizId} updated successfully", quiz.QuizId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating quiz async: {Message}", ex.Message);
            throw;
        }
    }

    public void Delete(int id)
    {
        try
        {
            var quiz = _dbContext.Quizzes
                .Include(q => q.Questions)
                .Include(q => q.Attempts)
                .FirstOrDefault(q => q.QuizId == id);
            if (quiz == null)
                throw new InvalidOperationException($"Không tìm thấy quiz với ID: {id}");
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue || quiz.CreatorId != currentUserId.Value)
                throw new InvalidOperationException("Bạn không có quyền thao tác với quiz này");
            _dbContext.QuizQuestions.RemoveRange(quiz.Questions);
            _dbContext.QuizAttempts.RemoveRange(quiz.Attempts);
            _dbContext.Quizzes.Remove(quiz);
            _dbContext.SaveChanges();
            _logger.LogInformation("Quiz {QuizId} deleted successfully", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting quiz {QuizId}: {Message}", id, ex.Message);
            throw new InvalidOperationException($"Có lỗi xảy ra khi xóa quiz: {ex.Message}");
        }
    }

    public List<QuizQuestion> GetQuestions(int quizId)
    {
        try
        {
            return _dbContext.QuizQuestions
                .Where(q => q.QuizId == quizId)
                .OrderBy(q => q.QuestionId)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting questions for quiz {QuizId}: {Message}", quizId, ex.Message);
            throw new InvalidOperationException("Có lỗi xảy ra khi lấy danh sách câu hỏi: " + ex.Message);
        }
    }

    public QuizQuestion GetQuestionById(int questionId)
    {
        try
    {
        return _dbContext.QuizQuestions
            .FirstOrDefault(q => q.QuestionId == questionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting question by ID {QuestionId}: {Message}", questionId, ex.Message);
            throw new InvalidOperationException("Có lỗi xảy ra khi lấy thông tin câu hỏi: " + ex.Message);
        }
    }

    private void ValidateQuestion(QuizQuestion question)
    {
        _logger.LogInformation("Validating question: Type='{Type}', Content='{Content}', Choices='{Choices}', Answer='{Answer}'", 
            question.QuestionType, question.Content, question.Choices, question.AnswerKey);
            
        if (string.IsNullOrWhiteSpace(question.Content))
            throw new ArgumentException("Nội dung câu hỏi không được để trống");
        if (string.IsNullOrWhiteSpace(question.QuestionType))
            throw new ArgumentException("Loại câu hỏi không được để trống");
        
        // QuestionType đã được chuẩn hóa từ trước, không cần chuẩn hóa lại
        switch (question.QuestionType)
        {
            case "MultipleChoice":
                if (string.IsNullOrWhiteSpace(question.Choices))
                    throw new ArgumentException("Bạn phải nhập các lựa chọn cho câu hỏi trắc nghiệm");
                if (string.IsNullOrWhiteSpace(question.AnswerKey))
                    throw new ArgumentException("Bạn phải nhập đáp án cho câu hỏi trắc nghiệm");
                // Bổ sung: Đáp án đúng phải nằm trong các lựa chọn
                var choicesList = question.Choices.Split(new[] { '\n', '\r', ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(c => c.Trim()).ToList();
                if (!choicesList.Any(c => string.Equals(c, question.AnswerKey.Trim(), StringComparison.OrdinalIgnoreCase)))
                    throw new ArgumentException("Đáp án đúng phải nằm trong các lựa chọn của câu hỏi trắc nghiệm");
                _logger.LogInformation("MultipleChoice question validated successfully");
                break;
            case "TrueFalse":
                if (string.IsNullOrWhiteSpace(question.AnswerKey))
                    throw new ArgumentException("Bạn phải nhập đáp án cho câu hỏi đúng/sai");
                // Chấp nhận cả "Đúng"/"Sai" và "true"/"false"
                var answer = question.AnswerKey.Trim().ToLower();
                if (answer != "đúng" && answer != "sai" && answer != "true" && answer != "false")
                    throw new ArgumentException("Đáp án Đúng/Sai chỉ được là 'Đúng', 'Sai', 'true' hoặc 'false'");
                _logger.LogInformation("TrueFalse question validated successfully");
                break;
            case "ShortAnswer":
                if (string.IsNullOrWhiteSpace(question.AnswerKey))
                    throw new ArgumentException("Bạn phải nhập đáp án cho câu hỏi điền từ");
                _logger.LogInformation("ShortAnswer question validated successfully");
                break;
            case "Essay":
                // Không cần answerKey
                _logger.LogInformation("Essay question validated successfully");
                break;
            default:
                throw new ArgumentException($"Loại câu hỏi '{question.QuestionType}' không hợp lệ");
        }
    }

    public void AddQuestion(QuizQuestion question)
    {
        try
        {
            ValidateQuestion(question);
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                _logger.LogError("No UserId found in session");
                throw new InvalidOperationException("Bạn phải đăng nhập để tạo quiz");
            }
            
            // Kiểm tra quiz có tồn tại và user có quyền thêm câu hỏi không
            var quiz = _dbContext.Quizzes.FirstOrDefault(q => q.QuizId == question.QuizId);
            if (quiz == null)
            {
                throw new InvalidOperationException($"Không tìm thấy quiz với ID: {question.QuizId}");
            }
            
            if (quiz.CreatorId != currentUserId.Value)
            {
                throw new InvalidOperationException("Bạn không có quyền thêm câu hỏi vào quiz này");
            }
            
            _logger.LogInformation("Adding question to quiz {QuizId} by user {UserId}", 
                question.QuizId, currentUserId.Value);
            _dbContext.QuizQuestions.Add(question);
            _dbContext.SaveChanges();
            _logger.LogInformation("Question added successfully to quiz {QuizId}", question.QuizId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding question: {Message}", ex.Message);
            throw new InvalidOperationException($"Có lỗi xảy ra khi thêm câu hỏi: {ex.Message}");
        }
    }

    public void UpdateQuestion(QuizQuestion question)
    {
        try
        {
            ValidateQuestion(question);
            var existingQuestion = _dbContext.QuizQuestions.FirstOrDefault(q => q.QuestionId == question.QuestionId);
            if (existingQuestion == null)
                throw new InvalidOperationException($"Không tìm thấy câu hỏi với ID: {question.QuestionId}");
            
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
                throw new InvalidOperationException("Bạn phải đăng nhập để cập nhật câu hỏi");
            
            // Kiểm tra quyền sở hữu quiz
            var quiz = _dbContext.Quizzes.FirstOrDefault(q => q.QuizId == existingQuestion.QuizId);
            if (quiz == null || quiz.CreatorId != currentUserId.Value)
                throw new InvalidOperationException("Bạn không có quyền cập nhật câu hỏi này");
            
            existingQuestion.Content = question.Content;
            existingQuestion.QuestionType = question.QuestionType;
            existingQuestion.Choices = question.Choices;
            existingQuestion.AnswerKey = question.AnswerKey;
            _dbContext.SaveChanges();
            _logger.LogInformation("Question {QuestionId} updated successfully", question.QuestionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating question: {Message}", ex.Message);
            throw new InvalidOperationException($"Có lỗi xảy ra khi cập nhật câu hỏi: {ex.Message}");
        }
    }

    public void DeleteQuestion(int questionId)
    {
        try
        {
            var question = _dbContext.QuizQuestions.FirstOrDefault(q => q.QuestionId == questionId);
            if (question == null)
                throw new InvalidOperationException($"Không tìm thấy câu hỏi với ID: {questionId}");
            
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
                throw new InvalidOperationException("Bạn phải đăng nhập để xóa câu hỏi");
            
            // Kiểm tra quyền sở hữu quiz
            var quiz = _dbContext.Quizzes.FirstOrDefault(q => q.QuizId == question.QuizId);
            if (quiz == null || quiz.CreatorId != currentUserId.Value)
                throw new InvalidOperationException("Bạn không có quyền xóa câu hỏi này");
            
            _dbContext.QuizQuestions.Remove(question);
            _dbContext.SaveChanges();
            _logger.LogInformation("Question {QuestionId} deleted successfully", questionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting question {QuestionId}: {Message}", questionId, ex.Message);
            throw new InvalidOperationException($"Có lỗi xảy ra khi xóa câu hỏi: {ex.Message}");
        }
    }

    public List<QuizAttempt> GetAttempts(int quizId)
    {
        try
    {
        return _dbContext.QuizAttempts
            .Where(a => a.QuizId == quizId)
            .Include(a => a.User)
                .OrderByDescending(a => a.EndedAt)
            .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting attempts for quiz {QuizId}: {Message}", quizId, ex.Message);
            throw new InvalidOperationException("Có lỗi xảy ra khi lấy danh sách bài làm: " + ex.Message);
        }
    }

    public Guid SubmitAttempt(int quizId, Dictionary<string, object> answers, int duration, float score)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                throw new InvalidOperationException("Bạn phải đăng nhập để nộp bài");
            }
            // Kiểm tra quiz có tồn tại không
            var quiz = GetById(quizId);
            if (quiz == null)
            {
                throw new InvalidOperationException("Không tìm thấy quiz");
            }

            // Đảm bảo duration không âm và hợp lý
            var actualDuration = Math.Max(0, duration);
            if (actualDuration == 0)
            {
                _logger.LogWarning($"[SubmitAttempt] Duration = 0, có thể có lỗi từ frontend. Duration nhận được: {duration}");
            }

            var attempt = new QuizAttempt
            {
                AttemptId = Guid.NewGuid(),
                QuizId = quizId,
                UserId = currentUserId.Value,
                StartedAt = DateTime.UtcNow.AddSeconds(-actualDuration),
                EndedAt = DateTime.UtcNow,
                Score = score,
                DisplayName = string.Empty // Thêm giá trị mặc định cho DisplayName
            };

            _logger.LogInformation($"[SubmitAttempt] Thời gian: StartedAt={attempt.StartedAt}, EndedAt={attempt.EndedAt}, Duration={actualDuration}s");

            _logger.LogInformation($"[SubmitAttempt] Tạo QuizAttempt: AttemptId={attempt.AttemptId}, QuizId={quizId}, UserId={currentUserId.Value}, Score={score}");

            _dbContext.QuizAttempts.Add(attempt);
            
            try
            {
                _dbContext.SaveChanges();
                _logger.LogInformation($"[SubmitAttempt] Đã lưu QuizAttempt thành công");
            }
            catch (Exception saveEx)
            {
                _logger.LogError(saveEx, $"[SubmitAttempt] Lỗi khi lưu QuizAttempt: {saveEx.Message}");
                throw new InvalidOperationException($"Có lỗi xảy ra khi lưu bài làm: {saveEx.Message}");
            }

            // Log answers nhận được
            _logger.LogInformation($"[SubmitAttempt] Answers nhận được: {JsonSerializer.Serialize(answers)}");

            // Lưu chi tiết đáp án
            var detailsCount = 0;
            if (answers != null && answers.Count > 0)
            {
                _logger.LogInformation($"[SubmitAttempt] Bắt đầu lưu {answers.Count} câu trả lời");
                
                foreach (var entry in answers)
                {
                    int questionId;
                    if (int.TryParse(entry.Key, out questionId))
                    {
                        // Kiểm tra câu hỏi có tồn tại và thuộc quiz này không
                        var question = _dbContext.QuizQuestions.FirstOrDefault(q => q.QuestionId == questionId && q.QuizId == quizId);
                        if (question == null)
                        {
                            _logger.LogWarning($"[SubmitAttempt] Bỏ qua answer với questionId={questionId} vì không tìm thấy câu hỏi.");
                            continue; // Bỏ qua nếu không hợp lệ
                        }

                        // Tính toán đúng/sai
                        bool isCorrect = false;
                        if (!string.IsNullOrWhiteSpace(question.AnswerKey) && !string.IsNullOrWhiteSpace(entry.Value?.ToString()))
                        {
                            isCorrect = string.Equals(
                                entry.Value.ToString().Trim(), 
                                question.AnswerKey.Trim(), 
                                StringComparison.OrdinalIgnoreCase
                            );
                        }

                        var detail = new QuizAttemptDetail
                        {
                            AttemptId = attempt.AttemptId,
                            QuestionId = questionId,
                            Answer = entry.Value?.ToString() ?? string.Empty,
                            IsCorrect = isCorrect,
                            TimeTaken = 0.0 // Thêm giá trị mặc định
                        };
                        _dbContext.Add(detail);
                        detailsCount++;
                        _logger.LogInformation($"[SubmitAttempt] Đã tạo QuizAttemptDetail: AttemptId={attempt.AttemptId}, QuestionId={questionId}, Answer={detail.Answer}, IsCorrect={isCorrect}");
                    }
                    else
                    {
                        _logger.LogWarning($"[SubmitAttempt] Không parse được questionId từ key: {entry.Key}");
                    }
                }
                
                try
                {
                    _dbContext.SaveChanges();
                    _logger.LogInformation($"[SubmitAttempt] Đã lưu {detailsCount} QuizAttemptDetail thành công");
                }
                catch (Exception detailSaveEx)
                {
                    _logger.LogError(detailSaveEx, $"[SubmitAttempt] Lỗi khi lưu QuizAttemptDetail: {detailSaveEx.Message}");
                    throw new InvalidOperationException($"Có lỗi xảy ra khi lưu chi tiết đáp án: {detailSaveEx.Message}");
                }
            }
            else
            {
                _logger.LogWarning($"[SubmitAttempt] Không có answers hoặc answers rỗng. Answers: {JsonSerializer.Serialize(answers)}");
            }

            _logger.LogInformation("Attempt submitted successfully: {AttemptId} for quiz {QuizId} by user {UserId}", 
                attempt.AttemptId, quizId, currentUserId.Value);
            return attempt.AttemptId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting attempt: {Message}", ex.Message);
            throw;
        }
    }

    public class ServiceResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public object Data { get; set; }
    }

    public QuizEditViewModel GetEditViewModelForEdit(int quizId)
    {
        var quiz = GetById(quizId);
        if (quiz == null) return null;
        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue || quiz.CreatorId != currentUserId.Value)
            return null;
        return ConvertToEditViewModel(quiz);
    }

    public ServiceResult UpdateQuizFromEditViewModel(QuizEditViewModel viewModel)
    {
        try
        {
            if (viewModel == null)
                return new ServiceResult { Success = false, Message = "Dữ liệu quiz không hợp lệ" };
            if (string.IsNullOrWhiteSpace(viewModel.Title))
                return new ServiceResult { Success = false, Message = "Tiêu đề quiz không được để trống" };
            var quiz = _dbContext.Quizzes.FirstOrDefault(q => q.QuizId == viewModel.QuizId);
            if (quiz == null)
                return new ServiceResult { Success = false, Message = $"Không tìm thấy quiz với ID: {viewModel.QuizId}" };
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue || quiz.CreatorId != currentUserId.Value)
                return new ServiceResult { Success = false, Message = "Bạn không có quyền cập nhật quiz này" };
            quiz.Title = viewModel.Title?.Trim();
            quiz.Description = viewModel.Description?.Trim();
            quiz.TimeLimit = viewModel.TimeLimit;
            // Lưu IsPublic và Password
            quiz.IsPublic = viewModel.IsPublic;
            quiz.Password = viewModel.Password;
            _dbContext.SaveChanges();
            return new ServiceResult { Success = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating quiz from viewmodel: {Message}", ex.Message);
            return new ServiceResult { Success = false, Message = "Có lỗi xảy ra khi cập nhật quiz: " + ex.Message };
        }
    }

    public Quiz GetAddQuestionsViewModel(int quizId)
    {
        var quiz = GetById(quizId);
        var currentUserId = GetCurrentUserId();
        if (quiz == null || !currentUserId.HasValue || quiz.CreatorId != currentUserId.Value)
            return null;
        return quiz;
    }

    public ServiceResult AddQuestionFromViewModel(QuizQuestion question)
    {
        try
        {
            ValidateQuestion(question);
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                _logger.LogError("No UserId found in session");
                return new ServiceResult { Success = false, Message = "Bạn phải đăng nhập để thêm câu hỏi" };
            }
            
            // Kiểm tra quiz có tồn tại và user có quyền thêm câu hỏi không
            var quiz = _dbContext.Quizzes.FirstOrDefault(q => q.QuizId == question.QuizId);
            if (quiz == null)
            {
                return new ServiceResult { Success = false, Message = $"Không tìm thấy quiz với ID: {question.QuizId}" };
            }
            
            if (quiz.CreatorId != currentUserId.Value)
            {
                return new ServiceResult { Success = false, Message = "Bạn không có quyền thêm câu hỏi vào quiz này" };
            }
            
            _logger.LogInformation("Adding question to quiz {QuizId} by user {UserId}", 
                question.QuizId, currentUserId.Value);
            _dbContext.QuizQuestions.Add(question);
            _dbContext.SaveChanges();
            return new ServiceResult { Success = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding question from viewmodel: {Message}", ex.Message);
            return new ServiceResult { Success = false, Message = "Có lỗi xảy ra khi thêm câu hỏi: " + ex.Message };
        }
    }

    public ServiceResult UpdateQuestionFromViewModel(QuizQuestion question)
    {
        try
        {
            ValidateQuestion(question);
            var existingQuestion = _dbContext.QuizQuestions.FirstOrDefault(q => q.QuestionId == question.QuestionId);
            if (existingQuestion == null)
                return new ServiceResult { Success = false, Message = $"Không tìm thấy câu hỏi với ID: {question.QuestionId}" };
            
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
                return new ServiceResult { Success = false, Message = "Bạn phải đăng nhập để cập nhật câu hỏi" };
            
            // Kiểm tra quyền sở hữu quiz
            var quiz = _dbContext.Quizzes.FirstOrDefault(q => q.QuizId == existingQuestion.QuizId);
            if (quiz == null || quiz.CreatorId != currentUserId.Value)
                return new ServiceResult { Success = false, Message = "Bạn không có quyền cập nhật câu hỏi này" };
            
            existingQuestion.Content = question.Content;
            existingQuestion.QuestionType = question.QuestionType;
            existingQuestion.Choices = question.Choices;
            existingQuestion.AnswerKey = question.AnswerKey;
            _dbContext.SaveChanges();
            return new ServiceResult { Success = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating question from viewmodel: {Message}", ex.Message);
            return new ServiceResult { Success = false, Message = "Có lỗi xảy ra khi cập nhật câu hỏi: " + ex.Message };
        }
    }

    public ServiceResult DeleteQuestionById(int questionId)
    {
        try
        {
            var question = _dbContext.QuizQuestions.FirstOrDefault(q => q.QuestionId == questionId);
            if (question == null)
                return new ServiceResult { Success = false, Message = $"Không tìm thấy câu hỏi với ID: {questionId}" };
            
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
                return new ServiceResult { Success = false, Message = "Bạn phải đăng nhập để xóa câu hỏi" };
            
            // Kiểm tra quyền sở hữu quiz
            var quiz = _dbContext.Quizzes.FirstOrDefault(q => q.QuizId == question.QuizId);
            if (quiz == null || quiz.CreatorId != currentUserId.Value)
                return new ServiceResult { Success = false, Message = "Bạn không có quyền xóa câu hỏi này" };
            
            _dbContext.QuizQuestions.Remove(question);
            _dbContext.SaveChanges();
            return new ServiceResult { Success = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting question by id: {Message}", ex.Message);
            return new ServiceResult { Success = false, Message = "Có lỗi xảy ra khi xóa câu hỏi: " + ex.Message };
        }
    }

    public List<QuizAttemptListItemViewModel> GetAttemptListViewModel(int quizId)
    {
        var attempts = _dbContext.QuizAttempts
            .Where(a => a.QuizId == quizId)
            .Include(a => a.User)
            .OrderByDescending(a => a.EndedAt)
            .Select(a => new QuizAttemptListItemViewModel
            {
                AttemptId = a.AttemptId,
                UserId = a.UserId,
                UserFullName = a.User != null ? a.User.FullName : "(Khách)",
                StartedAt = a.StartedAt,
                EndedAt = a.EndedAt,
                Score = a.Score
            })
            .ToList();
        return attempts;
    }

    public List<QuizAttemptDetailViewModel> GetAttemptDetailViewModel(Guid attemptId, int quizId)
    {
        var details = _dbContext.QuizAttemptDetail
            .Where(d => d.AttemptId == attemptId)
            .Include(d => d.Question)
            .ToList();
        var result = details.Select(d => new QuizAttemptDetailViewModel
        {
            QuestionContent = d.Question.Content,
            QuestionType = d.Question.QuestionType,
            Choices = d.Question.Choices,
            CorrectAnswer = d.Question.AnswerKey,
            UserAnswer = d.Answer,
            IsCorrect = d.IsCorrect // Sử dụng giá trị đã lưu trong database
        }).ToList();
        return result;
    }

    public QuizStatisticsViewModel GetQuizStatistics(int quizId)
    {
        try
        {
            // Kiểm tra quiz có tồn tại không
            var quiz = GetById(quizId);
            if (quiz == null)
                throw new InvalidOperationException("Không tìm thấy quiz");
                
            // Kiểm tra quyền truy cập
            var currentUserId = GetCurrentUserId();
            if (quiz.CreatorId != currentUserId)
                throw new UnauthorizedAccessException("Bạn không có quyền xem thống kê của quiz này");

            var attempts = GetAttempts(quizId);
            if (!attempts.Any())
                return new QuizStatisticsViewModel 
                { 
                    QuizId = quizId,
                    QuizTitle = quiz.Title ?? "Unknown Quiz" 
                };

            var totalAttempts = attempts.Count;
            var uniqueUsers = attempts.Select(a => a.UserId).Distinct().Count();
            var maxAttemptsPerUser = attempts.GroupBy(a => a.UserId).Max(g => g.Count());
            var averageScore = attempts.Average(a => a.Score);
            var maxScore = attempts.Max(a => a.Score);
            var minScore = attempts.Min(a => a.Score);

            // Lấy thống kê chi tiết cho từng câu hỏi
            var questionStats = new List<QuestionStatistics>();
            var questions = GetQuestions(quizId);

            foreach (var question in questions)
            {
                var questionAttempts = _dbContext.QuizAttemptDetail
                    .Where(d => d.QuestionId == question.QuestionId && 
                              attempts.Select(a => a.AttemptId).Contains(d.AttemptId))
                    .ToList();

                var totalAnswers = questionAttempts.Count;
                var correctAnswers = questionAttempts.Count(d => d.IsCorrect);
                var correctRate = totalAnswers > 0 ? (float)correctAnswers / totalAnswers : 0;

                questionStats.Add(new QuestionStatistics
                {
                    QuestionId = question.QuestionId,
                    Content = question.Content,
                    QuestionType = question.QuestionType,
                    TotalAnswers = totalAnswers,
                    CorrectAnswers = correctAnswers,
                    CorrectRate = correctRate
                });
            }

            return new QuizStatisticsViewModel
            {
                QuizId = quizId,
                QuizTitle = quiz.Title ?? "Unknown Quiz",
                TotalAttempts = totalAttempts,
                UniqueUsers = uniqueUsers,
                MaxAttemptsPerUser = maxAttemptsPerUser,
                AverageScore = averageScore,
                MaxScore = maxScore,
                MinScore = minScore,
                ScoreDistribution = attempts.OrderBy(a => a.StartedAt).Select(a => a.Score).ToList(),
                QuestionStats = questionStats
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting quiz statistics: {Message}", ex.Message);
            throw new InvalidOperationException("Có lỗi xảy ra khi lấy thống kê quiz: " + ex.Message);
        }
    }

    public QuizResultViewModel GetQuizResult(Guid attemptId, int quizId)
    {
        try
        {
            // Kiểm tra quyền xem kết quả bài làm
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
                throw new InvalidOperationException("Bạn phải đăng nhập để xem kết quả bài làm");

            var attempt = _dbContext.QuizAttempts
                .Include(a => a.Quiz)
                .Include(a => a.User)
                .FirstOrDefault(a => a.AttemptId == attemptId && a.QuizId == quizId);

            if (attempt == null)
                throw new InvalidOperationException("Không tìm thấy bài làm");

            // Chỉ cho phép người tạo quiz hoặc người làm bài xem kết quả
            if (attempt.Quiz.CreatorId != currentUserId.Value && attempt.UserId != currentUserId.Value)
                throw new InvalidOperationException("Bạn không có quyền xem kết quả bài làm này");

            var details = _dbContext.QuizAttemptDetail
                .Include(d => d.Question)
                .Where(d => d.AttemptId == attemptId)
                .OrderBy(d => d.QuestionId)
                .ToList();

            var questions = GetQuestions(quizId);
            var essayQuestionsCount = questions.Count(q => q.QuestionType == "Essay");

            // Tính số câu đúng
            var correctAnswers = details.Count(d => d.IsCorrect);

            // Tính xếp loại
            var (grade, gradeColor) = CalculateGrade(attempt.Score);

            var resultDetails = new List<QuizResultDetailViewModel>();
            for (int i = 0; i < details.Count; i++)
            {
                var detail = details[i];
                var question = questions.FirstOrDefault(q => q.QuestionId == detail.QuestionId);
                if (question == null) continue;

                var (status, statusColor) = GetAnswerStatus(detail, question);
                
                resultDetails.Add(new QuizResultDetailViewModel
                {
                    QuestionNumber = i + 1,
                    QuestionContent = question.Content,
                    QuestionType = GetQuestionTypeName(question.QuestionType),
                    CorrectAnswer = question.AnswerKey ?? "",
                    UserAnswer = detail.Answer,
                    IsCorrect = detail.IsCorrect,
                    Status = status,
                    StatusColor = statusColor
                });
            }

            return new QuizResultViewModel
            {
                AttemptId = attempt.AttemptId,
                QuizId = attempt.QuizId,
                QuizTitle = attempt.Quiz?.Title ?? "",
                QuizDescription = attempt.Quiz?.Description ?? "",
                Score = attempt.Score,
                TotalQuestions = questions.Count,
                CorrectAnswers = correctAnswers,
                Duration = (int)(attempt.EndedAt - attempt.StartedAt).TotalSeconds,
                StartedAt = attempt.StartedAt,
                EndedAt = attempt.EndedAt,
                UserName = attempt.User?.FullName ?? attempt.DisplayName ?? "Unknown",
                Details = resultDetails,
                ShowScoreAfterSubmit = true, // Có thể lấy từ quiz settings
                EssayQuestionsCount = essayQuestionsCount,
                Grade = grade,
                GradeColor = gradeColor
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting quiz result: {Message}", ex.Message);
            throw new InvalidOperationException("Có lỗi xảy ra khi lấy kết quả bài làm: " + ex.Message);
        }
    }

    private (string grade, string color) CalculateGrade(float score)
    {
        return score switch
        {
            >= 90 => ("Xuất sắc", "success"),
            >= 80 => ("Giỏi", "info"),
            >= 70 => ("Khá", "primary"),
            >= 50 => ("Trung bình", "warning"),
            _ => ("Yếu", "danger")
        };
    }

    private (string status, string color) GetAnswerStatus(QuizAttemptDetail detail, QuizQuestion question)
    {
        if (string.IsNullOrEmpty(detail.Answer))
            return ("Chưa trả lời", "secondary");

        if (detail.IsCorrect)
            return ("Đúng", "success");

        return ("Sai", "danger");
    }

    private string GetQuestionTypeName(string questionType)
    {
        return questionType switch
        {
            "MultipleChoice" => "Trắc nghiệm",
            "TrueFalse" => "Đúng/Sai",
            "ShortAnswer" => "Câu trả lời ngắn",
            "Essay" => "Tự luận",
            _ => questionType
        };
    }

    public async Task DeleteQuestionsAsync(List<int> questionIds)
    {
        if (questionIds == null || !questionIds.Any())
            return;
            
        try
        {
            var questions = _dbContext.QuizQuestions.Where(q => questionIds.Contains(q.QuestionId)).ToList();
            if (!questions.Any())
                return;
                
            // Xóa các record QuizAttemptDetail trước (foreign key constraint)
            var attemptDetails = _dbContext.QuizAttemptDetail
                .Where(d => questionIds.Contains(d.QuestionId))
                .ToList();
            
            if (attemptDetails.Any())
            {
                _dbContext.QuizAttemptDetail.RemoveRange(attemptDetails);
                _logger.LogInformation("Deleted {Count} attempt details for selected questions", attemptDetails.Count);
            }
            
            // Xóa các câu hỏi
            _dbContext.QuizQuestions.RemoveRange(questions);
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Deleted {Count} questions", questions.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting questions: {Message}", ex.Message);
            throw new InvalidOperationException($"Có lỗi xảy ra khi xóa câu hỏi: {ex.Message}");
        }
    }

    public async Task DeleteAllQuestionsAsync(int quizId)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
                throw new InvalidOperationException("Bạn phải đăng nhập để xóa câu hỏi");

            var quiz = await _dbContext.Quizzes
                .Include(q => q.Questions)
                .FirstOrDefaultAsync(q => q.QuizId == quizId);

            if (quiz == null)
                throw new InvalidOperationException("Không tìm thấy quiz");

            if (quiz.CreatorId != currentUserId.Value)
                throw new InvalidOperationException("Bạn không có quyền xóa câu hỏi của quiz này");

            // Xóa tất cả QuizAttemptDetail trước
            var attemptDetails = await _dbContext.QuizAttemptDetail
                .Where(qad => quiz.Questions.Select(q => q.QuestionId).Contains(qad.QuestionId))
                .ToListAsync();
            _dbContext.QuizAttemptDetail.RemoveRange(attemptDetails);

            // Sau đó xóa tất cả câu hỏi
            _dbContext.QuizQuestions.RemoveRange(quiz.Questions);
            await _dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting all questions for quiz {QuizId}: {Message}", quizId, ex.Message);
            throw;
        }
    }

    // Thêm method xử lý upload file câu hỏi
    public async Task<ServiceResult> UploadQuestionsFromFileAsync(int quizId, IFormFile questionsFile)
    {
        try
        {
            if (questionsFile == null || questionsFile.Length == 0)
                return new ServiceResult { Success = false, Message = "Vui lòng chọn file câu hỏi." };

            var ext = System.IO.Path.GetExtension(questionsFile.FileName).ToLower();
            if (ext != ".csv" && ext != ".txt" && ext != ".docx")
                return new ServiceResult { Success = false, Message = "Chỉ hỗ trợ file CSV, TXT hoặc DOCX." };

            // Kiểm tra quyền sở hữu quiz
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
                return new ServiceResult { Success = false, Message = "Bạn phải đăng nhập để thêm câu hỏi" };

            var quiz = await _dbContext.Quizzes.FirstOrDefaultAsync(q => q.QuizId == quizId);
            if (quiz == null)
                return new ServiceResult { Success = false, Message = "Không tìm thấy quiz" };

            if (quiz.CreatorId != currentUserId.Value)
                return new ServiceResult { Success = false, Message = "Bạn không có quyền thêm câu hỏi cho quiz này" };

            int added = 0;
            int lineNumber = 0;
            if (ext == ".docx")
            {
                // Đọc file docx bằng DocX
                using (var stream = questionsFile.OpenReadStream())
                using (var ms = new System.IO.MemoryStream())
                {
                    await stream.CopyToAsync(ms);
                    ms.Position = 0;
                    using (var doc = DocX.Load(ms))
                    {
                        // Giả sử mỗi bảng là 1 danh sách câu hỏi, mỗi dòng là 1 câu hỏi
                        foreach (var table in doc.Tables)
                        {
                            foreach (var row in table.Rows.Skip(1)) // Bỏ header
                            {
                                lineNumber++;
                                try
                                {
                                    var cells = row.Cells;
                                    if (cells.Count < 2) continue;
                                    var content = cells[0].Paragraphs[0].Text.Trim();
                                    var questionType = cells[1].Paragraphs[0].Text.Trim();
                                    var choices = cells.Count > 2 ? cells[2].Paragraphs[0].Text.Trim() : "";
                                    var answerKey = cells.Count > 3 ? cells[3].Paragraphs[0].Text.Trim() : "";
                                    if (string.IsNullOrWhiteSpace(content) || string.IsNullOrWhiteSpace(questionType)) continue;
                                    questionType = NormalizeQuestionType(questionType);
                                    if (!string.IsNullOrWhiteSpace(choices) && questionType == "MultipleChoice")
                                    {
                                        choices = choices.Replace("\\n", "\n");
                                    }
                                    var question = new QuizQuestion
                                    {
                                        QuizId = quizId,
                                        Content = content,
                                        QuestionType = questionType,
                                        Choices = choices,
                                        AnswerKey = answerKey
                                    };
                                    ValidateQuestion(question);
                                    _dbContext.QuizQuestions.Add(question);
                                    added++;
                                }
                                catch (ArgumentException ex)
                                {
                                    _logger.LogWarning("Validation error on docx row {LineNumber}: {Message}", lineNumber, ex.Message);
                                    continue;
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                using (var stream = questionsFile.OpenReadStream())
                using (var reader = new System.IO.StreamReader(stream))
                {
                    string line;
                    while ((line = await reader.ReadLineAsync()) != null)
                    {
                        lineNumber++;
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        try
                        {
                            var parts = ParseCsvLine(line);
                            if (parts.Length < 2) continue;
                            var content = parts[0]?.Trim();
                            var questionType = parts[1]?.Trim();
                            var choices = parts.Length > 2 ? parts[2]?.Trim() : "";
                            var answerKey = parts.Length > 3 ? parts[3]?.Trim() : "";
                            if (string.IsNullOrWhiteSpace(content) || string.IsNullOrWhiteSpace(questionType)) continue;
                            questionType = NormalizeQuestionType(questionType);
                            if (!string.IsNullOrWhiteSpace(choices) && questionType == "MultipleChoice")
                            {
                                choices = choices.Replace("\\n", "\n");
                            }
                            var question = new QuizQuestion
                            {
                                QuizId = quizId,
                                Content = content,
                                QuestionType = questionType,
                                Choices = choices,
                                AnswerKey = answerKey
                            };
                            ValidateQuestion(question);
                            _dbContext.QuizQuestions.Add(question);
                            added++;
                        }
                        catch (ArgumentException ex)
                        {
                            _logger.LogWarning("Validation error on line {LineNumber}: {Message}", lineNumber, ex.Message);
                            continue;
                        }
                    }
                }
            }
            await _dbContext.SaveChangesAsync();
            return new ServiceResult { Success = true, Message = $"Đã thêm {added} câu hỏi từ file." };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading questions from file for quiz {QuizId}: {Message}", quizId, ex.Message);
            return new ServiceResult { Success = false, Message = "Lỗi khi đọc file: " + ex.Message };
        }
    }

    // Thêm method parse CSV line để xử lý dấu phẩy trong nội dung
    private string[] ParseCsvLine(string line)
    {
        var result = new List<string>();
        var current = "";
        bool inQuotes = false;
        
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(current.Trim('"')); // Loại bỏ dấu ngoặc kép
                current = "";
            }
            else
            {
                current += c;
            }
        }
        
        result.Add(current.Trim('"')); // Loại bỏ dấu ngoặc kép cho phần cuối cùng
        return result.ToArray();
    }

    // Thêm method chuẩn hóa QuestionType
    private string NormalizeQuestionType(string questionType)
    {
        if (string.IsNullOrWhiteSpace(questionType))
            return "MultipleChoice"; // Default

        var normalized = questionType.Trim().ToLower();
        
        switch (normalized)
        {
            case "multiplechoice":
            case "multiple-choice":
            case "multiple_choice":
            case "multiple choice":
                return "MultipleChoice";
            case "truefalse":
            case "true-false":
            case "true_false":
            case "true false":
                return "TrueFalse";
            case "shortanswer":
            case "short-answer":
            case "short_answer":
            case "short answer":
            case "fillintheblank":
            case "fill-in-the-blank":
            case "fill_in_the_blank":
            case "fill in the blank":
                return "ShortAnswer";
            case "essay":
                return "Essay";
            default:
                throw new ArgumentException($"Loại câu hỏi '{questionType}' không được hỗ trợ. Các loại được hỗ trợ: MultipleChoice, TrueFalse, ShortAnswer, Essay");
        }
    }

    // Thêm method xử lý submit attempt với validation
    public ServiceResult SubmitAttemptWithValidation(Dictionary<string, object> submission)
    {
        try
        {
            if (submission == null)
                return new ServiceResult { Success = false, Message = "Dữ liệu không hợp lệ" };

            if (!submission.ContainsKey("quizId") || !submission.ContainsKey("answers") || 
                !submission.ContainsKey("duration") || !submission.ContainsKey("score"))
                return new ServiceResult { Success = false, Message = "Thiếu thông tin cần thiết" };

            var quizId = ((System.Text.Json.JsonElement)submission["quizId"]).GetInt32();
            var duration = ((System.Text.Json.JsonElement)submission["duration"]).GetInt32();
            var score = ((System.Text.Json.JsonElement)submission["score"]).GetSingle();
            var answersElement = (System.Text.Json.JsonElement)submission["answers"];
            var answers = new Dictionary<string, object>();
            foreach (var prop in answersElement.EnumerateObject())
            {
                answers[prop.Name] = prop.Value.ValueKind == System.Text.Json.JsonValueKind.String ? prop.Value.GetString() : prop.Value.ToString();
            }

            var attemptId = SubmitAttempt(quizId, answers, duration, score);
            return new ServiceResult { Success = true, Message = "Nộp bài thành công!", Data = attemptId };
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "InvalidOperationException when submitting attempt: {Message}", ex.Message);
            return new ServiceResult { Success = false, Message = ex.Message };
        }
        catch (FormatException ex)
        {
            _logger.LogError(ex, "FormatException when submitting attempt: {Message}", ex.Message);
            return new ServiceResult { Success = false, Message = "Dữ liệu không đúng định dạng" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception when submitting attempt: {Message}", ex.Message);
            return new ServiceResult { Success = false, Message = "Có lỗi xảy ra khi nộp bài: " + ex.Message };
        }
    }

    // Thêm method xử lý delete questions với validation
    public async Task<ServiceResult> DeleteQuestionsWithValidationAsync(List<int> questionIds)
    {
        try
        {
            if (questionIds == null || !questionIds.Any())
                return new ServiceResult { Success = false, Message = "Danh sách câu hỏi không hợp lệ" };

            await DeleteQuestionsAsync(questionIds);
            return new ServiceResult { Success = true, Message = "Đã xóa các câu hỏi đã chọn." };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi xóa nhiều câu hỏi: {Message}", ex.Message);
            return new ServiceResult { Success = false, Message = ex.Message };
        }
    }

    // Thêm method xử lý delete all questions với validation
    public async Task<ServiceResult> DeleteAllQuestionsWithValidationAsync(int quizId)
    {
        try
        {
            if (quizId <= 0)
                return new ServiceResult { Success = false, Message = "QuizId không hợp lệ" };

            await DeleteAllQuestionsAsync(quizId);
            return new ServiceResult { Success = true, Message = "Đã xóa toàn bộ câu hỏi." };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi xóa toàn bộ câu hỏi: {Message}", ex.Message);
            return new ServiceResult { Success = false, Message = ex.Message };
        }
    }

    // Thêm method xử lý take quiz với validation
    public ServiceResult ValidateTakeQuiz(int quizId)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
                return new ServiceResult { Success = false, Message = "Bạn cần đăng nhập để làm bài" };

            var quiz = GetById(quizId);
            if (quiz == null)
                return new ServiceResult { Success = false, Message = "Không tìm thấy quiz" };

            if (quiz.Questions == null || !quiz.Questions.Any())
                return new ServiceResult { Success = false, Message = "Quiz này chưa có câu hỏi nào" };

            return new ServiceResult { Success = true, Data = quiz };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating take quiz for quiz {QuizId}: {Message}", quizId, ex.Message);
            return new ServiceResult { Success = false, Message = "Có lỗi xảy ra khi tải quiz" };
        }
    }

    // Thêm method xử lý get questions với validation
    public ServiceResult GetQuestionsWithValidation(int quizId)
    {
        try
        {
            var questions = GetQuestions(quizId);
            return new ServiceResult { Success = true, Data = questions };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting questions for quiz {QuizId}: {Message}", quizId, ex.Message);
            return new ServiceResult { Success = false, Message = "Có lỗi xảy ra khi lấy danh sách câu hỏi" };
        }
    }

    // Thêm method xử lý get question với validation
    public ServiceResult GetQuestionWithValidation(int questionId)
    {
        try
        {
            var question = GetQuestionById(questionId);
            if (question == null)
                return new ServiceResult { Success = false, Message = "Không tìm thấy câu hỏi" };

            return new ServiceResult { Success = true, Data = question };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting question {QuestionId}: {Message}", questionId, ex.Message);
            return new ServiceResult { Success = false, Message = "Có lỗi xảy ra khi lấy thông tin câu hỏi" };
        }
    }

    // Thêm method xử lý get attempts với validation
    public ServiceResult GetAttemptsWithValidation(int quizId)
    {
        try
        {
            // Kiểm tra quyền xem danh sách bài làm
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
                return new ServiceResult { Success = false, Message = "Bạn phải đăng nhập để xem danh sách bài làm" };

            // Kiểm tra quiz có tồn tại không
            var quiz = GetById(quizId);
            if (quiz == null)
                return new ServiceResult { Success = false, Message = "Không tìm thấy quiz" };

            List<QuizAttemptListItemViewModel> attempts;
            // Nếu là creator thì trả về tất cả bài làm, nếu không thì chỉ trả về bài làm của user hiện tại
            if (quiz.CreatorId == currentUserId.Value)
            {
                attempts = GetAttemptListViewModel(quizId);
            }
            else
            {
                attempts = GetAttemptListViewModel(quizId)
                    .Where(a => a.UserId == currentUserId.Value)
                    .ToList();
            }
            return new ServiceResult {
                Success = true,
                Data = new IQuizService.AttemptsData { Attempts = attempts, Quiz = quiz }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting attempts for quiz {QuizId}: {Message}", quizId, ex.Message);
            return new ServiceResult { Success = false, Message = ex.Message };
        }
    }

    // Thêm method xử lý get attempt detail với validation
    public ServiceResult GetAttemptDetailWithValidation(Guid attemptId, int quizId)
    {
        try
        {
            // Kiểm tra quyền xem chi tiết bài làm
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
                return new ServiceResult { Success = false, Message = "Bạn phải đăng nhập để xem chi tiết bài làm" };

            // Kiểm tra quiz có tồn tại không
            var quiz = GetById(quizId);
            if (quiz == null)
                return new ServiceResult { Success = false, Message = "Không tìm thấy quiz" };

            // Kiểm tra attempt có tồn tại và thuộc quiz này không
            var attempt = _dbContext.QuizAttempts.FirstOrDefault(a => a.AttemptId == attemptId && a.QuizId == quizId);
            if (attempt == null)
                return new ServiceResult { Success = false, Message = "Không tìm thấy bài làm" };

            // Chỉ cho phép người tạo quiz hoặc người làm bài xem chi tiết
            if (quiz.CreatorId != currentUserId.Value && attempt.UserId != currentUserId.Value)
                return new ServiceResult { Success = false, Message = "Bạn không có quyền xem chi tiết bài làm này" };

            // Kiểm tra và sửa chữa dữ liệu không nhất quán
            var validationResult = ValidateAndRepairAttemptData(attemptId, quizId);
            if (!validationResult.Success)
            {
                return validationResult; // Trả về lỗi validation
            }

            var details = GetAttemptDetailViewModel(attemptId, quizId);
            return new ServiceResult { 
                Success = true, 
                Data = new IQuizService.AttemptDetailData { Details = details, Quiz = quiz }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting attempt detail {AttemptId}: {Message}", attemptId, ex.Message);
            return new ServiceResult { Success = false, Message = ex.Message };
        }
    }

    // Thêm method xử lý get statistics với validation
    public ServiceResult GetStatisticsWithValidation(int quizId)
    {
        try
        {
            // Kiểm tra quyền xem thống kê
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
                return new ServiceResult { Success = false, Message = "Bạn phải đăng nhập để xem thống kê" };

            // Kiểm tra quiz có tồn tại không
            var quiz = GetById(quizId);
            if (quiz == null)
                return new ServiceResult { Success = false, Message = "Không tìm thấy quiz" };

            // Chỉ cho phép người tạo quiz xem thống kê
            if (quiz.CreatorId != currentUserId.Value)
                return new ServiceResult { Success = false, Message = "Bạn không có quyền xem thống kê của quiz này" };

            var stats = GetQuizStatistics(quizId);
            return new ServiceResult { Success = true, Data = stats };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting statistics for quiz {QuizId}: {Message}", quizId, ex.Message);
            return new ServiceResult { Success = false, Message = ex.Message };
        }
    }

    // Thêm method xử lý get quiz result với validation
    public ServiceResult GetQuizResultWithValidation(Guid attemptId, int quizId)
    {
        try
        {
            var result = GetQuizResult(attemptId, quizId);
            return new ServiceResult { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting quiz result {AttemptId}: {Message}", attemptId, ex.Message);
            return new ServiceResult { Success = false, Message = ex.Message };
        }
    }

    // Method để kiểm tra và sửa chữa dữ liệu không nhất quán
    public ServiceResult ValidateAndRepairAttemptData(Guid attemptId, int quizId)
    {
        try
        {
            var attempt = _dbContext.QuizAttempts
                .Include(a => a.Details)
                .FirstOrDefault(a => a.AttemptId == attemptId && a.QuizId == quizId);

            if (attempt == null)
                return new ServiceResult { Success = false, Message = "Không tìm thấy bài làm" };

            var details = attempt.Details?.ToList() ?? new List<QuizAttemptDetail>();
            var questions = GetQuestions(quizId);

            _logger.LogInformation($"[ValidateAndRepair] Attempt {attemptId}: Score={attempt.Score}, DetailsCount={details.Count}, QuestionsCount={questions.Count}");

            // Nếu có điểm số nhưng không có chi tiết, có thể có lỗi
            if (attempt.Score > 0 && details.Count == 0)
            {
                _logger.LogWarning($"[ValidateAndRepair] Phát hiện dữ liệu không nhất quán: Score={attempt.Score} nhưng DetailsCount=0");
                return new ServiceResult { 
                    Success = false, 
                    Message = $"Dữ liệu không nhất quán: Điểm số {attempt.Score} nhưng không có chi tiết bài làm. Vui lòng liên hệ quản trị viên." 
                };
            }

            // Nếu có chi tiết nhưng điểm số = 0, tính lại điểm số
            if (details.Count > 0 && attempt.Score == 0)
            {
                var correctCount = details.Count(d => d.IsCorrect);
                var newScore = questions.Count > 0 ? (float)correctCount / questions.Count * 100 : 0;
                
                attempt.Score = newScore;
                _dbContext.SaveChanges();
                
                _logger.LogInformation($"[ValidateAndRepair] Đã sửa điểm số: {newScore} (correctCount={correctCount}, totalQuestions={questions.Count})");
                
                return new ServiceResult { 
                    Success = true, 
                    Message = $"Đã sửa điểm số từ 0 thành {newScore:F1}%" 
                };
            }

            return new ServiceResult { Success = true, Message = "Dữ liệu hợp lệ" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating attempt data {AttemptId}: {Message}", attemptId, ex.Message);
            return new ServiceResult { Success = false, Message = ex.Message };
        }
    }

    // Method để lấy thông tin debug về attempt
    public object GetAttemptDebugInfo(Guid attemptId, int quizId)
    {
        try
        {
            var attempt = _dbContext.QuizAttempts
                .Include(a => a.Details)
                .Include(a => a.User)
                .FirstOrDefault(a => a.AttemptId == attemptId && a.QuizId == quizId);

            if (attempt == null)
                return new { error = "Không tìm thấy bài làm" };

            var details = attempt.Details?.ToList() ?? new List<QuizAttemptDetail>();
            var questions = GetQuestions(quizId);

            return new
            {
                attemptId = attempt.AttemptId,
                quizId = attempt.QuizId,
                userId = attempt.UserId,
                userName = attempt.User?.FullName ?? attempt.DisplayName,
                score = attempt.Score,
                startedAt = attempt.StartedAt,
                endedAt = attempt.EndedAt,
                duration = (int)(attempt.EndedAt - attempt.StartedAt).TotalSeconds,
                detailsCount = details.Count,
                questionsCount = questions.Count,
                correctCount = details.Count(d => d.IsCorrect),
                details = details.Select(d => new
                {
                    questionId = d.QuestionId,
                    answer = d.Answer,
                    isCorrect = d.IsCorrect,
                    timeTaken = d.TimeTaken
                }).ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting attempt debug info {AttemptId}: {Message}", attemptId, ex.Message);
            return new { error = ex.Message };
        }
    }

    public QuizQuestion GetFirstQuestion(int quizId)
    {
        var questions = GetQuestions(quizId);
        if (questions == null || questions.Count == 0) return null;
        return questions[0];
    }

    public QuizQuestion GetNextQuestion(int quizId, int currentQuestionId)
    {
        var questions = GetQuestions(quizId);
        if (questions == null || questions.Count == 0) return null;
        int idx = questions.FindIndex(q => q.QuestionId == currentQuestionId);
        if (idx < 0 || idx + 1 >= questions.Count) return null;
        return questions[idx + 1];
    }

    public object SubmitRealtimeAnswer(string roomCode, int quizId, int questionId, string answer, string displayName)
    {
        if (!_quizRooms.ContainsKey(roomCode))
            _quizRooms[roomCode] = new RoomState();
        var room = _quizRooms[roomCode];
        if (!room.Players.ContainsKey(displayName))
            room.Players[displayName] = new PlayerState { DisplayName = displayName };
        var player = room.Players[displayName];
        var question = GetQuestionById(questionId);
        if (question == null) return new { Success = false, Message = "Câu hỏi không tồn tại" };
        // Tính đúng/sai
        bool isCorrect = false;
        if (!string.IsNullOrWhiteSpace(question.AnswerKey))
        {
            isCorrect = string.Equals(answer?.Trim(), question.AnswerKey.Trim(), StringComparison.OrdinalIgnoreCase);
        }
        // Tính thời gian trả lời
        double timeTaken = 0;
        if (room.QuestionStartTime.HasValue)
        {
            timeTaken = (DateTime.UtcNow - room.QuestionStartTime.Value).TotalSeconds;
        }
        // Lưu đáp án
        player.Answers[questionId] = new AnswerState
        {
            Answer = answer,
            IsCorrect = isCorrect,
            TimeTaken = timeTaken
        };
        if (isCorrect)
        {
            player.CorrectCount++;
            player.TotalTime += timeTaken;
        }
        return new { Success = true, IsCorrect = isCorrect, TimeTaken = timeTaken };
    }

    public object GetRealtimeLeaderboard(string roomCode, int quizId)
    {
        if (!_quizRooms.ContainsKey(roomCode)) return new List<object>();
        var room = _quizRooms[roomCode];
        var leaderboard = new List<object>();
        foreach (var player in room.Players.Values)
        {
            leaderboard.Add(new
            {
                Name = player.DisplayName,
                Correct = player.CorrectCount,
                TotalTime = player.TotalTime
            });
        }
        // Xếp hạng: đúng nhiều nhất, thời gian ít nhất
        leaderboard = leaderboard
            .OrderByDescending(x => ((dynamic)x).Correct)
            .ThenBy(x => ((dynamic)x).TotalTime)
            .ToList();
        return leaderboard;
    }

    public void SaveQuizRoomHistory(string roomCode, int quizId)
    {
        if (!_quizRooms.ContainsKey(roomCode)) return;
        var room = _quizRooms[roomCode];
        foreach (var player in room.Players.Values)
        {
            var attempt = new QuizAttempt
            {
                AttemptId = Guid.NewGuid(),
                QuizId = quizId,
                UserId = null, // Không có user, chỉ lưu tên
                DisplayName = player.DisplayName,
                StartedAt = DateTime.UtcNow,
                EndedAt = DateTime.UtcNow,
                Score = player.CorrectCount
            };
            _dbContext.QuizAttempts.Add(attempt);
            foreach (var ans in player.Answers)
            {
                _dbContext.QuizAttemptDetail.Add(new QuizAttemptDetail
                {
                    AttemptId = attempt.AttemptId,
                    QuestionId = ans.Key,
                    Answer = ans.Value.Answer,
                    IsCorrect = ans.Value.IsCorrect,
                    TimeTaken = ans.Value.TimeTaken
                });
            }
        }
        _dbContext.SaveChanges();
        _quizRooms.Remove(roomCode);
    }

    public (IQuizService.HomeworkAccessResult result, Quiz quiz) CheckHomeworkAccess(int quizId)
    {
        var quiz = GetById(quizId);
        if (quiz == null) return (IQuizService.HomeworkAccessResult.NotFound, null);
        if (!quiz.IsHomework) return (IQuizService.HomeworkAccessResult.Ok, quiz);
        var now = DateTime.UtcNow;
        if (quiz.StartTime.HasValue && now < quiz.StartTime.Value)
            return (IQuizService.HomeworkAccessResult.NotOpen, quiz);
        if (quiz.EndTime.HasValue && now > quiz.EndTime.Value)
            return (IQuizService.HomeworkAccessResult.Closed, quiz);
        return (IQuizService.HomeworkAccessResult.Ok, quiz);
    }

    public ServiceResult SaveAttemptComment(Guid attemptId, int quizId, string comment)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
                return new ServiceResult { Success = false, Message = "Bạn phải đăng nhập để lưu phản hồi" };
            var attempt = _dbContext.QuizAttempts.FirstOrDefault(a => a.AttemptId == attemptId && a.QuizId == quizId);
            if (attempt == null)
                return new ServiceResult { Success = false, Message = "Không tìm thấy bài làm" };
            var quiz = _dbContext.Quizzes.FirstOrDefault(q => q.QuizId == quizId);
            if (quiz == null || quiz.CreatorId != currentUserId.Value)
                return new ServiceResult { Success = false, Message = "Bạn không có quyền phản hồi bài làm này" };
            attempt.Comment = comment;
            _dbContext.SaveChanges();
            return new ServiceResult { Success = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving attempt comment: {Message}", ex.Message);
            return new ServiceResult { Success = false, Message = "Có lỗi xảy ra khi lưu phản hồi: " + ex.Message };
        }
    }

    public QuizAttempt GetQuizAttemptById(Guid attemptId)
    {
        return _dbContext.QuizAttempts.FirstOrDefault(a => a.AttemptId == attemptId);
    }
}