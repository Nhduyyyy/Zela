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
}