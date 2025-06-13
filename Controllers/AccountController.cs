using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Google;
using Zela.DbContext;
using Zela.Models;

namespace Zela.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _db;

        public AccountController(ApplicationDbContext db)
        {
            _db = db;
        }

        // Trang Login riêng
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // Khi bấm nút Google Login (gọi bằng form POST)
        [HttpPost]
        public IActionResult GoogleLogin(string returnUrl = "/Chat/Index")
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = Url.Action("GoogleResponse", new { ReturnUrl = returnUrl })
            };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        // Google callback về đây
        [HttpGet]
        public async Task<IActionResult> GoogleResponse(string returnUrl = "/Chat/Index")
        {
            var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            if (!result.Succeeded)
                return RedirectToAction("Login");

            var email = result.Principal.FindFirst(ClaimTypes.Email)?.Value;
            var fullName = result.Principal.FindFirst(ClaimTypes.Name)?.Value;
            var avatarUrl = result.Principal.FindFirst("picture")?.Value;

            // Nếu không có avatar từ Google, dùng ảnh mặc định
            if (string.IsNullOrEmpty(avatarUrl))
                avatarUrl = "/images/default-avatar.jpeg";

            // Nếu Google không trả về tên, thay thế bằng email (hoặc để chuỗi rỗng nếu muốn)
            if (string.IsNullOrEmpty(fullName))
                fullName = email ?? "";

            if (string.IsNullOrEmpty(email))
                return RedirectToAction("Login");

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                user = new User
                {
                    Email = email,
                    FullName = fullName,
                    AvatarUrl = avatarUrl,
                    CreatedAt = DateTime.Now,
                    LastLoginAt = DateTime.Now,
                    IsPremium = false,
                    // Các navigation property không set ở đây, chỉ set khi thực sự add entity liên quan
                };
                _db.Users.Add(user);
            }
            else
            {
                user.LastLoginAt = DateTime.Now;
                // Có thể update thêm avatar, fullname nếu muốn đồng bộ mỗi lần login Google
                if (!string.IsNullOrEmpty(fullName)) user.FullName = fullName;
                if (!string.IsNullOrEmpty(avatarUrl)) user.AvatarUrl = avatarUrl;
            }
            await _db.SaveChangesAsync();

            HttpContext.Session.SetInt32("UserId", user.UserId);
            HttpContext.Session.SetString("FullName", user.FullName ?? "");
            HttpContext.Session.SetString("AvatarUrl", user.AvatarUrl ?? "");
            
            var claims = result.Principal.Claims.ToList();
            claims.Add(new Claim("UserId", user.UserId.ToString()));

            var identity  = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal);
            return Redirect(returnUrl);
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            return RedirectToAction("Login","Account");
        }
    }
}
