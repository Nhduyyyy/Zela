using System.Collections.Generic;
using Zela.Models;
using Zela.ViewModels;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Zela.Services;

public interface IQuizService
{
    List<QuizzesViewModel> GetAll();
    Quiz GetById(int id);
    int Create(Quiz quiz);
    void Update(Quiz quiz);
    Task UpdateAsync(Quiz quiz);
    void Delete(int id);
    List<QuizQuestion> GetQuestions(int quizId);
    QuizQuestion GetQuestionById(int questionId);
    void AddQuestion(QuizQuestion question);
    void UpdateQuestion(QuizQuestion question);
    void DeleteQuestion(int questionId);
    List<QuizAttempt> GetAttempts(int quizId);
    Guid SubmitAttempt(int quizId, Dictionary<string, object> answers, int duration, float score);
    
    // Conversion methods for Edit functionality
    QuizEditViewModel ConvertToEditViewModel(Quiz quiz);
    Quiz ConvertFromEditViewModel(QuizEditViewModel viewModel);
    
    QuizEditViewModel GetEditViewModelForEdit(int quizId);
    QuizService.ServiceResult UpdateQuizFromEditViewModel(QuizEditViewModel viewModel);

    Quiz GetAddQuestionsViewModel(int quizId);
    QuizService.ServiceResult AddQuestionFromViewModel(QuizQuestion question);
    QuizService.ServiceResult UpdateQuestionFromViewModel(QuizQuestion question);
    QuizService.ServiceResult DeleteQuestionById(int questionId);
    
    // Thêm hàm lấy UserId từ session
    int? GetCurrentUserId();

    List<QuizAttemptListItemViewModel> GetAttemptListViewModel(int quizId);

    List<QuizAttemptDetailViewModel> GetAttemptDetailViewModel(Guid attemptId, int quizId);

    QuizStatisticsViewModel GetQuizStatistics(int quizId);

    Task DeleteQuestionsAsync(List<int> questionIds);
    Task DeleteAllQuestionsAsync(int quizId);

    // New methods for handling business logic
    Task<QuizService.ServiceResult> UploadQuestionsFromFileAsync(int quizId, IFormFile questionsFile);
    QuizService.ServiceResult SubmitAttemptWithValidation(Dictionary<string, object> submission);
    Task<QuizService.ServiceResult> DeleteQuestionsWithValidationAsync(List<int> questionIds);
    Task<QuizService.ServiceResult> DeleteAllQuestionsWithValidationAsync(int quizId);
    QuizService.ServiceResult ValidateTakeQuiz(int quizId);
    QuizService.ServiceResult GetQuestionsWithValidation(int quizId);
    QuizService.ServiceResult GetQuestionWithValidation(int questionId);
    QuizService.ServiceResult GetAttemptsWithValidation(int quizId);
    QuizService.ServiceResult GetAttemptDetailWithValidation(Guid attemptId, int quizId);
    QuizService.ServiceResult GetStatisticsWithValidation(int quizId);
    QuizService.ServiceResult GetQuizResultWithValidation(Guid attemptId, int quizId);
    QuizService.ServiceResult ValidateAndRepairAttemptData(Guid attemptId, int quizId);
    object GetAttemptDebugInfo(Guid attemptId, int quizId);

    // Method để lấy kết quả bài làm
    QuizResultViewModel GetQuizResult(Guid attemptId, int quizId);

    // Data wrapper classes
    public class AttemptsData
    {
        public List<QuizAttemptListItemViewModel> Attempts { get; set; }
        public Quiz Quiz { get; set; }
    }

    public class AttemptDetailData
    {
        public List<QuizAttemptDetailViewModel> Details { get; set; }
        public Quiz Quiz { get; set; }
    }

    QuizQuestion GetFirstQuestion(int quizId);
    QuizQuestion GetNextQuestion(int quizId, int currentQuestionId);
    object SubmitRealtimeAnswer(string roomCode, int quizId, int questionId, string answer, string displayName);
    object GetRealtimeLeaderboard(string roomCode, int quizId);

    void SaveQuizRoomHistory(string roomCode, int quizId);

    // Thêm hai method cho comment/attempt
    QuizAttempt GetQuizAttemptById(Guid attemptId);
    QuizService.ServiceResult SaveAttemptComment(Guid attemptId, int quizId, string comment);

    public enum HomeworkAccessResult
    {
        Ok,
        NotOpen,
        Closed,
        NotFound
    }
    (HomeworkAccessResult result, Quiz quiz) CheckHomeworkAccess(int quizId);
}