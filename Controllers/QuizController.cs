using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Zela.Services;
using Zela.Models;
using Zela.ViewModels;


namespace Zela.Controllers;

// Custom Authorization Attribute để kiểm tra session-based authentication
public class RequireSessionAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        // Kiểm tra xem action có được đánh dấu là AllowAnonymous không
        var allowAnonymous = context.ActionDescriptor.EndpointMetadata
            .Any(em => em.GetType().Name == "AllowAnonymousAttribute");
        
        if (!allowAnonymous)
        {
            var userId = context.HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                context.Result = new RedirectToActionResult("Login", "Account", null);
                return;
            }
        }
        base.OnActionExecuting(context);
    }
}

// Custom AllowAnonymous Attribute
public class AllowAnonymousAttribute : Attribute { }

[RequireSession] // Yêu cầu đăng nhập cho tất cả actions trong controller
public class QuizController : Controller
{
    private readonly IQuizService _quizService;
    private readonly ILogger<QuizController> _logger;

    public QuizController(IQuizService quizService, ILogger<QuizController> logger)
    {
        _quizService = quizService;
        _logger = logger;
    }

    // Trang danh sách quiz - cho phép xem mà không cần đăng nhập
    [AllowAnonymous]
    public IActionResult Index()
    {
        try
        {
            var quizViewModels = _quizService.GetAll();
            return View(quizViewModels);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Index action: {Message}", ex.Message);
            TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách quiz";
            return View(new List<QuizzesViewModel>());
        }
    }

    // Trang chi tiết quiz - cho phép xem mà không cần đăng nhập
    [AllowAnonymous]
    public IActionResult Details(int id)
    {
        try
        {
            var quiz = _quizService.GetById(id);
            if (quiz == null) 
            {
                TempData["ErrorMessage"] = "Không tìm thấy quiz";
                return RedirectToAction("Index");
            }
            return View(quiz);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Details action for quiz {QuizId}: {Message}", id, ex.Message);
            TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải thông tin quiz";
            return RedirectToAction("Index");
        }
    }

    // Trang tạo quiz - yêu cầu login
    [HttpGet]
    public IActionResult Create()
    {
        return View(new Quiz());
    }

    [HttpPost]
    public IActionResult Create(Quiz quiz)
    {
        if (quiz == null)
        {
            TempData["ErrorMessage"] = "Dữ liệu quiz không hợp lệ";
            return View();
        }

        if (ModelState.IsValid)
        {
            try
            {
                int quizId = _quizService.Create(quiz);
                TempData["SuccessMessage"] = "Tạo quiz thành công! Bây giờ hãy thêm câu hỏi.";
                return RedirectToAction("AddQuestions", new { id = quizId });
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction("Login", "Account");
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError("", ex.Message);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Có lỗi xảy ra khi tạo quiz: " + ex.Message);
            }
        }
        
        return View(quiz);
    }

    // Trang chỉnh sửa quiz - yêu cầu login
    [HttpGet]
    public IActionResult Edit(int id)
    {
        var editViewModel = _quizService.GetEditViewModelForEdit(id);
        if (editViewModel == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy hoặc không có quyền sửa quiz";
            return RedirectToAction("Index");
        }
        return View(editViewModel);
    }

