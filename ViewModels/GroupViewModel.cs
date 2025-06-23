using System.ComponentModel.DataAnnotations;

namespace Zela.ViewModels;

public class GroupViewModel
{
    public int GroupId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string LastMessage { get; set; }
    public string LastTime { get; set; }
    public int MemberCount { get; set; }
    public string AvatarUrl { get; set; } = "/images/default-group-avatar.png";
    public bool IsOnline { get; set; } = true; // Groups luôn online
    public DateTime CreatedAt { get; set; }
    public string CreatorName { get; set; }
}