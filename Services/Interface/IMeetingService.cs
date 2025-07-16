using Zela.Services.Dto;
using Zela.Models;
using Zela.ViewModels;

namespace Zela.Services.Interface
{
    public interface IMeetingService
    {
        Task<string> CreateMeetingAsync(int creatorId);
        Task<JoinResult> JoinMeetingAsync(string password);
        
        Task<bool> IsHostAsync(string password, int userId);
        
        Task CloseMeetingAsync(string password);
        
        // ======== NEW METHODS FOR CALL SESSION MANAGEMENT ========
        Task<Guid> StartCallSessionAsync(string password);
        Task EndCallSessionAsync(string password);
        Task<CallSession?> GetActiveSessionAsync(string password);
        Task<VideoRoom?> GetRoomByCodeAsync(string code);
        
        // ======== ATTENDANCE TRACKING ========
        Task TrackUserJoinAsync(Guid sessionId, int userId);
        Task TrackUserLeaveAsync(Guid sessionId, int userId);
        
        // ======== RECORDING MANAGEMENT ========
        Task SaveRecordingUrlAsync(Guid sessionId, string recordingUrl);
        
        // ======== STATISTICS & REPORTS ========
        Task<List<CallSession>> GetCallHistoryAsync(string password);
        Task<List<Attendance>> GetAttendanceReportAsync(Guid sessionId);
        Task<Dictionary<string, object>> GetCallStatisticsAsync(string password);
        
        // ======== ROOM STATS FOR IN-MEETING VIEW ========
        Task<RoomStatsDataViewModel> GetRoomStatsDataAsync(string password);
    }
}