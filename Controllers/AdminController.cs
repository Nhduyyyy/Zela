using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zela.DbContext;
using Zela.Models;

namespace Zela.Controllers;

[Authorize(Roles = "Admin")] // Chỉ cho phép người dùng có Role = "Admin"
public class AdminController : Controller
{
    private readonly ApplicationDbContext _db;

    public AdminController(ApplicationDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Trang dashboard: thống kê nhanh.
    /// </summary>
    public async Task<IActionResult> Index()
    {
        ViewBag.UserCount    = await _db.Users.CountAsync();
        ViewBag.GroupCount   = await _db.ChatGroups.CountAsync();
        ViewBag.RoomCount    = await _db.VideoRooms.CountAsync();
        ViewBag.StickerCount = await _db.Stickers.CountAsync();
        ViewBag.MessageCount = await _db.Messages.CountAsync();

        return View();
    }

    /// <summary>
    /// Danh sách người dùng kèm role.
    /// </summary>
    public async Task<IActionResult> Users()
    {
        var users = await _db.Users
            .Include(u => u.Roles)
            .OrderBy(u => u.UserId)
            .ToListAsync();

        return View(users);
    }

    /// <summary>
    /// Gán quyền Admin cho 1 user.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Promote(int userId)
    {
        // Kiểm tra đã có role Admin chưa
        var exists = await _db.Roles.AnyAsync(r => r.UserId == userId && r.RoleName == "Admin");
        if (!exists)
        {
            _db.Roles.Add(new Role
            {
                UserId = userId,
                RoleName = "Admin",
                CreateAt = DateTime.Now
            });
            await _db.SaveChangesAsync();
        }

        return RedirectToAction("Index");
    }

    /// <summary>
    /// Gỡ bỏ quyền Admin khỏi user.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Demote(int userId)
    {
        var role = await _db.Roles.FirstOrDefaultAsync(r => r.UserId == userId && r.RoleName == "Admin");
        if (role != null)
        {
            _db.Roles.Remove(role);
            await _db.SaveChangesAsync();
        }
        return RedirectToAction("Index");
    }
} 