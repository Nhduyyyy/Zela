using Microsoft.AspNetCore.Mvc;
using Zela.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Zela.Hubs;

namespace Zela.Controllers
{
    [Authorize]
    public class ChatController : Controller
    {
        private readonly IChatService _chatService;
        private readonly IHubContext<ChatHub> _hubContext;
        public ChatController(IChatService chatService, IHubContext<ChatHub> hubContext)
        {
            _chatService = chatService;
            _hubContext = hubContext;
        }

        // Trang chat – render danh sách bạn bè
        public async Task<IActionResult> Index()
        {
            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            var friends = await _chatService.GetFriendListAsync(userId);
            return View(friends);
        }

        // Trả về partial chứa lịch sử chat 1-1 với friendId
        [HttpGet]
        public async Task<IActionResult> GetMessages(int friendId)
        {
            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            var messages = await _chatService.GetMessagesAsync(userId, friendId);
            // messages là List<MessageViewModel>
            return PartialView("_ChatMessagesPartial", messages);
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage(int friendId, string content, IFormFile file)
        {
            try
            {
                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                var msgVm = await _chatService.SendMessageAsync(userId, friendId, content, file);
                // Broadcast message mới cho cả hai phía qua SignalR
                await _hubContext.Clients.User(userId.ToString()).SendAsync("ReceiveMessage", msgVm);
                if (userId != friendId)
                    await _hubContext.Clients.User(friendId.ToString()).SendAsync("ReceiveMessage", msgVm);
                return Ok();
            }
            catch (Exception ex)
            {
                // Log lỗi ra console hoặc file
                Console.WriteLine("Lỗi gửi tin nhắn: " + ex);
                return StatusCode(500, ex.Message);
            }
        }
    }
}