using System.Collections.Concurrent;
using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using Zela.Services;

namespace Zela.Hubs
{
    public class MeetingHub : Hub
    {
        static ConcurrentDictionary<string, HashSet<string>> rooms = new();
        static ConcurrentDictionary<string, string>        hosts = new();

        private readonly IMeetingService _meetingService;
        public MeetingHub(IMeetingService meetingService)
        {
            _meetingService = meetingService;
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            foreach (var kv in rooms)
            {
                if (kv.Value.Remove(Context.ConnectionId) && kv.Value.Count == 0)
                {
                    rooms.TryRemove(kv.Key, out _);
                    hosts.TryRemove(kv.Key, out _);
                }
            }
            return base.OnDisconnectedAsync(exception);
        }
        
        public async Task JoinRoom(string password)
        {
            var conns = rooms.GetOrAdd(password, _ => new HashSet<string>());
            lock (conns) conns.Add(Context.ConnectionId);
            await Groups.AddToGroupAsync(Context.ConnectionId, password);

            // Lần đầu ai join thì set làm host
            hosts.TryAdd(password, Context.ConnectionId);

            var existing = conns.Where(id => id != Context.ConnectionId).ToList();
            await Clients.Caller.SendAsync("Peers", existing);
            await Clients.OthersInGroup(password).SendAsync("NewPeer", Context.ConnectionId);
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
        
        public async Task LeaveRoom(string room)
        {
            if (rooms.TryGetValue(room, out var conns))
            {
                lock(conns)
                    conns.Remove(Context.ConnectionId);
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, room);
                if (conns.Count == 0)
                    rooms.TryRemove(room, out _);
            }
        }

        public async Task EndRoom(string password)
        {
            // 1) Cập nhật DB: đóng phòng
            await _meetingService.CloseMeetingAsync(password);

            // 2) Broadcast cho tất cả client trong group event “CallEnded”
            await Clients.Group(password)
                .SendAsync("CallEnded");
        }
    }
}