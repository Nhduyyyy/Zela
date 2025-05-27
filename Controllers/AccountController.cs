using Microsoft.AspNetCore.Mvc;
using Zela.Models;

namespace Web_Demo_Home.Controllers;

public class AccountController : Controller 
{
    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Login(LoginModel model)
    {
        // Giả lập đăng nhập
        if (model.Username == "admin" && model.Password == "123456")
        {
            // Đăng nhập thành công
            return RedirectToAction("Index", "Messenger");
        }

        // Đăng nhập thất bại
        ViewBag.Error = "Sai tài khoản hoặc mật khẩu.";
        return View();
    }
}