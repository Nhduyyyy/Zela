using Microsoft.EntityFrameworkCore;
using Zela.DbContext;
using Zela.Hubs;
using Zela.Models;
using Zela.Services.Dto;

namespace Zela.Services
{
    public class MeetingService : IMeetingService
    {
        private readonly ApplicationDbContext _db;
        
        public MeetingService(ApplicationDbContext db) 
            => _db = db;

        public async Task<string> CreateMeetingAsync(int creatorId)
        {
            string code;
            do {
                code = RandomString(10);
            } while (await _db.VideoRooms.AnyAsync(r => r.Password == code));

            var room = new VideoRoom
            {
                CreatorId = creatorId,
                IsOpen    = true,
                CreatedAt = DateTime.UtcNow,
                Password  = code,
                Name      = "Meeting-" + code
            };
            _db.VideoRooms.Add(room);
            await _db.SaveChangesAsync();
            return code;
        }

        public async Task<JoinResult> JoinMeetingAsync(string password)
        {
            var room = await _db.VideoRooms
                .FirstOrDefaultAsync(r => r.Password == password && r.IsOpen);
            if (room == null)
                return new JoinResult { Success = false, Error = "Mã phòng không hợp lệ" };

            var peers = MeetingHub.GetPeersInRoom(password);
            return new JoinResult { Success = true, Peers = peers };
        }
        
        public async Task<bool> IsHostAsync(string password, int userId)
        {
            // Tìm phòng theo password
            var room = await _db.VideoRooms
                .FirstOrDefaultAsync(r => r.Password == password);
            // Host nếu phòng tồn tại và creatorId trùng
            return room != null && room.CreatorId == userId;
        }

        public async Task CloseMeetingAsync(string password)
        {
            var room = await _db.VideoRooms
                .FirstOrDefaultAsync(r => r.Password == password);
            if (room == null) return;

            room.IsOpen = false;
            _db.VideoRooms.Update(room);
            await _db.SaveChangesAsync();
        }
        
        private static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var rng = new Random();
            return new string(Enumerable.Range(0, length)
                .Select(_ => chars[rng.Next(chars.Length)]).ToArray());
        }
    }
}