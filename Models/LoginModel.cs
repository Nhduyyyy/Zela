using System.ComponentModel.DataAnnotations;

namespace Zela.Models;

public class LoginModel
{
    
    [Required(ErrorMessage = "Vui lòng nhập tài khoản")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    public string Username { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
    [DataType(DataType.Password)]
    public string Password { get; set; }

    public bool RememberMe { get; set; }
}