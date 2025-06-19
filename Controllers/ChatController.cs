using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;
using Zela.Services;
using Zela.Hubs;

namespace Zela.Controllers
{
    [Authorize]
    public class ChatController : Controller
    {
        private readonly IChatService _chatService;
        private readonly IStickerService _stickerService;
        private readonly IHubContext<ChatHub> _hubContext;

        public ChatController(
            IChatService chatService,
            IStickerService stickerService,
            IHubContext<ChatHub> hubContext)
        {
            _chatService = chatService;
            _stickerService = stickerService;
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
            return PartialView("_ChatMessagesPartial", messages);
        }

        // Gửi tin nhắn (text hoặc file đính kèm)
        [HttpPost]
        public async Task<IActionResult> SendMessage(int friendId, string content, IFormFile file)
        {
            try
            {
                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                var msgVm = await _chatService.SendMessageAsync(userId, friendId, content, file);

                // Gửi tin nhắn qua SignalR
                await _hubContext.Clients.User(userId.ToString()).SendAsync("ReceiveMessage", msgVm);
                if (userId != friendId)
                    await _hubContext.Clients.User(friendId.ToString()).SendAsync("ReceiveMessage", msgVm);

                return Ok();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi gửi tin nhắn: " + ex);
                return StatusCode(500, ex.Message);
            }
        }

        // Lấy danh sách sticker có sẵn
        [HttpGet]
        public async Task<IActionResult> GetStickers()
        {
            var stickers = await _stickerService.GetAvailableStickersAsync();
            return Json(stickers);
        }
    }
}
