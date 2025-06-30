using Microsoft.AspNetCore.SignalR;
using Zela.Services.Interface;
using System.Text.Json;

namespace Zela.Hubs
{
    public class WhiteboardHub : Hub
    {
        private readonly IWhiteboardService _whiteboardService;
        private readonly ILogger<WhiteboardHub> _logger;
        
        // Track active whiteboard sessions
        private static readonly Dictionary<string, Guid> userSessions = new();
        private static readonly Dictionary<Guid, HashSet<string>> sessionUsers = new();

        public WhiteboardHub(IWhiteboardService whiteboardService, ILogger<WhiteboardHub> logger)
        {
            _whiteboardService = whiteboardService;
            _logger = logger;
        }

        // ======== CONNECTION MANAGEMENT ========

        public override async Task OnConnectedAsync()
        {
            _logger.LogInformation("Whiteboard client connected: {ConnectionId}", Context.ConnectionId);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            // Remove user from session tracking
            if (userSessions.TryGetValue(Context.ConnectionId, out var sessionId))
            {
                userSessions.Remove(Context.ConnectionId);
                
                if (sessionUsers.TryGetValue(sessionId, out var users))
                {
                    users.Remove(Context.ConnectionId);
                    if (users.Count == 0)
                    {
                        sessionUsers.Remove(sessionId);
                    }
                }
            }

            _logger.LogInformation("Whiteboard client disconnected: {ConnectionId}", Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }

        // ======== SESSION MANAGEMENT ========

        public async Task JoinWhiteboardSession(string sessionId, int userId)
        {
            try
            {
                var guidSessionId = Guid.Parse(sessionId);
                
                // Track user in session
                userSessions[Context.ConnectionId] = guidSessionId;
                
                if (!sessionUsers.ContainsKey(guidSessionId))
                {
                    sessionUsers[guidSessionId] = new HashSet<string>();
                }
                sessionUsers[guidSessionId].Add(Context.ConnectionId);

                // Add to SignalR group
                await Groups.AddToGroupAsync(Context.ConnectionId, sessionId);

                // Notify other users
                await Clients.OthersInGroup(sessionId).SendAsync("UserJoinedWhiteboard", userId);

                _logger.LogInformation("User {UserId} joined whiteboard session {SessionId}", userId, sessionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error joining whiteboard session {SessionId}", sessionId);
                throw;
            }
        }

        public async Task LeaveWhiteboardSession(string sessionId, int userId)
        {
            try
            {
                var guidSessionId = Guid.Parse(sessionId);
                
                // Remove from tracking
                userSessions.Remove(Context.ConnectionId);
                
                if (sessionUsers.TryGetValue(guidSessionId, out var users))
                {
                    users.Remove(Context.ConnectionId);
                    if (users.Count == 0)
                    {
                        sessionUsers.Remove(guidSessionId);
                    }
                }

                // Remove from SignalR group
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, sessionId);

                // Notify other users
                await Clients.OthersInGroup(sessionId).SendAsync("UserLeftWhiteboard", userId);

                _logger.LogInformation("User {UserId} left whiteboard session {SessionId}", userId, sessionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error leaving whiteboard session {SessionId}", sessionId);
                throw;
            }
        }

        // ======== DRAWING ACTIONS ========

        public async Task DrawAction(string sessionId, string actionType, string payload, int userId)
        {
            try
            {
                var guidSessionId = Guid.Parse(sessionId);
                
                // Save action to database
                var action = await _whiteboardService.AddDrawActionAsync(guidSessionId, userId, actionType, payload);

                // Broadcast to all users in session
                await Clients.OthersInGroup(sessionId).SendAsync("DrawActionReceived", new
                {
                    actionId = action.ActionId,
                    actionType = action.ActionType,
                    payload = action.Payload,
                    timestamp = action.Timestamp,
                    userId = action.UserId
                });

                _logger.LogDebug("Draw action {ActionType} broadcasted in session {SessionId}", actionType, sessionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing draw action in session {SessionId}", sessionId);
                throw;
            }
        }

        public async Task ClearWhiteboard(string sessionId, int userId)
        {
            try
            {
                var guidSessionId = Guid.Parse(sessionId);
                
                // Save clear action to database
                var success = await _whiteboardService.ClearWhiteboardAsync(guidSessionId, userId);

                if (success)
                {
                    // Broadcast clear action to all users
                    await Clients.OthersInGroup(sessionId).SendAsync("WhiteboardCleared", userId);
                    
                    _logger.LogInformation("Whiteboard cleared by user {UserId} in session {SessionId}", userId, sessionId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing whiteboard in session {SessionId}", sessionId);
                throw;
            }
        }

        // ======== CURSOR TRACKING ========

        public async Task UpdateCursor(string sessionId, int userId, double x, double y)
        {
            try
            {
                // Broadcast cursor position to other users
                await Clients.OthersInGroup(sessionId).SendAsync("CursorUpdated", new
                {
                    userId = userId,
                    x = x,
                    y = y,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating cursor in session {SessionId}", sessionId);
                throw;
            }
        }

        // ======== COLLABORATION FEATURES ========

        public async Task RequestControl(string sessionId, int userId)
        {
            try
            {
                // Broadcast control request
                await Clients.OthersInGroup(sessionId).SendAsync("ControlRequested", new
                {
                    userId = userId,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error requesting control in session {SessionId}", sessionId);
                throw;
            }
        }

        public async Task GrantControl(string sessionId, int userId, int targetUserId)
        {
            try
            {
                // Broadcast control granted
                await Clients.Group(sessionId).SendAsync("ControlGranted", new
                {
                    fromUserId = userId,
                    toUserId = targetUserId,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error granting control in session {SessionId}", sessionId);
                throw;
            }
        }

        // ======== TEMPLATE SHARING ========

        public async Task ShareTemplate(string sessionId, int userId, string templateName, string templateData)
        {
            try
            {
                // Broadcast template to all users
                await Clients.OthersInGroup(sessionId).SendAsync("TemplateShared", new
                {
                    userId = userId,
                    templateName = templateName,
                    templateData = templateData,
                    timestamp = DateTime.UtcNow
                });

                _logger.LogInformation("Template '{TemplateName}' shared by user {UserId} in session {SessionId}", 
                    templateName, userId, sessionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sharing template in session {SessionId}", sessionId);
                throw;
            }
        }

        // ======== CHAT & ANNOTATIONS ========

        public async Task AddAnnotation(string sessionId, int userId, string annotation)
        {
            try
            {
                // Broadcast annotation to all users
                await Clients.Group(sessionId).SendAsync("AnnotationAdded", new
                {
                    userId = userId,
                    annotation = annotation,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding annotation in session {SessionId}", sessionId);
                throw;
            }
        }

        // ======== UTILITY METHODS ========

        public async Task GetSessionParticipants(string sessionId)
        {
            try
            {
                var guidSessionId = Guid.Parse(sessionId);
                var participants = await _whiteboardService.GetSessionParticipantsAsync(guidSessionId);
                
                await Clients.Caller.SendAsync("SessionParticipants", participants);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting session participants for {SessionId}", sessionId);
                throw;
            }
        }

        public async Task Ping(string sessionId)
        {
            try
            {
                await Clients.Caller.SendAsync("Pong", DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ping for session {SessionId}", sessionId);
                throw;
            }
        }
    }
} 