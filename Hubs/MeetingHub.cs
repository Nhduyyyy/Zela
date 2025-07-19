using System.Collections.Concurrent;
using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using Zela.Services.Interface;

namespace Zela.Hubs
{
    public class MeetingHub : Hub
    {
        static ConcurrentDictionary<string, HashSet<string>> rooms = new();
        static ConcurrentDictionary<string, string> hosts = new();
        static ConcurrentDictionary<string, Guid> activeSessions = new(); // Track active sessions
        static ConcurrentDictionary<string, int> userConnections = new(); // Map connectionId to userId

        private readonly IMeetingService _meetingService;
        public MeetingHub(IMeetingService meetingService)
        {
            _meetingService = meetingService;
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            // Find which room this connection was in
            string roomPassword = null;
            foreach (var kv in rooms)
            {
                if (kv.Value.Contains(Context.ConnectionId))
                {
                    roomPassword = kv.Key;
                    break;
                }
            }

            // Track user leave if we have session info
            if (roomPassword != null && 
                activeSessions.TryGetValue(roomPassword, out var sessionId) && 
                userConnections.TryGetValue(Context.ConnectionId, out var userId))
            {
                try
                {
                    await _meetingService.TrackUserLeaveAsync(sessionId, userId);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error tracking user leave: {ex.Message}");
                }
            }

            // Clean up connection tracking
            userConnections.TryRemove(Context.ConnectionId, out _);

            // Remove from rooms
            foreach (var kv in rooms)
            {
                if (kv.Value.Remove(Context.ConnectionId) && kv.Value.Count == 0)
                {
                    rooms.TryRemove(kv.Key, out _);
                    hosts.TryRemove(kv.Key, out _);
                    
                    // End session when last person leaves
                    if (activeSessions.TryRemove(kv.Key, out _))
                    {
                        try
                        {
                            await _meetingService.EndCallSessionAsync(kv.Key);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error ending session: {ex.Message}");
                        }
                    }
                }
            }
            
            await base.OnDisconnectedAsync(exception);
        }
        
        public async Task JoinRoom(string password, int userId)
        {
            try
            {
                // Track user connection
                userConnections[Context.ConnectionId] = userId;

            var conns = rooms.GetOrAdd(password, _ => new HashSet<string>());
                bool isFirstJoin = false;
                
                lock (conns) 
                {
                    isFirstJoin = conns.Count == 0;
                    conns.Add(Context.ConnectionId);
                }
                
            await Groups.AddToGroupAsync(Context.ConnectionId, password);

                // Set host for first person
            hosts.TryAdd(password, Context.ConnectionId);

                // Start call session if this is the first person joining
                if (isFirstJoin)
                {
                    try
                    {
                        var sessionId = await _meetingService.StartCallSessionAsync(password);
                        activeSessions[password] = sessionId;
                        Console.WriteLine($"Started call session {sessionId} for room {password}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error starting call session: {ex.Message}");
                    }
                }

                // Track user attendance
                if (activeSessions.TryGetValue(password, out var currentSessionId))
                {
                    try
                    {
                        await _meetingService.TrackUserJoinAsync(currentSessionId, userId);
                        Console.WriteLine($"Tracked user {userId} join to session {currentSessionId}");
                        
                        // Broadcast stats update
                        await BroadcastStatsUpdate(password);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error tracking user join: {ex.Message}");
                    }
                }

            var existing = conns.Where(id => id != Context.ConnectionId).ToList();
            await Clients.Caller.SendAsync("Peers", existing);
            await Clients.OthersInGroup(password).SendAsync("NewPeer", Context.ConnectionId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in JoinRoom: {ex.Message}");
                throw;
            }
        }

        public async Task Signal(string toConnectionId, object data)
        {
            await Clients.Client(toConnectionId)
                .SendAsync("Signal", Context.ConnectionId, data);
        }

        // Phương thức helper để service JoinMeetingAsync dùng
        public static List<string> GetPeersInRoom(string password)
        {
            if (rooms.TryGetValue(password, out var conns))
            {
                lock (conns) return conns.ToList();
            }
            return new List<string>();
        }
        
        public async Task LeaveRoom(string room, int userId)
        {
            try
            {
                // Track user leave
                if (activeSessions.TryGetValue(room, out var sessionId))
                {
                    try
                    {
                        await _meetingService.TrackUserLeaveAsync(sessionId, userId);
                        Console.WriteLine($"Tracked user {userId} leave from session {sessionId}");
                        
                        // Broadcast stats update
                        await BroadcastStatsUpdate(room);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error tracking user leave: {ex.Message}");
                    }
                }

                // Remove from room
                if (rooms.TryGetValue(room, out var conns))
            {
                lock(conns)
                    conns.Remove(Context.ConnectionId);
                    
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, room);
                    
                if (conns.Count == 0)
                    {
                    rooms.TryRemove(room, out _);
                        hosts.TryRemove(room, out _);
                        
                        // End session when last person leaves
                        if (activeSessions.TryRemove(room, out _))
                        {
                            try
                            {
                                await _meetingService.EndCallSessionAsync(room);
                                Console.WriteLine($"Ended call session for room {room}");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error ending session: {ex.Message}");
                            }
                        }
                    }
                }

                // Clean up user connection tracking
                userConnections.TryRemove(Context.ConnectionId, out _);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in LeaveRoom: {ex.Message}");
                throw;
            }
        }

