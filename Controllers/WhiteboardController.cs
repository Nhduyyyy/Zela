using Microsoft.AspNetCore.Mvc;
using Zela.Services.Interface;
using Zela.Models;
using System.Security.Claims;
using System.Text.Json;

namespace Zela.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WhiteboardController : ControllerBase
    {
        private readonly IWhiteboardService _whiteboardService;
        private readonly ILogger<WhiteboardController> _logger;

        public WhiteboardController(IWhiteboardService whiteboardService, ILogger<WhiteboardController> logger)
        {
            _whiteboardService = whiteboardService;
            _logger = logger;
        }

        // ======== SESSION MANAGEMENT ========

        [HttpPost("session/create")]
        public async Task<IActionResult> CreateSession([FromBody] CreateSessionRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return Unauthorized();

                var sessionId = await _whiteboardService.CreateWhiteboardSessionAsync(request.RoomId);
                
                return Ok(new { 
                    success = true, 
                    sessionId = sessionId,
                    message = "Whiteboard session created successfully" 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating whiteboard session for room {RoomId}", request.RoomId);
                return BadRequest(new { success = false, error = ex.Message });
            }
        }

        [HttpGet("session/{roomId}")]
        public async Task<IActionResult> GetActiveSession(int roomId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return Unauthorized();

                var session = await _whiteboardService.GetActiveSessionAsync(roomId);
                if (session == null)
                    return NotFound(new { success = false, error = "No active session found" });

                return Ok(new { 
                    success = true, 
                    session = new
                    {
                        sessionId = session.WbSessionId,
                        roomId = session.RoomId,
                        createdAt = session.CreatedAt,
                        actions = session.DrawActions?.Select(a => new
                        {
                            actionId = a.ActionId,
                            actionType = a.ActionType,
                            payload = a.Payload,
                            timestamp = a.Timestamp,
                            userId = a.UserId,
                            userName = a.User?.FullName
                        })
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active session for room {RoomId}", roomId);
                return BadRequest(new { success = false, error = ex.Message });
            }
        }

        [HttpPost("session/{sessionId}/end")]
        public async Task<IActionResult> EndSession(Guid sessionId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return Unauthorized();

                var success = await _whiteboardService.EndSessionAsync(sessionId);
                
                return Ok(new { 
                    success = success, 
                    message = success ? "Session ended successfully" : "Failed to end session" 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ending whiteboard session {SessionId}", sessionId);
                return BadRequest(new { success = false, error = ex.Message });
            }
        }

        // ======== DRAWING ACTIONS ========

        [HttpPost("session/{sessionId}/action")]
        public async Task<IActionResult> AddDrawAction(Guid sessionId, [FromBody] DrawActionRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return Unauthorized();

                var action = await _whiteboardService.AddDrawActionAsync(
                    sessionId, userId, request.ActionType, request.Payload);

                return Ok(new { 
                    success = true, 
                    actionId = action.ActionId,
                    message = "Draw action added successfully" 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding draw action for session {SessionId}", sessionId);
                return BadRequest(new { success = false, error = ex.Message });
            }
        }

        [HttpGet("session/{sessionId}/actions")]
        public async Task<IActionResult> GetDrawActions(Guid sessionId, [FromQuery] DateTime? since)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return Unauthorized();

                var actions = await _whiteboardService.GetDrawActionsAsync(sessionId, since);
                
                return Ok(new { 
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
                return BadRequest(new { success = false, error = ex.Message });
            }
        }

        [HttpPost("session/{sessionId}/clear")]
        public async Task<IActionResult> ClearWhiteboard(Guid sessionId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return Unauthorized();

                var success = await _whiteboardService.ClearWhiteboardAsync(sessionId, userId);
                
                return Ok(new { 
                    success = success, 
                    message = success ? "Whiteboard cleared successfully" : "Failed to clear whiteboard" 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing whiteboard for session {SessionId}", sessionId);
                return BadRequest(new { success = false, error = ex.Message });
            }
        }

        // ======== TEMPLATES ========

        [HttpPost("session/{sessionId}/save-template")]
        public async Task<IActionResult> SaveAsTemplate(Guid sessionId, [FromBody] SaveTemplateRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return Unauthorized();

                var templateData = await _whiteboardService.SaveAsTemplateAsync(
                    sessionId, request.TemplateName, userId);

                return Ok(new { 
                    success = true, 
                    templateData = templateData,
                    message = "Template saved successfully" 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving template for session {SessionId}", sessionId);
                return BadRequest(new { success = false, error = ex.Message });
            }
        }

        [HttpGet("templates")]
        public async Task<IActionResult> GetTemplates()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return Unauthorized();

                var templates = await _whiteboardService.GetTemplatesAsync(userId);
                
                return Ok(new { 
                    success = true, 
                    templates = templates 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting templates for user {UserId}", GetCurrentUserId());
                return BadRequest(new { success = false, error = ex.Message });
            }
        }

        [HttpPost("session/{sessionId}/load-template")]
        public async Task<IActionResult> LoadTemplate(Guid sessionId, [FromBody] LoadTemplateRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return Unauthorized();

                var success = await _whiteboardService.LoadTemplateAsync(
                    sessionId, request.TemplateName, userId);

                return Ok(new { 
                    success = success, 
                    message = success ? "Template loaded successfully" : "Failed to load template" 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading template for session {SessionId}", sessionId);
                return BadRequest(new { success = false, error = ex.Message });
            }
        }

        // ======== EXPORT ========

        [HttpGet("session/{sessionId}/export/image")]
        public async Task<IActionResult> ExportAsImage(Guid sessionId, [FromQuery] string format = "png")
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return Unauthorized();

                var imageData = await _whiteboardService.ExportAsImageAsync(sessionId, format);
                
                return File(imageData, $"image/{format}", $"whiteboard-{sessionId}.{format}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting whiteboard session {SessionId} as image", sessionId);
                return BadRequest(new { success = false, error = ex.Message });
            }
        }

        [HttpGet("session/{sessionId}/export/pdf")]
        public async Task<IActionResult> ExportAsPDF(Guid sessionId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return Unauthorized();

                var pdfData = await _whiteboardService.ExportAsPDFAsync(sessionId);
                
                return File(pdfData, "application/pdf", $"whiteboard-{sessionId}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting whiteboard session {SessionId} as PDF", sessionId);
                return BadRequest(new { success = false, error = ex.Message });
            }
        }

        // ======== STATISTICS ========

        [HttpGet("session/{sessionId}/stats")]
        public async Task<IActionResult> GetSessionStats(Guid sessionId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return Unauthorized();

                var stats = await _whiteboardService.GetSessionStatsAsync(sessionId);
                
                return Ok(new { 
                    success = true, 
                    stats = stats 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting stats for whiteboard session {SessionId}", sessionId);
                return BadRequest(new { success = false, error = ex.Message });
            }
        }

        // ======== HELPER METHODS ========

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (int.TryParse(userIdClaim, out int userId))
                return userId;
            return 0;
        }
    }

    // ======== REQUEST MODELS ========

    public class CreateSessionRequest
    {
        public int RoomId { get; set; }
    }

    public class DrawActionRequest
    {
        public string ActionType { get; set; } = string.Empty;
        public string Payload { get; set; } = string.Empty;
    }

    public class SaveTemplateRequest
    {
        public string TemplateName { get; set; } = string.Empty;
    }

    public class LoadTemplateRequest
    {
        public string TemplateName { get; set; } = string.Empty;
    }
} 