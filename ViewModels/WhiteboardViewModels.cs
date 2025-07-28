using System.ComponentModel.DataAnnotations;

namespace Zela.ViewModels;

/// <summary>
/// ViewModel cho trang quản lý Whiteboard
/// </summary>
public class WhiteboardIndexViewModel
{
    public List<WhiteboardCardViewModel> MyWhiteboards { get; set; } = new();
    public List<WhiteboardCardViewModel> PublicTemplates { get; set; } = new();
    public List<WhiteboardCardViewModel> RecentSessions { get; set; } = new();
    public string SearchTerm { get; set; }
    public string FilterType { get; set; } = "all";
}

/// <summary>
/// ViewModel cho card Whiteboard
/// </summary>
public class WhiteboardCardViewModel
{
    public int WhiteboardId { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string CreatorName { get; set; }
    public string CreatorAvatar { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastModifiedAt { get; set; }
    public bool IsPublic { get; set; }
    public bool IsTemplate { get; set; }
    public string ThumbnailUrl { get; set; }
    public int SessionCount { get; set; }
    public bool IsOwner { get; set; }
}

/// <summary>
/// ViewModel cho trang tạo Whiteboard mới
/// </summary>
public class CreateWhiteboardViewModel
{
    [Required(ErrorMessage = "Tiêu đề là bắt buộc")]
    [MaxLength(200, ErrorMessage = "Tiêu đề không được vượt quá 200 ký tự")]
    public string Title { get; set; }

    [MaxLength(1000, ErrorMessage = "Mô tả không được vượt quá 1000 ký tự")]
    public string Description { get; set; }

    public bool IsPublic { get; set; } = false;
    public bool IsTemplate { get; set; } = false;
    public int? RoomId { get; set; } // Nếu tạo từ video room
}

/// <summary>
/// ViewModel cho trang chỉnh sửa Whiteboard
/// </summary>
public class EditWhiteboardViewModel
{
    public int WhiteboardId { get; set; }

    [Required(ErrorMessage = "Tiêu đề là bắt buộc")]
    [MaxLength(200, ErrorMessage = "Tiêu đề không được vượt quá 200 ký tự")]
    public string Title { get; set; }

    [MaxLength(1000, ErrorMessage = "Mô tả không được vượt quá 1000 ký tự")]
    public string Description { get; set; }

    public bool IsPublic { get; set; }
    public bool IsTemplate { get; set; }
    public string CanvasData { get; set; }
}

/// <summary>
/// ViewModel cho API responses
/// </summary>
public class WhiteboardApiResponse
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public object Data { get; set; }
}

/// <summary>
/// ViewModel cho session data
/// </summary>
public class WhiteboardSessionViewModel
{
    public int SessionId { get; set; }
    public int WhiteboardId { get; set; }
    public string CanvasData { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastModifiedAt { get; set; }
    public string ThumbnailUrl { get; set; }
}

/// <summary>
/// ViewModel cho trang Editor Whiteboard
/// </summary>
public class WhiteboardEditorViewModel
{
    // Whiteboard information
    public int WhiteboardId { get; set; }
    public string WhiteboardTitle { get; set; }
    public string WhiteboardDescription { get; set; }
    public bool IsPublic { get; set; }
    public bool IsTemplate { get; set; }
    
    // Current session information
    public int SessionId { get; set; }
    public string CanvasData { get; set; }
    public DateTime SessionCreatedAt { get; set; }
    public DateTime? SessionLastModifiedAt { get; set; }
    
    // Session list for sidebar
    public List<WhiteboardSessionViewModel> Sessions { get; set; } = new();
    public int SessionCount { get; set; }
    
    // User permissions
    public bool CanEdit { get; set; }
    public bool IsOwner { get; set; }
}

/// <summary>
/// Request model cho việc cập nhật session data
/// </summary>
public class UpdateSessionDataRequest
{
    public int SessionId { get; set; }
    public string CanvasData { get; set; }
} 