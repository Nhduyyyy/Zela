namespace Zela.ViewModels;

public class UserViewModel
{
    public int UserId { get; set; }
    public string FullName { get; set; }
    public string Email { get; set; }
    public string AvatarUrl { get; set; }
    public bool IsOnline { get; set; }
    public DateTime? LastLoginAt { get; set; }
}