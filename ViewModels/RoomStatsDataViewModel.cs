namespace Zela.ViewModels
{
    public class RoomStatsDataViewModel
    {
        public bool HasActiveSession { get; set; }
        public SessionInfoViewModel? Session { get; set; }
        public RoomInfoViewModel RoomInfo { get; set; } = new();
        public ParticipantsStatsViewModel? Participants { get; set; }
    }

    public class SessionInfoViewModel
    {
        public Guid SessionId { get; set; }
        public DateTime StartedAt { get; set; }
        public double DurationMinutes { get; set; }
        public bool HasRecording { get; set; }
    }

    public class RoomInfoViewModel
    {
        public string Name { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int CreatorId { get; set; }
    }

    public class ParticipantsStatsViewModel
    {
        public int Total { get; set; }
        public int Active { get; set; }
        public int Left { get; set; }
        public List<ActiveParticipantViewModel> ActiveList { get; set; } = new();
        public List<LeftParticipantViewModel> LeftList { get; set; } = new();
    }

    public class ActiveParticipantViewModel
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public DateTime JoinTime { get; set; }
        public double DurationMinutes { get; set; }
    }

    public class LeftParticipantViewModel
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public DateTime JoinTime { get; set; }
        public DateTime? LeaveTime { get; set; }
        public double DurationMinutes { get; set; }
    }
} 