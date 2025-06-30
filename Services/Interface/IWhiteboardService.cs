using Zela.Models;
using Zela.ViewModels;

namespace Zela.Services.Interface
{
    public interface IWhiteboardService
    {
        // Session Management
        Task<Guid> CreateWhiteboardSessionAsync(int roomId);
        Task<WhiteboardSession?> GetActiveSessionAsync(int roomId);
        Task<bool> EndSessionAsync(Guid sessionId);
        
        // Drawing Actions
        Task<DrawAction> AddDrawActionAsync(Guid sessionId, int userId, string actionType, string payload);
        Task<List<DrawAction>> GetDrawActionsAsync(Guid sessionId, DateTime? since = null);
        Task<bool> ClearWhiteboardAsync(Guid sessionId, int userId);
        
        // Collaboration
        Task<bool> IsUserInSessionAsync(Guid sessionId, int userId);
        Task<List<int>> GetSessionParticipantsAsync(Guid sessionId);
        
        // Templates & Saving
        Task<string> SaveAsTemplateAsync(Guid sessionId, string templateName, int userId);
        Task<List<WhiteboardTemplate>> GetTemplatesAsync(int userId);
        Task<bool> LoadTemplateAsync(Guid sessionId, string templateName, int userId);
        
        // Export
        Task<byte[]> ExportAsImageAsync(Guid sessionId, string format = "png");
        Task<byte[]> ExportAsPDFAsync(Guid sessionId);
        
        // Statistics
        Task<WhiteboardStats> GetSessionStatsAsync(Guid sessionId);
    }
    
    public class WhiteboardTemplate
    {
        public string Name { get; set; }
        public string Data { get; set; }
        public DateTime CreatedAt { get; set; }
        public int CreatedByUserId { get; set; }
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