        public async Task EndRoom(string password)
        {
            try
            {
                // 1) End call session first
                if (activeSessions.TryRemove(password, out var sessionId))
                {
                    try
                    {
                        await _meetingService.EndCallSessionAsync(password);
                        Console.WriteLine($"Ended call session {sessionId} for room {password}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error ending call session: {ex.Message}");
                    }
                }

                // 2) Close room in database
            await _meetingService.CloseMeetingAsync(password);

                // 3) Clean up in-memory tracking
                rooms.TryRemove(password, out _);
                hosts.TryRemove(password, out _);

                // 4) Broadcast to all clients
                await Clients.Group(password).SendAsync("CallEnded");
                
                Console.WriteLine($"Room {password} ended successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in EndRoom: {ex.Message}");
                throw;
            }
        }

        // ======== NEW METHODS FOR RECORDING ========
        
        public async Task SaveRecording(string password, string recordingUrl)
        {
            try
            {
                if (activeSessions.TryGetValue(password, out var sessionId))
                {
                    await _meetingService.SaveRecordingUrlAsync(sessionId, recordingUrl);
                    Console.WriteLine($"Saved recording URL for session {sessionId}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving recording: {ex.Message}");
            }
        }

        // ======== METHODS FOR GETTING STATISTICS ========
        
        public async Task GetCallHistory(string password)
        {
            try
            {
                var history = await _meetingService.GetCallHistoryAsync(password);
                await Clients.Caller.SendAsync("CallHistory", history);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting call history: {ex.Message}");
            }
        }

        public async Task GetCallStatistics(string password)
        {
            try
            {
                var stats = await _meetingService.GetCallStatisticsAsync(password);
                await Clients.Caller.SendAsync("CallStatistics", stats);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting call statistics: {ex.Message}");
            }
        }

        // ======== STATS BROADCASTING ========
        
        private async Task BroadcastStatsUpdate(string password)
        {
            try
            {
                // Broadcast to anyone listening to stats updates for this room
                await Clients.Group($"stats-{password}").SendAsync("StatsUpdated");
                Console.WriteLine($"Broadcasted stats update for room {password}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error broadcasting stats update: {ex.Message}");
            }
        }

        public async Task JoinStatsRoom(string password)
        {
            try
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"stats-{password}");
                Console.WriteLine($"Client joined stats room for {password}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error joining stats room: {ex.Message}");
            }
        }

        public async Task LeaveStatsRoom(string password)
        {
            try
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"stats-{password}");
                Console.WriteLine($"Client left stats room for {password}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error leaving stats room: {ex.Message}");
            }
        }

        // ======== SUBTITLE SHARING METHODS ========
        
        public async Task JoinSubtitleGroup(string sessionId)
        {
            try
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"subtitle_{sessionId}");
                Console.WriteLine($"User joined subtitle group for session {sessionId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error joining subtitle group: {ex.Message}");
            }
        }

        public async Task LeaveSubtitleGroup(string sessionId)
        {
            try
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"subtitle_{sessionId}");
                Console.WriteLine($"User left subtitle group for session {sessionId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error leaving subtitle group: {ex.Message}");
            }
        }

        public async Task SendSubtitle(string sessionId, object subtitle)
        {
            try
            {
                await Clients.Group($"subtitle_{sessionId}").SendAsync("ReceiveSubtitle", subtitle);
                Console.WriteLine($"Subtitle sent to group subtitle_{sessionId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending subtitle: {ex.Message}");
            }
        }

        public async Task UserSubtitleToggled(string sessionId, int userId, bool enabled)
        {
            try
            {
                await Clients.Group($"subtitle_{sessionId}").SendAsync("UserSubtitleToggled", userId, enabled);
                Console.WriteLine($"User {userId} subtitle toggled: {enabled} for session {sessionId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error broadcasting subtitle toggle: {ex.Message}");
            }
        }
    }
}