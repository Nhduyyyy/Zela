using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Zela.Services;

namespace Zela.Controllers
{
    [Authorize]
    public class ChatController : Controller
    {
        private readonly IChatService _chatService;
        public ChatController(IChatService chatService)
        {
            _chatService = chatService;
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
    }
}