using Zela.Models;
using Zela.ViewModels;

namespace Zela.Services.Interface
{
    public interface IWhiteboardService
    {
        // Session Management
        Task<Guid> CreateWhiteboardSessionAsync(int roomId);
        Task<Guid> CreateStandaloneSessionAsync(int userId);
        Task<WhiteboardSession?> GetActiveSessionAsync(int roomId);
        Task<bool> EndSessionAsync(Guid sessionId);
        Task<bool> SaveSessionAsync(Guid sessionId, int userId, string? sessionName = null);
        Task<List<WhiteboardSession>> GetUserSessionsAsync(int userId, int limit = 20);
        
        // Drawing Actions
        Task<DrawAction> AddDrawActionAsync(Guid sessionId, int userId, string actionType, string payload);
        Task<List<DrawAction>> GetDrawActionsAsync(Guid sessionId, DateTime? since = null);
        Task<bool> ClearWhiteboardAsync(Guid sessionId, int userId);
        
        // Collaboration
        Task<bool> IsUserInSessionAsync(Guid sessionId, int userId);
        Task<List<int>> GetSessionParticipantsAsync(Guid sessionId);
        
        // Templates & Saving
        Task<string> SaveAsTemplateAsync(Guid sessionId, string templateName, int userId, string? description = null, bool isPublic = false);
        Task<List<Models.WhiteboardTemplate>> GetTemplatesAsync(int userId);
        Task<Models.WhiteboardTemplate?> GetTemplateByIdAsync(int templateId, int userId);
        Task<bool> LoadTemplateAsync(Guid sessionId, int templateId, int userId);
        Task<bool> LoadTemplateByNameAsync(Guid sessionId, string templateName, int userId);
        Task<bool> DeleteTemplateAsync(int templateId, int userId);
        
        // Export
        Task<byte[]> ExportAsImageAsync(Guid sessionId, string format = "png");
        Task<byte[]> ExportAsPDFAsync(Guid sessionId);
        
        // Statistics
        Task<WhiteboardStats> GetSessionStatsAsync(Guid sessionId);
    }
    
    public class WhiteboardStats
    {
        public int TotalActions { get; set; }
        public int ActiveUsers { get; set; }
        public TimeSpan SessionDuration { get; set; }
        public Dictionary<string, int> ActionTypes { get; set; }
        public List<UserActivity> UserActivities { get; set; }
    }
    
    public class UserActivity
    {
        public int UserId { get; set; }
        public string UserName { get; set; }
        public int ActionCount { get; set; }
        public DateTime LastActivity { get; set; }
    }
} 