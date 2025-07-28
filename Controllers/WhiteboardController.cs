using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zela.Services.Interface;
using Zela.ViewModels;
using System.Security.Claims;

namespace Zela.Controllers;

[Authorize]
public class WhiteboardController : Controller
{
    private readonly IWhiteboardService _whiteboardService;

    public WhiteboardController(IWhiteboardService whiteboardService)
    {
        _whiteboardService = whiteboardService;
    }

    /// <summary>
    /// Trang chính quản lý Whiteboard
    /// </summary>
    public async Task<IActionResult> Index(string searchTerm = "", string filterType = "all")
    {
        var userId = GetCurrentUserId();
        var viewModel = await _whiteboardService.GetWhiteboardIndexAsync(userId, searchTerm, filterType);
        return View(viewModel);
    }

    /// <summary>
    /// Trang tạo Whiteboard mới
    /// </summary>
    public IActionResult Create(int? roomId = null)
    {
        var viewModel = new CreateWhiteboardViewModel
        {
            RoomId = roomId
        };
        return View(viewModel);
    }

    /// <summary>
    /// Xử lý tạo Whiteboard mới
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateWhiteboardViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var userId = GetCurrentUserId();
        var whiteboardId = await _whiteboardService.CreateWhiteboardAsync(model, userId);

        TempData["SuccessMessage"] = "Tạo bảng trắng thành công!";
        return RedirectToAction(nameof(Editor), new { id = whiteboardId });
    }

    /// <summary>
    /// Trang chỉnh sửa Whiteboard
    /// </summary>
    public async Task<IActionResult> Editor(int id, int? sessionId = null)
    {
        var userId = GetCurrentUserId();
        var whiteboard = await _whiteboardService.GetWhiteboardByIdAsync(id, userId);

        if (whiteboard == null)
        {
            return NotFound();
        }

        if (!await _whiteboardService.CanUserAccessWhiteboardAsync(id, userId))
        {
            return Forbid();
        }

        var sessions = await _whiteboardService.GetSessionsByWhiteboardAsync(id);
        var currentSession = sessionId.HasValue 
            ? sessions.FirstOrDefault(s => s.SessionId == sessionId.Value)
            : sessions.FirstOrDefault();

        if (currentSession == null)
        {
            return NotFound("Session không tồn tại");
        }

        var viewModel = new WhiteboardEditorViewModel
        {
            // Whiteboard information
            WhiteboardId = id,
            WhiteboardTitle = whiteboard.Title,
            WhiteboardDescription = whiteboard.Description,
            IsPublic = whiteboard.IsPublic,
            IsTemplate = whiteboard.IsTemplate,
            
            // Current session information
            SessionId = currentSession.SessionId,
            CanvasData = currentSession.CanvasData,
            SessionCreatedAt = currentSession.CreatedAt,
            SessionLastModifiedAt = currentSession.LastModifiedAt,
            
            // Session list for sidebar
            Sessions = sessions,
            SessionCount = sessions.Count,
            
            // User permissions
            CanEdit = await _whiteboardService.CanUserEditWhiteboardAsync(id, userId),
            IsOwner = whiteboard.IsOwner
        };

        return View(viewModel);
    }

    /// <summary>
    /// Cập nhật thông tin Whiteboard
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(EditWhiteboardViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return RedirectToAction(nameof(Editor), new { id = model.WhiteboardId });
        }

        var userId = GetCurrentUserId();
        var success = await _whiteboardService.UpdateWhiteboardAsync(model, userId);

        if (success)
        {
            TempData["SuccessMessage"] = "Cập nhật thành công!";
        }
        else
        {
            TempData["ErrorMessage"] = "Không thể cập nhật bảng trắng.";
        }

        return RedirectToAction(nameof(Editor), new { id = model.WhiteboardId });
    }

    /// <summary>
    /// Xóa Whiteboard
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetCurrentUserId();
        var success = await _whiteboardService.DeleteWhiteboardAsync(id, userId);

        if (success)
        {
            TempData["SuccessMessage"] = "Xóa bảng trắng thành công!";
        }
        else
        {
            TempData["ErrorMessage"] = "Không thể xóa bảng trắng.";
        }

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Toggle trạng thái public
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> TogglePublic(int id)
    {
        var userId = GetCurrentUserId();
        var success = await _whiteboardService.TogglePublicAsync(id, userId);

        return Json(new { success });
    }

    /// <summary>
    /// Toggle trạng thái template
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> ToggleTemplate(int id)
    {
        var userId = GetCurrentUserId();
        var success = await _whiteboardService.ToggleTemplateAsync(id, userId);

        return Json(new { success });
    }

    /// <summary>
    /// Clone Whiteboard
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Clone(int id, string title)
    {
        var userId = GetCurrentUserId();
        var success = await _whiteboardService.CloneWhiteboardAsync(id, userId, title);

        if (success)
        {
            TempData["SuccessMessage"] = "Sao chép bảng trắng thành công!";
        }
        else
        {
            TempData["ErrorMessage"] = "Không thể sao chép bảng trắng.";
        }

        return RedirectToAction(nameof(Index));
    }

    #region API Endpoints

    /// <summary>
    /// API: Lấy dữ liệu session
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetSession(int sessionId)
    {
        var session = await _whiteboardService.GetSessionByIdAsync(sessionId);
        
        if (session == null)
        {
            return NotFound();
        }

        return Json(new WhiteboardApiResponse
        {
            Success = true,
            Data = session
        });
    }

    /// <summary>
    /// API: Cập nhật dữ liệu canvas
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> UpdateSessionData([FromBody] UpdateSessionDataRequest request)
    {
        var success = await _whiteboardService.UpdateSessionDataAsync(request.SessionId, request.CanvasData);

        return Json(new WhiteboardApiResponse
        {
            Success = success,
            Message = success ? "Cập nhật thành công" : "Không thể cập nhật"
        });
    }

    /// <summary>
    /// API: Lưu thumbnail
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> SaveThumbnail(int sessionId, [FromBody] string thumbnailData)
    {
        var success = await _whiteboardService.SaveSessionThumbnailAsync(sessionId, thumbnailData);

        return Json(new WhiteboardApiResponse
        {
            Success = success,
            Message = success ? "Lưu thumbnail thành công" : "Không thể lưu thumbnail"
        });
    }

    /// <summary>
    /// API: Tạo session mới
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateSession(int whiteboardId, int? roomId = null)
    {
        var sessionId = await _whiteboardService.CreateSessionAsync(whiteboardId, roomId);

        return Json(new WhiteboardApiResponse
        {
            Success = sessionId > 0,
            Data = new { sessionId },
            Message = sessionId > 0 ? "Tạo session thành công" : "Không thể tạo session"
        });
    }

    #endregion

    private int GetCurrentUserId()
    {
        try
        {
            var userIdClaim = User.FindFirst("UserId");
            if (userIdClaim?.Value != null)
            {
                if (int.TryParse(userIdClaim.Value, out int userId))
                {
                    return userId;
                }
            }
            
            // Fallback: try to get from NameIdentifier claim
            var nameIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (nameIdClaim?.Value != null)
            {
                if (int.TryParse(nameIdClaim.Value, out int nameId))
                {
                    return nameId;
                }
            }
            
            return 0;
        }
        catch (Exception)
        {
            return 0;
        }
    }
} 