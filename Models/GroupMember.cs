using System.ComponentModel.DataAnnotations.Schema;

namespace Zela.Models;

/// <summary>
/// Liên kết User tham gia ChatGroup kèm quyền Moderator.
/// </summary>
public class GroupMember
{
    public int GroupId { get; set; }               // FK -> ChatGroup
    public int UserId { get; set; }                // FK -> User

    public bool IsModerator { get; set; }          // BIT
    public DateTime JoinedAt { get; set; }         // DATETIME2
    
    public bool IsBanned { get; set; }             // BIT - trạng thái bị ban
    public DateTime? BanUntil { get; set; }        // DATETIME2 - thời gian ban đến

    [ForeignKey(nameof(GroupId))]
    public ChatGroup ChatGroup { get; set; }
    [ForeignKey(nameof(UserId))]
    public User User { get; set; }
}