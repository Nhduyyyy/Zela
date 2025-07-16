namespace Zela.Enum;

// ======== ROOM MANAGEMENT ENUMS ========

/// <summary>
/// Loại phòng video call
/// </summary>
public enum RoomType
{
    Public = 0,      // Phòng công khai
    Private = 1,     // Phòng riêng tư
    Scheduled = 2,   // Phòng đã lên lịch
    Recurring = 3    // Phòng định kỳ
}

/// <summary>
/// Chính sách ghi hình
/// </summary>
public enum RecordingPolicy
{
    Disabled = 0,    // Không cho phép ghi hình
    HostOnly = 1,    // Chỉ host được ghi hình
    Anyone = 2       // Ai cũng có thể ghi hình
}

/// <summary>
/// Trạng thái tham gia phòng
/// </summary>
public enum ParticipantStatus
{
    Waiting = 0,     // Đang chờ host approve
    Joined = 1,      // Đã tham gia
    Left = 2,        // Đã rời phòng
    Removed = 3      // Bị loại khỏi phòng
}

/// <summary>
/// Chất lượng video
/// </summary>
public enum VideoQuality
{
    Low = 0,         // 360p
    Medium = 1,      // 720p
    High = 2,        // 1080p
    Ultra = 3        // 4K
}

/// <summary>
/// Loại tin nhắn trong phòng
/// </summary>
public enum RoomMessageType
{
    Chat = 0,        // Tin nhắn chat
    System = 1,      // Tin nhắn hệ thống
    HandRaise = 2,   // Giơ tay
    Poll = 3,        // Bình chọn
    Breakout = 4     // Breakout room
}

/// <summary>
/// Loại phiên gọi
/// </summary>
public enum SessionType
{
    Normal = 0,      // Phiên bình thường
    Breakout = 1     // Phiên breakout room
}

// ======== EVENT TRACKING ENUMS ========

/// <summary>
/// Loại sự kiện trong phòng
/// </summary>
public enum RoomEventType
{
    // Participant events
    ParticipantJoined = 0,
    ParticipantLeft = 1,
    ParticipantMuted = 2,
    ParticipantUnmuted = 3,
    VideoDisabled = 4,
    VideoEnabled = 5,
    
    // Media events
    ScreenShareStarted = 6,
    ScreenShareStopped = 7,
    
    // Interaction events
    HandRaised = 8,
    HandLowered = 9,
    
    // Poll events
    PollCreated = 10,
    PollVoted = 11,
    PollEnded = 12,
    
    // Chat events
    MessageSent = 13,
    MessageDeleted = 14,
    
    // Room control events
    RoomLocked = 15,
    RoomUnlocked = 16,
    
    // Recording events
    RecordingStarted = 17,
    RecordingStopped = 18,
    
    // Breakout room events
    BreakoutRoomCreated = 19,
    BreakoutRoomJoined = 20,
    BreakoutRoomLeft = 21,
    
    // Security events
    ParticipantRemoved = 22,
    ParticipantApproved = 23,
    ParticipantRejected = 24,
    
    // Quality events
    QualityChanged = 25,
    ConnectionLost = 26,
    ConnectionRestored = 27
}

// ======== POLL ENUMS ========

/// <summary>
/// Trạng thái bình chọn
/// </summary>
public enum PollStatus
{
    Draft = 0,       // Nháp
    Active = 1,      // Đang hoạt động
    Ended = 2,       // Đã kết thúc
    Cancelled = 3    // Đã hủy
}

// ======== BREAKOUT ROOM ENUMS ========

/// <summary>
/// Trạng thái phòng nhóm nhỏ
/// </summary>
public enum BreakoutRoomStatus
{
    Created = 0,     // Đã tạo
    Active = 1,      // Đang hoạt động
    Ended = 2,       // Đã kết thúc
    Cancelled = 3    // Đã hủy
}

// ======== CONNECTION ENUMS ========

/// <summary>
/// Trạng thái kết nối
/// </summary>
public enum ConnectionStatus
{
    Disconnected = 0,
    Connecting = 1,
    Connected = 2,
    Reconnecting = 3,
    Failed = 4
}

/// <summary>
/// Loại kết nối
/// </summary>
public enum ConnectionType
{
    WebRTC = 0,      // WebRTC peer-to-peer
    TURN = 1,        // TURN server relay
    STUN = 2         // STUN server
}

// ======== NOTIFICATION ENUMS ========

/// <summary>
/// Loại thông báo
/// </summary>
public enum NotificationType
{
    Info = 0,        // Thông tin
    Success = 1,     // Thành công
    Warning = 2,     // Cảnh báo
    Error = 3,       // Lỗi
    System = 4       // Hệ thống
}

/// <summary>
/// Mức độ ưu tiên thông báo
/// </summary>
public enum NotificationPriority
{
    Low = 0,         // Thấp
    Normal = 1,      // Bình thường
    High = 2,        // Cao
    Urgent = 3       // Khẩn cấp
}
