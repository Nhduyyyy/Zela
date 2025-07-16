using Microsoft.EntityFrameworkCore;
using Zela.DbContext;
using Zela.Models;
using Zela.Services.Interface;
using Zela.ViewModels;
using System.Text.Json;

namespace Zela.Services
{
    public class WhiteboardService : IWhiteboardService
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<WhiteboardService> _logger;

        public WhiteboardService(ApplicationDbContext db, ILogger<WhiteboardService> logger)
        {
            _db = db;
            _logger = logger;
        }

        // ======== SESSION MANAGEMENT ========
        
        public async Task<Guid> CreateWhiteboardSessionAsync(int roomId)
        {
            try
            {
                // Check if room exists
                var room = await _db.VideoRooms.FindAsync(roomId);
                if (room == null)
                    throw new InvalidOperationException("Room not found");

                // Check if there's already an active session
                var existingSession = await _db.WhiteboardSessions
                    .FirstOrDefaultAsync(wb => wb.RoomId == roomId && wb.CreatedAt > DateTime.UtcNow.AddHours(-24));
                
                if (existingSession != null)
                    return existingSession.WbSessionId;

                // Create new session
                var session = new WhiteboardSession
                {
                    WbSessionId = Guid.NewGuid(),
                    RoomId = roomId,
                    CreatedByUserId = room.CreatorId, // Use room creator as session creator
                    CreatedAt = DateTime.UtcNow
                };

                _db.WhiteboardSessions.Add(session);
                await _db.SaveChangesAsync();

                _logger.LogInformation("Created whiteboard session {SessionId} for room {RoomId}", 
                    session.WbSessionId, roomId);

                return session.WbSessionId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating whiteboard session for room {RoomId}", roomId);
                throw;
            }
        }

        public async Task<Guid> CreateStandaloneSessionAsync(int userId)
        {
            try
            {
                // Create new standalone session (no room required)
                var session = new WhiteboardSession
                {
                    WbSessionId = Guid.NewGuid(),
                    RoomId = null, // Standalone sessions don't need a room
                    CreatedByUserId = userId,
                    CreatedAt = DateTime.UtcNow
                };

                _db.WhiteboardSessions.Add(session);
                await _db.SaveChangesAsync();

                _logger.LogInformation("Created standalone whiteboard session {SessionId} for user {UserId}", 
                    session.WbSessionId, userId);

                return session.WbSessionId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating standalone whiteboard session for user {UserId}", userId);
                throw;
            }
        }

