using Zela.ViewModels;

namespace Zela.Services.Interface;

public interface IWhiteboardService
{
    // Quản lý Whiteboard
    Task<WhiteboardIndexViewModel> GetWhiteboardIndexAsync(int userId, string searchTerm = "", string filterType = "all");
    Task<WhiteboardCardViewModel> GetWhiteboardByIdAsync(int whiteboardId, int userId);
    Task<int> CreateWhiteboardAsync(CreateWhiteboardViewModel model, int userId);
    Task<bool> UpdateWhiteboardAsync(EditWhiteboardViewModel model, int userId);
    Task<bool> DeleteWhiteboardAsync(int whiteboardId, int userId);
    Task<bool> TogglePublicAsync(int whiteboardId, int userId);
    Task<bool> ToggleTemplateAsync(int whiteboardId, int userId);

    // Quản lý Session
    Task<WhiteboardSessionViewModel> GetSessionByIdAsync(int sessionId);
    Task<int> CreateSessionAsync(int whiteboardId, int? roomId = null);
    Task<bool> UpdateSessionDataAsync(int sessionId, string canvasData);
    Task<bool> SaveSessionThumbnailAsync(int sessionId, string thumbnailData);
    Task<List<WhiteboardSessionViewModel>> GetSessionsByWhiteboardAsync(int whiteboardId);
    Task<bool> DeleteSessionAsync(int sessionId, int userId);

    // Template và Public
    Task<List<WhiteboardCardViewModel>> GetPublicTemplatesAsync(int userId = 0);
    Task<List<WhiteboardCardViewModel>> GetRecentSessionsAsync(int userId, int limit = 10);
    Task<bool> CloneWhiteboardAsync(int sourceWhiteboardId, int userId, string newTitle);

    // Validation
    Task<bool> CanUserAccessWhiteboardAsync(int whiteboardId, int userId);
    Task<bool> CanUserEditWhiteboardAsync(int whiteboardId, int userId);
} 