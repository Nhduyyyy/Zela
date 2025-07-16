using Microsoft.EntityFrameworkCore;
using Zela.DbContext;
using Zela.Hubs;
using Zela.Models;
using Zela.Services.Dto;
using Zela.Services.Interface;
using Zela.ViewModels;
using Zela.Enum;

namespace Zela.Services
{
    public class MeetingService : IMeetingService
    {
        private readonly ApplicationDbContext _db;

        public MeetingService(ApplicationDbContext db)
            => _db = db;

        // Phương thức tạo một phòng họp mới, trả về mã phòng (code) vừa tạo
        public async Task<string> CreateMeetingAsync(int creatorId)
        {
            string code;
            // Sinh mã phòng ngẫu nhiên 10 ký tự, lặp lại cho đến khi chắc chắn không trùng với bất kỳ phòng nào đã có trong database
            do
            {
                code = RandomString(10); // Tạo chuỗi ngẫu nhiên 10 ký tự
            } while (await _db.VideoRooms.AnyAsync(r => r.Password == code));  // Kiểm tra xem đã có phòng nào dùng mã này chưa

            // Tạo đối tượng phòng họp mới với các thông tin cần thiết
            var room = new VideoRoom
            {
                CreatorId = creatorId, // Gán ID người tạo phòng
                IsOpen = true, // Đánh dấu phòng đang mở
                CreatedAt = DateTime.UtcNow, // Lưu thời điểm tạo phòng (giờ UTC)
                Password = code, // Gán mã phòng vừa tạo cho trường Password
                Name = "Meeting-" + code, // Đặt tên phòng theo mẫu "Meeting-xxxxxx"
                
                // ======== NEW FIELDS WITH DEFAULT VALUES ========
                RoomType = RoomType.Public,
                MaxParticipants = 50,
                RecordingPolicy = RecordingPolicy.HostOnly,
                AllowScreenShare = true,
                AllowChat = true,
                AllowVideo = true,
                AllowAudio = true,
                RequireAuthentication = false,
                AllowJoinBeforeHost = true,
                AutoRecord = false,
                EndWhenHostLeaves = false,
                AutoEndDelay = 5,
                IsLocked = false,
                WaitingRoomEnabled = false
            };

            // Thêm phòng họp mới vào database
            _db.VideoRooms.Add(room);
            // Lưu thay đổi vào database (bất đồng bộ)
            await _db.SaveChangesAsync();
            // Trả về mã phòng để controller/action sử dụng (ví dụ: chuyển hướng người dùng vào phòng họp này)
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

        // Kiểm tra xem user có phải là host (người tạo) của phòng họp với mã phòng (password) cho trước hay không
        public async Task<bool> IsHostAsync(string password, int userId)
        {
            // Tìm phòng họp trong database dựa trên password (mã phòng)
            var room = await _db.VideoRooms
                .FirstOrDefaultAsync(r => r.Password == password);

            // Kiểm tra:
            // - Nếu tìm thấy phòng (room != null)
            // - Và CreatorId của phòng trùng với userId truyền vào (tức là user này là người tạo phòng)
            // => Trả về true (user là host), ngược lại trả về false
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
                RecordingUrl = "", // Will be updated later when recording is available
                
                // ======== NEW FIELDS WITH DEFAULT VALUES ========
                SessionType = SessionType.Normal,
                ParticipantCount = 0,
                MessageCount = 0,
                PollCount = 0,
                HandRaiseCount = 0,
                CreatedBy = room.CreatorId
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

        public async Task<VideoRoom?> GetRoomByCodeAsync(string code)
        {
            return await _db.VideoRooms
                .FirstOrDefaultAsync(r => r.Password == code);
        }

        // ======== ATTENDANCE TRACKING ========

        public async Task TrackUserJoinAsync(Guid sessionId, int userId)
        {
            // Get room info from session
            var session = await _db.CallSessions
                .Include(cs => cs.VideoRoom)
                .FirstOrDefaultAsync(cs => cs.SessionId == sessionId);
            
            if (session == null) return;

            // Check if user is already in this session (prevent duplicates)
            var existingAttendance = await _db.Attendances
                .FirstOrDefaultAsync(a => a.SessionId == sessionId &&
                                   a.UserId == userId &&
                                   a.LeaveTime == null);

            if (existingAttendance != null) return; // Already joined

            // Add attendance record
            var attendance = new Attendance
            {
                SessionId = sessionId,
                UserId = userId,
                JoinTime = DateTime.UtcNow
            };

            _db.Attendances.Add(attendance);

            // Add/Update RoomParticipant record
            var existingParticipant = await _db.RoomParticipants
                .FirstOrDefaultAsync(rp => rp.RoomId == session.RoomId && rp.UserId == userId);

            if (existingParticipant == null)
            {
                // Check if this is the first participant (host)
                var isFirstParticipant = !await _db.RoomParticipants
                    .AnyAsync(rp => rp.RoomId == session.RoomId);

                var participant = new RoomParticipant
                {
                    RoomId = session.RoomId,
                    UserId = userId,
                    IsModerator = false,
                    IsHost = isFirstParticipant, // true nếu là người đầu tiên, false nếu không
                    JoinedAt = DateTime.UtcNow,
                    
                    // ======== NEW FIELDS WITH DEFAULT VALUES ========
                    Status = ParticipantStatus.Joined,
                    CurrentVideoQuality = VideoQuality.Medium,
                    IsVideoEnabled = true,
                    IsAudioEnabled = true,
                    IsScreenSharing = false,
                    IsHandRaised = false,
                    IsMutedByHost = false,
                    IsVideoDisabledByHost = false,
                    LastActivityAt = DateTime.UtcNow
                };

                _db.RoomParticipants.Add(participant);
                
                // Log để debug
                Console.WriteLine($"User {userId} joined room {session.RoomId} as {(isFirstParticipant ? "HOST" : "PARTICIPANT")}");
            }
            else
            {
                // Update existing participant - đảm bảo IsHost = false cho người tham gia lại
                existingParticipant.Status = ParticipantStatus.Joined;
                existingParticipant.LeftAt = null;
                existingParticipant.LeaveReason = null;
                existingParticipant.IsHost = false; // Người tham gia lại không phải host
                existingParticipant.LastActivityAt = DateTime.UtcNow;
                
                _db.RoomParticipants.Update(existingParticipant);
                
                Console.WriteLine($"User {userId} rejoined room {session.RoomId} as PARTICIPANT");
            }

            // Update session participant count
            session.ParticipantCount = await _db.RoomParticipants
                .CountAsync(rp => rp.RoomId == session.RoomId && rp.Status == ParticipantStatus.Joined);

            await _db.SaveChangesAsync();
        }

        public async Task TrackUserLeaveAsync(Guid sessionId, int userId)
        {
            // Get room info from session
            var session = await _db.CallSessions
                .Include(cs => cs.VideoRoom)
                .FirstOrDefaultAsync(cs => cs.SessionId == sessionId);
            
            if (session == null) return;

            // Update attendance record
            var attendance = await _db.Attendances
                .FirstOrDefaultAsync(a => a.SessionId == sessionId &&
                                   a.UserId == userId &&
                                   a.LeaveTime == null);

            if (attendance != null)
            {
                attendance.LeaveTime = DateTime.UtcNow;
                _db.Attendances.Update(attendance);
            }

            // Update RoomParticipant record
            var participant = await _db.RoomParticipants
                .FirstOrDefaultAsync(rp => rp.RoomId == session.RoomId && rp.UserId == userId);

            if (participant != null)
            {
                participant.Status = ParticipantStatus.Left;
                participant.LeftAt = DateTime.UtcNow;
                participant.LeaveReason = "User left the room";
                participant.LastActivityAt = DateTime.UtcNow;
                
                _db.RoomParticipants.Update(participant);
            }

            // Update session participant count
            session.ParticipantCount = await _db.RoomParticipants
                .CountAsync(rp => rp.RoomId == session.RoomId && rp.Status == ParticipantStatus.Joined);

            await _db.SaveChangesAsync();
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
                    ActiveList = activeParticipants.Select(a => new ActiveParticipantViewModel
                    {
                        UserId = a.UserId,
                        UserName = a.User?.FullName ?? "Unknown",
                        JoinTime = a.JoinTime,
                        DurationMinutes = Math.Round((DateTime.UtcNow - a.JoinTime).TotalMinutes, 1)
                    }).OrderBy(p => p.JoinTime).ToList(),
                    LeftList = leftParticipants.Select(a => new LeftParticipantViewModel
                    {
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
            // Khai báo chuỗi chứa tất cả các ký tự có thể dùng để sinh mã ngẫu nhiên (chữ hoa và số)
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

            // Tạo một đối tượng Random để sinh số ngẫu nhiên
            var rng = new Random();

            // Sinh ra một mảng ký tự ngẫu nhiên với độ dài 'length'
            // Enumerable.Range(0, length): Tạo ra một dãy số từ 0 đến length-1
            // .Select(_ => chars[rng.Next(chars.Length)]): Với mỗi số trong dãy, chọn ngẫu nhiên một ký tự từ chuỗi chars
            // .ToArray(): Chuyển kết quả thành mảng ký tự
            return new string(Enumerable.Range(0, length)
                .Select(_ => chars[rng.Next(chars.Length)]).ToArray());
        }
    }
}