        public async Task<WhiteboardSession?> GetActiveSessionAsync(int roomId)
        {
            return await _db.WhiteboardSessions
                .Include(wb => wb.DrawActions)
                    .ThenInclude(da => da.User)
                .Where(wb => wb.RoomId == roomId && wb.CreatedAt > DateTime.UtcNow.AddHours(-24))
                .OrderByDescending(wb => wb.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<bool> EndSessionAsync(Guid sessionId)
        {
            try
            {
                var session = await _db.WhiteboardSessions.FindAsync(sessionId);
                if (session == null) return false;

                // Mark session as ended (soft delete)
                session.CreatedAt = DateTime.UtcNow.AddHours(-25); // Make it inactive
                await _db.SaveChangesAsync();

                _logger.LogInformation("Ended whiteboard session {SessionId}", sessionId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ending whiteboard session {SessionId}", sessionId);
                return false;
            }
        }

        // ======== DRAWING ACTIONS ========
        
        public async Task<DrawAction> AddDrawActionAsync(Guid sessionId, int userId, string actionType, string payload)
        {
            try
            {
                _logger.LogInformation("Adding draw action: Type={ActionType}, User={UserId}, Session={SessionId}", 
                    actionType, userId, sessionId);
                
                // Check if session exists
                var session = await _db.WhiteboardSessions.FindAsync(sessionId);
                if (session == null)
                {
                    _logger.LogError("Session {SessionId} not found", sessionId);
                    throw new InvalidOperationException($"Session {sessionId} not found");
                }
                
                _logger.LogInformation("Session found: {SessionId}", sessionId);
                
                var action = new DrawAction
                {
                    WbSessionId = sessionId,
                    UserId = userId,
                    ActionType = actionType,
                    Payload = payload,
                    Timestamp = DateTime.UtcNow
                };

                _logger.LogInformation("Created DrawAction object: {ActionType}", actionType);
                
                _db.DrawActions.Add(action);
                _logger.LogInformation("Added action to DbContext");
                
                await _db.SaveChangesAsync();
                _logger.LogInformation("Saved changes successfully");

                _logger.LogDebug("Added draw action {ActionType} for user {UserId} in session {SessionId}", 
                    actionType, userId, sessionId);

                return action;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding draw action for user {UserId} in session {SessionId}", 
                    userId, sessionId);
                throw;
            }
        }

        public async Task<List<DrawAction>> GetDrawActionsAsync(Guid sessionId, DateTime? since = null)
        {
            var query = _db.DrawActions
                .Include(da => da.User)
                .Where(da => da.WbSessionId == sessionId);

            if (since.HasValue)
            {
                query = query.Where(da => da.Timestamp > since.Value);
            }

            return await query
                .OrderBy(da => da.Timestamp)
                .ToListAsync();
        }

        public async Task<bool> ClearWhiteboardAsync(Guid sessionId, int userId)
        {
            try
            {
                // Add a clear action
                var clearAction = new DrawAction
                {
                    WbSessionId = sessionId,
                    UserId = userId,
                    ActionType = "clear",
                    Payload = JsonSerializer.Serialize(new { clearedBy = userId, timestamp = DateTime.UtcNow }),
                    Timestamp = DateTime.UtcNow
                };

                _db.DrawActions.Add(clearAction);
                await _db.SaveChangesAsync();

                _logger.LogInformation("Whiteboard cleared by user {UserId} in session {SessionId}", 
                    userId, sessionId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing whiteboard for user {UserId} in session {SessionId}", 
                    userId, sessionId);
                return false;
            }
        }

        // ======== COLLABORATION ========
        
        public async Task<bool> IsUserInSessionAsync(Guid sessionId, int userId)
        {
            return await _db.DrawActions
                .AnyAsync(da => da.WbSessionId == sessionId && da.UserId == userId);
        }

        public async Task<List<int>> GetSessionParticipantsAsync(Guid sessionId)
        {
            return await _db.DrawActions
                .Where(da => da.WbSessionId == sessionId)
                .Select(da => da.UserId)
                .Distinct()
                .ToListAsync();
        }

        // ======== SESSION SAVING ========
        
        public async Task<bool> SaveSessionAsync(Guid sessionId, int userId, string? sessionName = null)
        {
            try
            {
                var session = await _db.WhiteboardSessions.FindAsync(sessionId);
                if (session == null) return false;

                // Update session with save info
                session.UpdatedAt = DateTime.UtcNow;
                session.LastSavedBy = userId;
                if (!string.IsNullOrEmpty(sessionName))
                {
                    session.SessionName = sessionName;
                }

                await _db.SaveChangesAsync();

                _logger.LogInformation("Session {SessionId} saved by user {UserId}", sessionId, userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving session {SessionId} for user {UserId}", sessionId, userId);
                return false;
            }
        }

        public async Task<List<WhiteboardSession>> GetUserSessionsAsync(int userId, int limit = 20)
        {
            try
            {
                return await _db.WhiteboardSessions
                    .Include(wb => wb.DrawActions)
                    .Where(wb => wb.CreatedByUserId == userId && wb.CreatedAt > DateTime.UtcNow.AddDays(-30))
                    .OrderByDescending(wb => wb.UpdatedAt ?? wb.CreatedAt)
                    .Take(limit)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sessions for user {UserId}", userId);
                return new List<WhiteboardSession>();
            }
        }

        // ======== TEMPLATES & SAVING ========
        
        public async Task<string> SaveAsTemplateAsync(Guid sessionId, string templateName, int userId, string? description = null, bool isPublic = false)
        {
            try
            {
                var actions = await GetDrawActionsAsync(sessionId);
                
                // Create template data
                var templateData = JsonSerializer.Serialize(new
                {
                    name = templateName,
                    actions = actions.Select(a => new
                    {
                        actionType = a.ActionType,
                        payload = a.Payload,
                        timestamp = a.Timestamp
                    }),
                    createdBy = userId,
                    createdAt = DateTime.UtcNow
                });

                // Generate thumbnail from current canvas state
                var thumbnail = await GenerateThumbnailAsync(sessionId);

                // Save to database
                var template = new WhiteboardTemplate
                {
                    Name = templateName,
                    Data = templateData,
                    CreatedByUserId = userId,
                    CreatedAt = DateTime.UtcNow,
                    IsPublic = isPublic,
                    Description = description,
                    Thumbnail = thumbnail
                };

                _db.WhiteboardTemplates.Add(template);
                await _db.SaveChangesAsync();

                _logger.LogInformation("Template '{TemplateName}' saved by user {UserId} for session {SessionId}", 
                    templateName, userId, sessionId);

                return templateData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving template '{TemplateName}' for user {UserId}", 
                    templateName, userId);
                throw;
            }
        }

        public async Task<List<WhiteboardTemplate>> GetTemplatesAsync(int userId)
        {
            try
            {
                return await _db.WhiteboardTemplates
                    .Include(t => t.CreatedByUser)
                    .Where(t => t.CreatedByUserId == userId || t.IsPublic)
                    .OrderByDescending(t => t.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting templates for user {UserId}", userId);
                return new List<WhiteboardTemplate>();
            }
        }

        public async Task<WhiteboardTemplate?> GetTemplateByIdAsync(int templateId, int userId)
        {
            try
            {
                return await _db.WhiteboardTemplates
                    .Include(t => t.CreatedByUser)
                    .FirstOrDefaultAsync(t => t.Id == templateId && 
                                            (t.CreatedByUserId == userId || t.IsPublic));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting template {TemplateId} for user {UserId}", templateId, userId);
                return null;
            }
        }

        public async Task<bool> LoadTemplateAsync(Guid sessionId, int templateId, int userId)
        {
            try
            {
                var template = await _db.WhiteboardTemplates
                    .FirstOrDefaultAsync(t => t.Id == templateId && 
                                            (t.CreatedByUserId == userId || t.IsPublic));

                if (template == null)
                {
                    _logger.LogWarning("Template {TemplateId} not found for user {UserId}", 
                        templateId, userId);
                    return false;
                }

                // Parse template data and replay actions
                var templateData = JsonSerializer.Deserialize<dynamic>(template.Data);
                var actions = templateData.GetProperty("actions");

                foreach (var action in actions.EnumerateArray())
                {
                    var actionType = action.GetProperty("actionType").GetString();
                    var payload = action.GetProperty("payload").GetString();
                    
                    await AddDrawActionAsync(sessionId, userId, actionType, payload);
                }

                _logger.LogInformation("Template {TemplateId} loaded by user {UserId} in session {SessionId}", 
                    templateId, userId, sessionId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading template {TemplateId} for user {UserId}", 
                    templateId, userId);
                return false;
            }
        }

        public async Task<bool> LoadTemplateByNameAsync(Guid sessionId, string templateName, int userId)
        {
            try
            {
                var template = await _db.WhiteboardTemplates
                    .FirstOrDefaultAsync(t => t.Name == templateName && 
                                            (t.CreatedByUserId == userId || t.IsPublic));

                if (template == null)
                {
                    _logger.LogWarning("Template '{TemplateName}' not found for user {UserId}", 
                        templateName, userId);
                    return false;
                }

                // Parse template data and replay actions
                var templateData = JsonSerializer.Deserialize<dynamic>(template.Data);
                var actions = templateData.GetProperty("actions");

                foreach (var action in actions.EnumerateArray())
                {
                    var actionType = action.GetProperty("actionType").GetString();
                    var payload = action.GetProperty("payload").GetString();
                    
                    await AddDrawActionAsync(sessionId, userId, actionType, payload);
                }

                _logger.LogInformation("Template '{TemplateName}' loaded by user {UserId} in session {SessionId}", 
                    templateName, userId, sessionId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading template '{TemplateName}' for user {UserId}", 
                    templateName, userId);
                return false;
            }
        }

        private async Task<string> GenerateThumbnailAsync(Guid sessionId)
        {
            try
            {
                // In a real implementation, you'd render the canvas to a small image
                // For now, return a placeholder
                return "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating thumbnail for session {SessionId}", sessionId);
                return string.Empty;
            }
        }

        public async Task<bool> DeleteTemplateAsync(int templateId, int userId)
        {
            try
            {
                var template = await _db.WhiteboardTemplates
                    .FirstOrDefaultAsync(t => t.Id == templateId && t.CreatedByUserId == userId);

                if (template == null)
                {
                    _logger.LogWarning("Template {TemplateId} not found or user {UserId} not authorized to delete", 
                        templateId, userId);
                    return false;
                }

                _db.WhiteboardTemplates.Remove(template);
                await _db.SaveChangesAsync();

                _logger.LogInformation("Template {TemplateId} deleted by user {UserId}", templateId, userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting template {TemplateId} for user {UserId}", templateId, userId);
                return false;
            }
        }

        // ======== EXPORT ========
        
        public async Task<byte[]> ExportAsImageAsync(Guid sessionId, string format = "png")
        {
            try
            {
                // In a real implementation, you'd render the whiteboard to an image
                // For now, return a placeholder
                _logger.LogInformation("Exporting whiteboard session {SessionId} as {Format}", 
                    sessionId, format);
                
                // Placeholder: return a simple PNG
                return new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }; // PNG header
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting whiteboard session {SessionId} as image", sessionId);
                throw;
            }
        }

        public async Task<byte[]> ExportAsPDFAsync(Guid sessionId)
        {
            try
            {
                // In a real implementation, you'd render the whiteboard to PDF
                _logger.LogInformation("Exporting whiteboard session {SessionId} as PDF", sessionId);
                
                // Placeholder: return a simple PDF
                return new byte[] { 0x25, 0x50, 0x44, 0x46 }; // PDF header
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting whiteboard session {SessionId} as PDF", sessionId);
                throw;
            }
        }

        // ======== STATISTICS ========
        
        public async Task<WhiteboardStats> GetSessionStatsAsync(Guid sessionId)
        {
            try
            {
                var actions = await _db.DrawActions
                    .Include(da => da.User)
                    .Where(da => da.WbSessionId == sessionId)
                    .ToListAsync();

                var session = await _db.WhiteboardSessions.FindAsync(sessionId);
                var sessionDuration = session != null ? DateTime.UtcNow - session.CreatedAt : TimeSpan.Zero;

                var actionTypes = actions
                    .GroupBy(a => a.ActionType)
                    .ToDictionary(g => g.Key, g => g.Count());

                var userActivities = actions
                    .GroupBy(a => new { a.UserId, a.User.FullName })
                    .Select(g => new UserActivity
                    {
                        UserId = g.Key.UserId,
                        UserName = g.Key.FullName ?? "Unknown",
                        ActionCount = g.Count(),
                        LastActivity = g.Max(a => a.Timestamp)
                    })
                    .OrderByDescending(ua => ua.ActionCount)
                    .ToList();

                return new WhiteboardStats
                {
                    TotalActions = actions.Count,
                    ActiveUsers = userActivities.Count,
                    SessionDuration = sessionDuration,
                    ActionTypes = actionTypes,
                    UserActivities = userActivities
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting stats for whiteboard session {SessionId}", sessionId);
                throw;
            }
        }
    }
} 