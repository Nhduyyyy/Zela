using Microsoft.AspNetCore.Mvc;
using Zela.Services.Interface;
using Zela.Models;
using System.Security.Claims;
using System.Text.Json;

namespace Zela.Controllers
{
    public class WhiteboardController : Controller
    {
        private readonly IWhiteboardService _whiteboardService;
        private readonly ILogger<WhiteboardController> _logger;

        public WhiteboardController(IWhiteboardService whiteboardService, ILogger<WhiteboardController> logger)
        {
            _whiteboardService = whiteboardService;
            _logger = logger;
        }

        // ======== MVC ACTIONS ========

        /// <summary>
        /// Trang chính whiteboard - quản lý template và tạo whiteboard mới
        /// </summary>
        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return RedirectToAction("Login", "Account");

            var templates = await _whiteboardService.GetTemplatesAsync(userId);
            ViewBag.Templates = templates;
            ViewBag.UserId = userId;
            
            return View();
        }

        /// <summary>
        /// Trang editor whiteboard - vẽ và chỉnh sửa
        /// </summary>
        public async Task<IActionResult> Editor(string? sessionId = null, int? templateId = null)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return RedirectToAction("Login", "Account");

            // Nếu có templateId, load template
            if (templateId.HasValue)
            {
                var template = await _whiteboardService.GetTemplateByIdAsync(templateId.Value, userId);
                if (template != null)
                {
                    ViewBag.Template = template;
                    ViewBag.TemplateId = templateId.Value;
                }
            }

            // Nếu có sessionId, load session
            if (!string.IsNullOrEmpty(sessionId) && Guid.TryParse(sessionId, out Guid sessionGuid))
            {
                ViewBag.SessionId = sessionId;
            }

