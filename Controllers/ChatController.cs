using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Zela.Services;
using Zela.ViewModels;

namespace Zela.Controllers
{
    /// <summary>
    /// Controller xử lý các chức năng chat: danh sách bạn, tin nhắn, sticker, sidebar.
    /// Chỉ cho phép user đã xác thực truy cập.
    /// </summary>
    [Authorize]
    public class ChatController : Controller
    {
        // Service chứa logic chat (lấy bạn, tin nhắn, etc.)
        private readonly IChatService _chatService;

        // Service để lấy sticker
        private readonly IStickerService _stickerService;

        // Context của SignalR Hub để gửi sự kiện real-time
        private readonly IHubContext<ChatHub> _hubContext;

        /// <summary>
        /// Khởi tạo ChatController với DI cho các service cần thiết.
        /// </summary>
        public ChatController(
            IChatService chatService,
            IStickerService stickerService,
            IHubContext<ChatHub> hubContext)
        {
            _chatService = chatService; // Gán chat service
            _stickerService = stickerService; // Gán sticker service
            _hubContext = hubContext; // Gán hub context để gửi real-time 
        }

        /// <summary>
        // /// Trang chính chat, render danh sách bạn bè.
        // /// </summary>
        // /// <returns>View với model danh sách bạn</returns>
        public async Task<IActionResult> Index()
        {
            // Lấy userId từ Session, nếu null thì 0
            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            // Gọi service lấy danh sách bạn
            var friends = await _chatService.GetFriendListAsync(userId);
            // Trả về view Index.cshtml với danh sách bạn
            return View(friends);
        }

        /// <summary>
        /// Trả về partial view chứa lịch sử tin nhắn 1-1 với friendId.
        /// </summary>
        /// <param name="friendId">ID của bạn chat</param>
        /// <returns>PartialView _ChatMessagesPartial với model messages</returns>
        [HttpGet]
        public async Task<IActionResult> GetMessages(int friendId)
        {
            // Lấy userId từ Session
            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            // Gọi service lấy tin nhắn giữa user và friendId
            var messages = await _chatService.GetMessagesAsync(userId, friendId);
            // Trả về partial view để client render
            return PartialView("_ChatMessagesPartial", messages);
        }

        /// <summary>
        /// Xử lý gửi tin nhắn (text hoặc nhiều file).
        /// Gửi real-time qua SignalR sau khi lưu.
        /// </summary>
        /// <param name="friendId">ID bạn chat</param>
        /// <param name="content">Nội dung text</param>
        /// <param name="files">Danh sách file đính kèm (có thể null)</param>
        /// <returns>Ok 200 hoặc lỗi 500</returns>
        [HttpPost]
        public async Task<IActionResult> SendMessage(int friendId, string content, List<IFormFile> files)
        {
            try
            {
                // Lấy userId
                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                // Gọi service lưu tin nhắn với nhiều file, nhận về MessageViewModel
                var msgVm = await _chatService.SendMessageAsync(userId, friendId, content, files);

                // Gửi tin nhắn về phía sender để cập nhật UI
                await _hubContext.Clients.User(userId.ToString()).SendAsync("ReceiveMessage", msgVm);
                // Nếu không tự chat với chính mình, gửi cho bạn chat
                if (userId != friendId)
                    await _hubContext.Clients.User(friendId.ToString()).SendAsync("ReceiveMessage", msgVm);
                // Trả về 200 OK
                return Ok();
            }
            catch (Exception ex)
            {
                // Log lỗi đơn giản ra console (có thể cải thiện dùng ILogger)
                Console.WriteLine("Lỗi gửi tin nhắn: " + ex);
                // Trả về HTTP 500 kèm message lỗi
                return StatusCode(500, ex.Message);
            }
        }

        ///<summary>
        /// Lấy danh sách sticker có sẵn dưới dạng JSON.
        /// </summary>
        /// <returns>JSON array sticker</returns>
        [HttpGet]
        public async Task<IActionResult> GetStickers()
        {
            // Gọi service lấy danh sách sticker
            var stickers = await _stickerService.GetAvailableStickersAsync();
            // Trả về JSON
            return Json(stickers);
        }
        
        /// <summary>
        /// Lấy dữ liệu bạn chat để render sidebar phải của chat window.
        /// </summary>
        /// <param name="friendId">ID bạn chat</param>
        /// <returns>PartialView _SidebarRight với FriendViewModel</returns>
        [HttpGet]
        public async Task<IActionResult> GetFriendSidebar(int friendId)
        {
            // Lấy userId hiện tại
            int currentUserId = HttpContext.Session.GetInt32("UserId") ?? 0;
            // Nếu chưa login, trả về BadRequest
            if (currentUserId == 0)
            {
                return BadRequest();
            }
            
            // Tìm user friend
            var friend = await _chatService.FindUserByIdAsync(friendId);
            // Nếu không tồn tại, trả NotFound
            if (friend == null)
            {
                return NotFound();
            }
            
            // Map dữ liệu sang ViewModel
            var friendViewModel = new FriendViewModel()
            {
                UserId = friend.UserId,
                FullName = friend.FullName,
                AvatarUrl = friend.AvatarUrl,
                Email = friend.Email,
                // Xác định online nếu hoạt động trong 5 phút qua
                IsOnline = friend.LastLoginAt > DateTime.UtcNow.AddMinutes(-5),
                // Placeholder, có thể lấy tin nhắn gần nhất
                LastMessage = "", // Có thể lấy từ service
                // Format giờ phút
                LastTime = friend.LastLoginAt.ToString("HH:mm")
            };
            
            // Trả về partial view sidebar
            return PartialView("_SidebarRight", friendViewModel);
        }
    }
}
