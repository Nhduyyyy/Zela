using System.ComponentModel.DataAnnotations;

namespace Zela.ViewModels;

public class EditGroupViewModel
{
    public int GroupId { get; set; }

    [Required(ErrorMessage = "Tên nhóm là bắt buộc")]
    [StringLength(100, ErrorMessage = "Tên nhóm không được vượt quá 100 ký tự")]
    public string Name { get; set; }

    [StringLength(50, ErrorMessage = "Mô tả không được vượt quá 50 ký tự")]
    public string? Description { get; set; }

    [StringLength(50, ErrorMessage = "Mật khẩu không được vượt quá 50 ký tự")]
    public string? Password { get; set; }

    public string? AvatarUrl { get; set; }

    public Zela.Enum.RoomType RoomType { get; set; }
}