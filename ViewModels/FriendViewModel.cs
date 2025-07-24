namespace Zela.ViewModels;

public class FriendViewModel
{
    public int UserId { get; set; }
    public string FullName { get; set; }
    public string AvatarUrl { get; set; }
    public string Email { get; set; }
    public bool IsOnline { get; set; }
    public string LastMessage { get; set; }
    public string LastTime { get; set; }
    
    // Thêm thuộc tính để lưu trữ media của cuộc hội thoại
    public List<MediaViewModel> Images { get; set; } = new List<MediaViewModel>();
    public List<MediaViewModel> Videos { get; set; } = new List<MediaViewModel>();
    public List<MediaViewModel> Files { get; set; } = new List<MediaViewModel>();
}