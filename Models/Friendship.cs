using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Zela.Models;

/// <summary>
/// Mối quan hệ bạn bè (Friendship) giữa hai User.
/// </summary>
public class Friendship
{
    [Key]
    public int FriendshipId { get; set; }          // PK: FriendshipId INT IDENTITY(1,1)

    public int UserId1 { get; set; }               // FK -> Requester (User)
    public int UserId2 { get; set; }               // FK -> Addressee (User)

    public DateTime CreatedAt { get; set; }        // DATETIME
    public int StatusId { get; set; }              // FK -> Status

    [ForeignKey(nameof(UserId1))]
    public User Requester { get; set; }            // User gửi lời mời

    [ForeignKey(nameof(UserId2))]
    public User Addressee { get; set; }           // User nhận lời mời

    [ForeignKey(nameof(StatusId))]
    public Status Status { get; set; }             // Trạng thái
}