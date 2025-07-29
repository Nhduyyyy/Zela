using Microsoft.AspNetCore.Mvc;
using Zela.Services.Interface;
using System.Threading.Tasks;

namespace Zela.Controllers
{
    // [ApiController] // Remove this to allow Views
    // [Route("api/notifications")] // We can keep this for API calls
    public class NotificationController : Controller
    {
        private readonly INotificationService _notificationService;

        public NotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpGet("/notifications")] // Route for the dedicated page
        public async Task<IActionResult> Index()
        {
            // Lấy userId từ session, nếu chưa đăng nhập thì chuyển hướng về trang đăng nhập
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            // Lấy danh sách thông báo mới nhất cho user (có thể cần mở rộng để lấy tất cả)
            var notifications = await _notificationService.GetLatestNotificationsAsync(userId.Value); 
            
            // Đánh dấu tất cả thông báo là đã đọc khi truy cập trang
            await _notificationService.MarkAllAsReadAsync(userId.Value);

            // Trả về view hiển thị danh sách thông báo
            return View(notifications);
        }

        [HttpGet("api/notifications")]
        public async Task<IActionResult> GetLatestNotifications()
        {
            // Lấy userId từ session, nếu chưa đăng nhập thì trả về Unauthorized
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return Unauthorized();

            // Lấy danh sách thông báo mới nhất cho user
            var notifications = await _notificationService.GetLatestNotificationsAsync(userId.Value);
            // Trả về dữ liệu dạng JSON
            return Ok(notifications);
        }

        [HttpPost("{id}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            // Đánh dấu một thông báo cụ thể là đã đọc
            await _notificationService.MarkAsReadAsync(id);
            return Ok();
        }

        [HttpPost("read-all")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            // Lấy userId từ session, nếu chưa đăng nhập thì trả về Unauthorized
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return Unauthorized();

            // Đánh dấu tất cả thông báo của user là đã đọc
            await _notificationService.MarkAllAsReadAsync(userId.Value);
            return Ok();
        }
    }
} 