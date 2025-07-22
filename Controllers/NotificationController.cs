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
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            // We need a new service method to get ALL notifications
            // For now, let's reuse the latest notifications method
            var notifications = await _notificationService.GetLatestNotificationsAsync(userId.Value); 
            
            // Mark all as read when visiting the page
            await _notificationService.MarkAllAsReadAsync(userId.Value);

            return View(notifications);
        }

        [HttpGet("api/notifications")]
        public async Task<IActionResult> GetLatestNotifications()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return Unauthorized();

            var notifications = await _notificationService.GetLatestNotificationsAsync(userId.Value);
            return Ok(notifications);
        }

        [HttpPost("{id}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            await _notificationService.MarkAsReadAsync(id);
            return Ok();
        }

        [HttpPost("read-all")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return Unauthorized();

            await _notificationService.MarkAllAsReadAsync(userId.Value);
            return Ok();
        }
    }
} 