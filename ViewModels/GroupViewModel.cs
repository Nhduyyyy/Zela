using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

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
    public int CreatorId { get; set; } // ID của người tạo nhóm
    public string CreatorName { get; set; }
    public List<UserViewModel> Members { get; set; } = new List<UserViewModel>();
    
    // Thêm thuộc tính để lưu trữ media của nhóm
    public List<MediaViewModel> Images { get; set; } = new List<MediaViewModel>();
    public List<MediaViewModel> Videos { get; set; } = new List<MediaViewModel>();
    public List<MediaViewModel> Files { get; set; } = new List<MediaViewModel>();
}