using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Zela.Models;

/// <summary>
/// Nhóm chat (ChatGroup).
/// </summary>
public class ChatGroup
{
    public ChatGroup()
    {
        Members = new List<GroupMember>();
        Messages = new List<Message>();
    }

    [Key]
    public int GroupId { get; set; }               // PK: GroupId INT IDENTITY(1,1)

    public bool IsOpen { get; set; }               // BIT
    public int CreatorId { get; set; }             // FK -> User tạo nhóm
    public DateTime CreatedAt { get; set; }        // DATETIME2

    [MaxLength(100)]
    public string Name { get; set; }               // NVARCHAR(100)

    [MaxLength(50)]
    public string Description { get; set; }        // NVARCHAR(50) NULL

    [MaxLength(500)]
    public string AvatarUrl { get; set; }          // NVARCHAR(500) NULL

    [ForeignKey(nameof(CreatorId))]
    public User Creator { get; set; }              // Navigation về User

    public ICollection<GroupMember> Members { get; set; }
    public ICollection<Message> Messages { get; set; }
}