            ViewBag.UserId = userId;
            return View();
        }

        // ======== API ACTIONS (JSON responses) ========

        /// <summary>
        /// Tạo session whiteboard mới
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateSession([FromBody] CreateSessionRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return Json(new { success = false, error = "Unauthorized" });

                var sessionId = await _whiteboardService.CreateWhiteboardSessionAsync(request.RoomId);
                
                // Nếu có template name, tạo template ngay
                if (!string.IsNullOrEmpty(request.TemplateName))
                {
                    await _whiteboardService.SaveAsTemplateAsync(
                        sessionId, 
                        request.TemplateName, 
                        userId, 
                        request.Description, 
                        request.IsPublic);
                }
                
                return Json(new { 
                    success = true, 
                    sessionId = sessionId,
                    message = "Whiteboard session created successfully" 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating whiteboard session for room {RoomId}", request.RoomId);
                return Json(new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// Tạo session whiteboard độc lập (không cần video room)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateStandaloneSession()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return Json(new { success = false, error = "Unauthorized" });

                var sessionId = await _whiteboardService.CreateStandaloneSessionAsync(userId);
                
                return Json(new { 
                    success = true, 
                    sessionId = sessionId,
                    message = "Standalone whiteboard session created successfully" 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating standalone whiteboard session");
                return Json(new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// Lưu session hiện tại
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> SaveSession(Guid sessionId, [FromBody] SaveSessionRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return Json(new { success = false, error = "Unauthorized" });

                var success = await _whiteboardService.SaveSessionAsync(sessionId, userId, request.SessionName);
                
                return Json(new { 
                    success = success, 
                    message = success ? "Session saved successfully" : "Failed to save session" 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving session {SessionId}", sessionId);
                return Json(new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// Lấy danh sách sessions của user
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetUserSessions()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return Json(new { success = false, error = "Unauthorized" });

                var sessions = await _whiteboardService.GetUserSessionsAsync(userId);
                
                return Json(new { 
                    success = true, 
                    sessions = sessions.Select(s => new
                    {
                        sessionId = s.WbSessionId,
                        sessionName = s.SessionName ?? $"Session {s.WbSessionId.ToString().Substring(0, 8)}",
                        createdAt = s.CreatedAt,
                        updatedAt = s.UpdatedAt,
                        actionCount = s.DrawActions?.Count ?? 0,
                        isActive = s.CreatedAt > DateTime.UtcNow.AddHours(-24)
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user sessions");
                return Json(new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// Load template vào session hiện tại
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> LoadTemplateToSession([FromBody] LoadTemplateToSessionRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return Json(new { success = false, error = "Unauthorized" });

                if (!Guid.TryParse(request.SessionId, out Guid sessionGuid))
                    return Json(new { success = false, error = "Invalid session ID" });

                var success = await _whiteboardService.LoadTemplateAsync(sessionGuid, request.TemplateId, userId);

                return Json(new { 
                    success = success, 
                    message = success ? "Template loaded successfully" : "Failed to load template" 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading template {TemplateId} to session {SessionId}", request.TemplateId, request.SessionId);
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetActiveSession(int roomId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return Json(new { success = false, error = "Unauthorized" });

                var session = await _whiteboardService.GetActiveSessionAsync(roomId);
                
                return Json(new { 
                    success = true, 
                    session = session != null ? new
                    {
                        sessionId = session.WbSessionId,
                        roomId = session.RoomId,
                        createdAt = session.CreatedAt,
                        actionCount = session.DrawActions?.Count ?? 0
                    } : null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active session for room {RoomId}", roomId);
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> EndSession(Guid sessionId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return Json(new { success = false, error = "Unauthorized" });

                var success = await _whiteboardService.EndSessionAsync(sessionId);
                
                return Json(new { 
                    success = success, 
                    message = success ? "Session ended successfully" : "Failed to end session" 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ending whiteboard session {SessionId}", sessionId);
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddDrawAction(Guid sessionId, [FromBody] DrawActionRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return Json(new { success = false, error = "Unauthorized" });

                var action = await _whiteboardService.AddDrawActionAsync(
                    sessionId, userId, request.ActionType, request.Payload);

                return Json(new { 
                    success = true, 
                    actionId = action.ActionId,
                    message = "Draw action added successfully" 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding draw action for session {SessionId}", sessionId);
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetDrawActions(Guid sessionId, DateTime? since)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return Json(new { success = false, error = "Unauthorized" });

                var actions = await _whiteboardService.GetDrawActionsAsync(sessionId, since);
                
                return Json(new { 
                    success = true, 
                    actions = actions.Select(a => new
                    {
                        actionId = a.ActionId,
                        actionType = a.ActionType,
                        payload = a.Payload,
                        timestamp = a.Timestamp,
                        userId = a.UserId,
                        userName = a.User?.FullName
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting draw actions for session {SessionId}", sessionId);
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ClearWhiteboard(Guid sessionId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return Json(new { success = false, error = "Unauthorized" });

                var success = await _whiteboardService.ClearWhiteboardAsync(sessionId, userId);
                
                return Json(new { 
                    success = success, 
                    message = success ? "Whiteboard cleared successfully" : "Failed to clear whiteboard" 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing whiteboard for session {SessionId}", sessionId);
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveTemplate(Guid sessionId, [FromBody] SaveTemplateRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return Json(new { success = false, error = "Unauthorized" });

                var templateData = await _whiteboardService.SaveAsTemplateAsync(
                    sessionId, request.TemplateName, userId, request.Description, request.IsPublic);

                return Json(new { 
                    success = true, 
                    templateData = templateData,
                    message = "Template saved successfully" 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving template for session {SessionId}", sessionId);
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetTemplates()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return Json(new { success = false, error = "Unauthorized" });

                var templates = await _whiteboardService.GetTemplatesAsync(userId);
                
                return Json(new { 
                    success = true, 
                    templates = templates.Select(t => new
                    {
                        id = t.Id,
                        name = t.Name,
                        description = t.Description,
                        thumbnail = t.Thumbnail,
                        createdAt = t.CreatedAt,
                        createdByUserId = t.CreatedByUserId,
                        createdByUserName = t.CreatedByUser?.FullName,
                        isPublic = t.IsPublic
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting templates for user {UserId}", GetCurrentUserId());
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetTemplate(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return Json(new { success = false, error = "Unauthorized" });

                var template = await _whiteboardService.GetTemplateByIdAsync(id, userId);
                if (template == null)
                    return Json(new { success = false, error = "Template not found or access denied" });

                return Json(new { 
                    success = true, 
                    template = new
                    {
                        id = template.Id,
                        name = template.Name,
                        description = template.Description,
                        thumbnail = template.Thumbnail,
                        createdAt = template.CreatedAt,
                        createdByUserId = template.CreatedByUserId,
                        createdByUserName = template.CreatedByUser?.FullName,
                        isPublic = template.IsPublic,
                        data = template.Data
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting template {TemplateId} for user {UserId}", id, GetCurrentUserId());
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteTemplate(int templateId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return Json(new { success = false, error = "Unauthorized" });

                var success = await _whiteboardService.DeleteTemplateAsync(templateId, userId);
                
                return Json(new { 
                    success = success, 
                    message = success ? "Template deleted successfully" : "Failed to delete template" 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting template {TemplateId} for user {UserId}", templateId, GetCurrentUserId());
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ExportImage(Guid sessionId, string format = "png")
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return Json(new { success = false, error = "Unauthorized" });

                var imageData = await _whiteboardService.ExportAsImageAsync(sessionId, format);
                
                return File(imageData, $"image/{format}", $"whiteboard-{sessionId}.{format}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting whiteboard session {SessionId} as image", sessionId);
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ExportPDF(Guid sessionId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return Json(new { success = false, error = "Unauthorized" });

                var pdfData = await _whiteboardService.ExportAsPDFAsync(sessionId);
                
                return File(pdfData, "application/pdf", $"whiteboard-{sessionId}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting whiteboard session {SessionId} as PDF", sessionId);
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetSessionStats(Guid sessionId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return Json(new { success = false, error = "Unauthorized" });

                var stats = await _whiteboardService.GetSessionStatsAsync(sessionId);
                
                return Json(new { 
                    success = true, 
                    stats = stats 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting stats for whiteboard session {SessionId}", sessionId);
                return Json(new { success = false, error = ex.Message });
            }
        }

        // ======== HELPER METHODS ========

        private int GetCurrentUserId()
        {
            // Try to get from claims first (for API calls)
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (int.TryParse(userIdClaim, out int userId))
                return userId;
            
            // Fallback to session (for MVC actions)
            return HttpContext.Session.GetInt32("UserId") ?? 0;
        }
    }

    // ======== REQUEST MODELS ========

    public class CreateSessionRequest
    {
        public int RoomId { get; set; }
        public string? TemplateName { get; set; }
        public string? Description { get; set; }
        public bool IsPublic { get; set; } = false;
    }

    public class DrawActionRequest
    {
        public string ActionType { get; set; } = string.Empty;
        public string Payload { get; set; } = string.Empty;
    }

    public class SaveTemplateRequest
    {
        public string TemplateName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsPublic { get; set; } = false;
    }

    public class SaveSessionRequest
    {
        public string? SessionName { get; set; }
    }

    public class LoadTemplateToSessionRequest
    {
        public string SessionId { get; set; } = string.Empty;
        public int TemplateId { get; set; }
    }
} 