using Microsoft.EntityFrameworkCore;
using Zela.DbContext;
using Zela.Hubs;
using Zela.Models;
using Zela.Services.Dto;
using Zela.Services.Interface;
using Zela.ViewModels;

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

        // ======== NEW METHODS FOR CALL SESSION MANAGEMENT ========
        
        public async Task<Guid> StartCallSessionAsync(string password)
        {
            var room = await _db.VideoRooms
                .FirstOrDefaultAsync(r => r.Password == password && r.IsOpen);
            
            if (room == null) 
                throw new InvalidOperationException("Room not found or closed");

            // Check if there's already an active session
            var existingSession = await _db.CallSessions
                .FirstOrDefaultAsync(cs => cs.RoomId == room.RoomId && cs.EndedAt == null);
            
            if (existingSession != null)
                return existingSession.SessionId; // Return existing session

            // Create new session
            var session = new CallSession
            {
                SessionId = Guid.NewGuid(),
                RoomId = room.RoomId,
                StartedAt = DateTime.UtcNow,
                RecordingUrl = "" // Will be updated later when recording is available
            };

            _db.CallSessions.Add(session);
            await _db.SaveChangesAsync();
            
            return session.SessionId;
        }

        public async Task EndCallSessionAsync(string password)
        {
            var room = await _db.VideoRooms
                .FirstOrDefaultAsync(r => r.Password == password);
            
            if (room == null) return;

            // Find active session
            var session = await _db.CallSessions
                .FirstOrDefaultAsync(cs => cs.RoomId == room.RoomId && cs.EndedAt == null);
            
            if (session != null)
            {
                session.EndedAt = DateTime.UtcNow;
                
                // Also update any attendances that haven't left yet
                var activeAttendances = await _db.Attendances
                    .Where(a => a.SessionId == session.SessionId && a.LeaveTime == null)
                    .ToListAsync();
                
                foreach (var attendance in activeAttendances)
                {
                    attendance.LeaveTime = DateTime.UtcNow;
                }
                
                await _db.SaveChangesAsync();
            }
        }

        public async Task<CallSession?> GetActiveSessionAsync(string password)
        {
            var room = await _db.VideoRooms
                .FirstOrDefaultAsync(r => r.Password == password);
            
            if (room == null) return null;

            return await _db.CallSessions
                .FirstOrDefaultAsync(cs => cs.RoomId == room.RoomId && cs.EndedAt == null);
        }

        // ======== ATTENDANCE TRACKING ========
        
        public async Task TrackUserJoinAsync(Guid sessionId, int userId)
        {
            // Check if user is already in this session (prevent duplicates)
            var existingAttendance = await _db.Attendances
                .FirstOrDefaultAsync(a => a.SessionId == sessionId && 
                                   a.UserId == userId && 
                                   a.LeaveTime == null);
            
            if (existingAttendance != null) return; // Already joined

            var attendance = new Attendance
            {
                SessionId = sessionId,
                UserId = userId,
                JoinTime = DateTime.UtcNow
            };

            _db.Attendances.Add(attendance);
            await _db.SaveChangesAsync();
        }

        public async Task TrackUserLeaveAsync(Guid sessionId, int userId)
        {
            var attendance = await _db.Attendances
                .FirstOrDefaultAsync(a => a.SessionId == sessionId && 
                               a.UserId == userId && 
                               a.LeaveTime == null);
            
            if (attendance != null)
            {
                attendance.LeaveTime = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }
        }

        // ======== RECORDING MANAGEMENT ========
        
        public async Task SaveRecordingUrlAsync(Guid sessionId, string recordingUrl)
        {
            var session = await _db.CallSessions.FindAsync(sessionId);
            if (session != null)
            {
                session.RecordingUrl = recordingUrl;
                await _db.SaveChangesAsync();
            }
        }

        // ======== STATISTICS & REPORTS ========
        
        public async Task<List<CallSession>> GetCallHistoryAsync(string password)
        {
            var room = await _db.VideoRooms
                .FirstOrDefaultAsync(r => r.Password == password);
            
            if (room == null) return new List<CallSession>();

            return await _db.CallSessions
                .Where(cs => cs.RoomId == room.RoomId)
                .Include(cs => cs.Attendances)
                    .ThenInclude(a => a.User)
                .OrderByDescending(cs => cs.StartedAt)
                .ToListAsync();
        }

        public async Task<List<Attendance>> GetAttendanceReportAsync(Guid sessionId)
        {
            return await _db.Attendances
                .Where(a => a.SessionId == sessionId)
                .Include(a => a.User)
                .OrderBy(a => a.JoinTime)
                .ToListAsync();
        }

        public async Task<Dictionary<string, object>> GetCallStatisticsAsync(string password)
        {
            var room = await _db.VideoRooms
                .FirstOrDefaultAsync(r => r.Password == password);
            
            if (room == null) return new Dictionary<string, object>();

            var sessions = await _db.CallSessions
                .Where(cs => cs.RoomId == room.RoomId)
                .Include(cs => cs.Attendances)
                .ToListAsync();

            var totalSessions = sessions.Count;
            var totalDuration = sessions
                .Where(s => s.EndedAt.HasValue)
                .Sum(s => (s.EndedAt!.Value - s.StartedAt).TotalMinutes);
            
            var totalAttendances = sessions.SelectMany(s => s.Attendances).Count();
            var uniqueParticipants = sessions
                .SelectMany(s => s.Attendances)
                .Select(a => a.UserId)
                .Distinct()
                .Count();

            var averageDuration = totalSessions > 0 ? totalDuration / totalSessions : 0;
            var averageParticipants = totalSessions > 0 ? (double)totalAttendances / totalSessions : 0;

            return new Dictionary<string, object>
            {
                ["totalSessions"] = totalSessions,
                ["totalDurationMinutes"] = Math.Round(totalDuration, 2),
                ["averageDurationMinutes"] = Math.Round(averageDuration, 2),
                ["totalAttendances"] = totalAttendances,
                ["uniqueParticipants"] = uniqueParticipants,
                ["averageParticipants"] = Math.Round(averageParticipants, 2),
                ["lastSessionDate"] = sessions.OrderByDescending(s => s.StartedAt).FirstOrDefault()?.StartedAt,
                ["roomCreatedDate"] = room.CreatedAt
            };
        }
        
        // ======== ROOM STATS FOR IN-MEETING VIEW ========
        
        public async Task<RoomStatsDataViewModel> GetRoomStatsDataAsync(string password)
        {
            var room = await _db.VideoRooms.FirstOrDefaultAsync(r => r.Password == password);
            if (room == null)
                throw new InvalidOperationException("Phòng không tồn tại");

            // Lấy session hiện tại
            var currentSession = await _db.CallSessions
                .Include(cs => cs.Attendances)
                    .ThenInclude(a => a.User)
                .FirstOrDefaultAsync(cs => cs.RoomId == room.RoomId && cs.EndedAt == null);

            if (currentSession == null)
            {
                return new RoomStatsDataViewModel
                {
                    HasActiveSession = false,
                    RoomInfo = new RoomInfoViewModel
                    {
                        Name = room.Name,
                        Password = room.Password,
                        CreatedAt = room.CreatedAt,
                        CreatorId = room.CreatorId
                    }
                };
            }

            // Thông tin session hiện tại
            var duration = (DateTime.UtcNow - currentSession.StartedAt).TotalMinutes;
            var attendances = currentSession.Attendances.ToList();
            var activeParticipants = attendances.Where(a => a.LeaveTime == null).ToList();
            var leftParticipants = attendances.Where(a => a.LeaveTime != null).ToList();

            return new RoomStatsDataViewModel
            {
                HasActiveSession = true,
                Session = new SessionInfoViewModel
                {
                    SessionId = currentSession.SessionId,
                    StartedAt = currentSession.StartedAt,
                    DurationMinutes = Math.Round(duration, 1),
                    HasRecording = !string.IsNullOrEmpty(currentSession.RecordingUrl)
                },
                RoomInfo = new RoomInfoViewModel
                {
                    Name = room.Name,
                    Password = room.Password,
                    CreatedAt = room.CreatedAt,
                    CreatorId = room.CreatorId
                },
                Participants = new ParticipantsStatsViewModel
                {
                    Total = attendances.Count,
                    Active = activeParticipants.Count,
                    Left = leftParticipants.Count,
                    ActiveList = activeParticipants.Select(a => new ActiveParticipantViewModel {
                        UserId = a.UserId,
                        UserName = a.User?.FullName ?? "Unknown",
                        JoinTime = a.JoinTime,
                        DurationMinutes = Math.Round((DateTime.UtcNow - a.JoinTime).TotalMinutes, 1)
                    }).OrderBy(p => p.JoinTime).ToList(),
                    LeftList = leftParticipants.Select(a => new LeftParticipantViewModel {
                        UserId = a.UserId,
                        UserName = a.User?.FullName ?? "Unknown",
                        JoinTime = a.JoinTime,
                        LeaveTime = a.LeaveTime,
                        DurationMinutes = a.LeaveTime.HasValue 
                            ? Math.Round((a.LeaveTime.Value - a.JoinTime).TotalMinutes, 1)
                            : 0
                    }).OrderByDescending(p => p.LeaveTime).ToList()
                }
            };
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