    [HttpPost]
    public IActionResult Edit(QuizEditViewModel viewModel)
    {
        if (!ModelState.IsValid)
            return View(viewModel);
        
        var result = _quizService.UpdateQuizFromEditViewModel(viewModel);
        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.Message;
            return View(viewModel);
        }
        TempData["SuccessMessage"] = "Cập nhật quiz thành công!";
        return RedirectToAction("Index");
    }

    // Xóa quiz - yêu cầu login
    [HttpPost]
    public IActionResult Delete(int id)
    {
        try
        {
            _quizService.Delete(id);
            TempData["SuccessMessage"] = "Xóa quiz thành công!";
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception when deleting quiz {QuizId}: {Message}", id, ex.Message);
            TempData["ErrorMessage"] = "Có lỗi xảy ra khi xóa quiz";
        }
        
        return RedirectToAction("Index");
    }

    // Trang thêm câu hỏi cho quiz - yêu cầu login
    [HttpGet]
    public IActionResult AddQuestions(int id)
    {
        var quiz = _quizService.GetAddQuestionsViewModel(id);
        if (quiz == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy quiz hoặc không có quyền thêm câu hỏi";
            return RedirectToAction("Index");
        }
        return View(quiz);
    }

    // Trang làm bài quiz - yêu cầu đăng nhập
    [HttpGet]
    public IActionResult Take(int id)
    {
        var (access, quiz) = _quizService.CheckHomeworkAccess(id);
        switch (access)
        {
            case IQuizService.HomeworkAccessResult.NotFound:
                TempData["ErrorMessage"] = "Không tìm thấy quiz";
                return RedirectToAction("Index");
            case IQuizService.HomeworkAccessResult.NotOpen:
                return View("HomeworkNotOpen", quiz);
            case IQuizService.HomeworkAccessResult.Closed:
                return View("HomeworkClosed", quiz);
            case IQuizService.HomeworkAccessResult.Ok:
            default:
                var result = _quizService.ValidateTakeQuiz(id);
                if (!result.Success)
                {
                    TempData["ErrorMessage"] = result.Message;
                    return RedirectToAction("Index");
                }
                return View(result.Data);
        }
    }

    // API: Lấy danh sách câu hỏi của quiz - yêu cầu đăng nhập
    [HttpGet]
    public IActionResult GetQuestions(int id)
    {
        var result = _quizService.GetQuestionsWithValidation(id);
        if (!result.Success)
            return StatusCode(500, new { error = result.Message });
        
        return Json(result.Data);
    }

    // API: Lấy thông tin câu hỏi - yêu cầu đăng nhập
    [HttpGet]
    public IActionResult GetQuestion(int id)
    {
        var result = _quizService.GetQuestionWithValidation(id);
        if (!result.Success)
            return BadRequest(new { message = result.Message });
        
        return Json(result.Data);
    }

    // API: Thêm câu hỏi mới - yêu cầu login
    [HttpPost]
    public IActionResult AddQuestion([FromBody] QuizQuestion question)
    {
        var result = _quizService.AddQuestionFromViewModel(question);
        if (result.Success)
            return Json(new { success = true, message = "Thêm câu hỏi thành công!" });
        return BadRequest(new { message = result.Message });
    }

    // API: Cập nhật câu hỏi - yêu cầu login
    [HttpPut]
    public IActionResult UpdateQuestion(int id, [FromBody] QuizQuestion question)
    {
        question.QuestionId = id;
        var result = _quizService.UpdateQuestionFromViewModel(question);
        if (result.Success)
            return Json(new { success = true, message = "Cập nhật câu hỏi thành công!" });
        return BadRequest(new { message = result.Message });
    }

    // API: Xóa câu hỏi - yêu cầu login
    [HttpDelete]
    public IActionResult DeleteQuestion(int id)
    {
        var result = _quizService.DeleteQuestionById(id);
        if (result.Success)
            return Json(new { success = true, message = "Xóa câu hỏi thành công!" });
        return BadRequest(new { message = result.Message });
    }

    // Lấy danh sách câu hỏi của quiz (legacy) - yêu cầu login
    public IActionResult Questions(int quizId)
    {
        var result = _quizService.GetQuestionsWithValidation(quizId);
        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.Message;
            return RedirectToAction("Index");
        }
        
        return View(result.Data);
    }

    // Lấy danh sách attempt của quiz - yêu cầu login
    [HttpGet]
    public IActionResult Attempts(int quizId)
    {
        var result = _quizService.GetAttemptsWithValidation(quizId);
        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.Message;
            return RedirectToAction("Index");
        }
        
        var data = result.Data as IQuizService.AttemptsData;
        ViewBag.QuizId = quizId;
        ViewBag.QuizTitle = data?.Quiz?.Title ?? "";
        return View(data?.Attempts);
    }

    // API: Nộp bài quiz - yêu cầu đăng nhập
    [HttpPost]
    public IActionResult SubmitAttempt([FromBody] Dictionary<string, object> submission)
    {
        var result = _quizService.SubmitAttemptWithValidation(submission);
        if (!result.Success)
        {
            if (result.Message.Contains("đăng nhập"))
                return Unauthorized(new { message = result.Message });
            return BadRequest(new { message = result.Message });
        }
        
        return Json(new { 
            success = true, 
            attemptId = result.Data,
            message = "Nộp bài thành công!"
        });
    }

    [HttpGet]
    public IActionResult AttemptDetail(Guid attemptId, int quizId)
    {
        var result = _quizService.GetAttemptDetailWithValidation(attemptId, quizId);
        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.Message;
            return RedirectToAction("Attempts", new { quizId });
        }
        
        var data = result.Data as IQuizService.AttemptDetailData;
        ViewBag.QuizId = quizId;
        ViewBag.AttemptId = attemptId;
        ViewBag.QuizCreatorId = data?.Quiz?.CreatorId;
        ViewBag.QuestionTypeStats = data?.Quiz?.Questions?.GroupBy(q => q.QuestionType)
            .Select(g => new { Type = g.Key, Count = g.Count() }).ToList();
        return View(data?.Details);
    }

    [HttpGet]
    public IActionResult Statistics(int quizId)
    {
        var result = _quizService.GetStatisticsWithValidation(quizId);
        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.Message;
            return RedirectToAction("Index");
        }
        
        return View(result.Data);
    }

    [HttpGet]
    public IActionResult Result(Guid attemptId, int quizId)
    {
        var result = _quizService.GetQuizResultWithValidation(attemptId, quizId);
        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.Message;
            return RedirectToAction("Index");
        }
        
        return View(result.Data);
    }

    [HttpPost]
    public IActionResult RepairAttemptData(Guid attemptId, int quizId)
    {
        var result = _quizService.ValidateAndRepairAttemptData(attemptId, quizId);
        if (result.Success)
        {
            TempData["SuccessMessage"] = result.Message;
        }
        else
        {
            TempData["ErrorMessage"] = result.Message;
        }
        
        return RedirectToAction("AttemptDetail", new { attemptId, quizId });
    }

    [HttpGet]
    public IActionResult GetAttemptDebugInfo(Guid attemptId, int quizId)
    {
        var debugInfo = _quizService.GetAttemptDebugInfo(attemptId, quizId);
        return Json(debugInfo);
    }

    [HttpPost]
    public async Task<IActionResult> DeleteQuestions([FromBody] List<int> questionIds)
    {
        var result = await _quizService.DeleteQuestionsWithValidationAsync(questionIds);
        if (!result.Success)
            return BadRequest(result.Message);
        
        return Ok(new { success = true, message = result.Message });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteAllQuestions([FromBody] int quizId)
    {
        var result = await _quizService.DeleteAllQuestionsWithValidationAsync(quizId);
        if (!result.Success)
            return BadRequest(result.Message);
        
        return Ok(new { success = true, message = result.Message });
    }

    // Upload questions from file
    [HttpPost]
    public async Task<IActionResult> UploadQuestions(int quizId, IFormFile questionsFile)
    {
        var result = await _quizService.UploadQuestionsFromFileAsync(quizId, questionsFile);
        if (!result.Success)
            TempData["ErrorMessage"] = result.Message;
        else
            TempData["SuccessMessage"] = result.Message;
        
        return RedirectToAction("AddQuestions", new { id = quizId });
    }

    [HttpGet]
    public IActionResult ExportQuizResult(int quizId)
    {
        var attempts = _quizService.GetAttempts(quizId);
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Tên,Điểm");
        foreach (var a in attempts)
        {
            sb.AppendLine($"{a.DisplayName},{a.Score}");
        }
        var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
        return File(bytes, "text/csv", $"QuizResult_{quizId}.csv");
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult QuizRealtime(int id, string roomCode = null)
    {
        var quiz = _quizService.GetById(id);
        if (quiz == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy quiz";
            return RedirectToAction("Index");
        }
        // Nếu là giáo viên, luôn tạo roomCode mới mỗi lần mở panel
        var currentUserId = HttpContext.Session.GetInt32("UserId");
        bool isTeacher = (currentUserId.HasValue && quiz.CreatorId == currentUserId.Value);
        if (isTeacher || string.IsNullOrWhiteSpace(roomCode))
        {
            // Tạo roomCode ngẫu nhiên 6 ký tự
            roomCode = Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();
            // TODO: Lưu roomCode vào cache/tạm thời (ví dụ: MemoryCache, hoặc static Dictionary)
        }
        ViewBag.QuizId = quiz.QuizId;
        ViewBag.RoomCode = roomCode;
        ViewBag.QuizTitle = quiz.Title;
        ViewBag.QuizDescription = quiz.Description;
        ViewBag.IsTeacher = isTeacher;
        return PartialView("~/Views/Quiz/_QuizRealtimePartial.cshtml");
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult JoinQuizRoom()
    {
        return View();
    }
}