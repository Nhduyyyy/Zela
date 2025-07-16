using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Zela.Enum;

namespace Zela.Models;

/// <summary>
/// Bình chọn trong phòng video call
/// </summary>
public class RoomPoll
{
    [Key]
    public long PollId { get; set; }
    
    public int RoomId { get; set; }                // FK -> VideoRoom
    public int CreatorId { get; set; }             // FK -> User (người tạo)
    public Guid SessionId { get; set; }            // FK -> CallSession
    
    [Required]
    [MaxLength(200)]
    public string Question { get; set; }           // Câu hỏi
    
    [MaxLength(1000)]
    public string? Description { get; set; }       // Mô tả
    
    public bool IsMultipleChoice { get; set; } = false;  // Cho phép chọn nhiều
    
    public bool IsAnonymous { get; set; } = false; // Bình chọn ẩn danh
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? StartedAt { get; set; }       // Thời gian bắt đầu
    
    public DateTime? EndedAt { get; set; }         // Thời gian kết thúc
    
    public PollStatus Status { get; set; } = PollStatus.Active;  // Trạng thái hoạt động
    
    public int TimeLimit { get; set; } = 0;        // Thời gian giới hạn (phút), 0 = không giới hạn
    
    // Navigation properties
    [ForeignKey(nameof(RoomId))]
    public VideoRoom Room { get; set; }
    
    [ForeignKey(nameof(CreatorId))]
    public User Creator { get; set; }
    
    [ForeignKey(nameof(SessionId))]
    public CallSession Session { get; set; }
    
    public ICollection<PollOption> Options { get; set; } = new List<PollOption>();
    public ICollection<PollVote> Votes { get; set; } = new List<PollVote>();
}

/// <summary>
/// Lựa chọn trong bình chọn
/// </summary>
public class PollOption
{
    [Key]
    public long OptionId { get; set; }
    
    public long PollId { get; set; }               // FK -> RoomPoll
    
    [Required]
    [MaxLength(200)]
    public string Text { get; set; }               // Nội dung lựa chọn
    
    public int OrderIndex { get; set; } = 0;       // Thứ tự hiển thị
    
    // Navigation properties
    [ForeignKey(nameof(PollId))]
    public RoomPoll Poll { get; set; }
    
    public ICollection<PollVote> Votes { get; set; } = new List<PollVote>();
}

/// <summary>
/// Phiếu bầu trong bình chọn
/// </summary>
public class PollVote
{
    [Key]
    public long VoteId { get; set; }
    
    public long PollId { get; set; }               // FK -> RoomPoll
    public long OptionId { get; set; }             // FK -> PollOption
    public int VoterId { get; set; }               // FK -> User
    
    public DateTime VotedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    [ForeignKey(nameof(PollId))]
    public RoomPoll Poll { get; set; }
    
    [ForeignKey(nameof(OptionId))]
    public PollOption Option { get; set; }
    
    [ForeignKey(nameof(VoterId))]
    public User Voter { get; set; }
}