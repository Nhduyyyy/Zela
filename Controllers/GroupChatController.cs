using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Zela.Services;
using Zela.Models;
using Zela.ViewModels;
using System.IO;
using System.Text.Json;

namespace Zela.Controllers
{
    [Authorize]
    public class GroupChatController : Controller
    {
        private readonly IChatService _chatService;

        public GroupChatController(IChatService chatService)
        {
            _chatService = chatService;
        }

        // Trang danh sách nhóm
        public async Task<IActionResult> Index()
        {
            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            var groups = await _chatService.GetUserGroupsAsync(userId);
            return View(groups);
        }

        // Trả về JSON chứa lịch sử chat nhóm
        [HttpGet]
        public async Task<IActionResult> GetGroupMessages(int groupId)
        {
            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            var messages = await _chatService.GetGroupMessagesAsync(groupId);
            
            // Set IsMine for each message
            foreach (var msg in messages)
            {
                msg.IsMine = msg.SenderId == userId;
                
                // Update reactions to show if current user has reacted
                foreach (var reaction in msg.Reactions)
                {
                    reaction.HasUserReaction = await _chatService.HasUserReactionAsync(msg.MessageId, userId, reaction.ReactionType);
                }
            }
            
            return Json(messages);
        }

        // Gửi tin nhắn nhóm với file
        [HttpPost]
        [RequestSizeLimit(50 * 1024 * 1024)] // 50MB limit
        public async Task<IActionResult> SendGroupMessage(int groupId, string content, List<IFormFile> files, long? replyToMessageId = null)
        {
            try
            {
                int senderId = HttpContext.Session.GetInt32("UserId") ?? 0;
                if (senderId == 0)
                {
                    return Unauthorized(new { message = "Không tìm thấy thông tin người dùng" });
                }

                // Validate input
                if (string.IsNullOrWhiteSpace(content) && (files == null || files.Count == 0))
                {
                    return BadRequest(new { message = "Vui lòng nhập nội dung tin nhắn hoặc chọn file" });
                }

                var message = await _chatService.SendGroupMessageAsync(senderId, groupId, content, files, replyToMessageId);
                return Ok(new { success = true, message = message });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending group message: {ex.Message}");
                return StatusCode(500, new { message = "Có lỗi xảy ra khi gửi tin nhắn. Vui lòng thử lại." });
            }
        }

        // Tạo nhóm chat mới (hỗ trợ avatar, password, friendIds)
        [HttpPost]
        public async Task<IActionResult> CreateGroup([FromForm] string name, [FromForm] string description, [FromForm] IFormFile avatar, [FromForm] string password, [FromForm] string friendIds)
        {
            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            var (success, message, groupVm) = await _chatService.CreateGroupWithAvatarAndFriendsAsync(userId, name, description, avatar, password, friendIds);
            if (!success)
                return BadRequest(new { message });
            return Json(new { success = true, group = groupVm });
        }

        // Thêm thành viên vào nhóm (đã chuyển toàn bộ logic sang service)
        [HttpPost]
        public async Task<IActionResult> AddMember(int groupId, int userId)
        {
            int currentUserId = HttpContext.Session.GetInt32("UserId") ?? 0;
            var (success, message) = await _chatService.AddMemberWithValidationAsync(groupId, userId, currentUserId);
            if (!success)
                return BadRequest(new { message });
            return Ok(new { message });
        }

        // Xóa thành viên khỏi nhóm
        [HttpPost]
        public async Task<IActionResult> RemoveMember(int groupId, int userId)
        {
            await _chatService.RemoveMemberFromGroupAsync(groupId, userId);
            return Ok();
        }

        // Lấy chi tiết nhóm
        [HttpGet]
        public async Task<IActionResult> GetGroupDetails(int groupId)
        {
            var group = await _chatService.GetGroupDetailsAsync(groupId);
            return Json(group);
        }

        // Cập nhật thông tin nhóm
        [HttpPost]
        public async Task<IActionResult> UpdateGroup(int groupId, string name, string description)
        {
            await _chatService.UpdateGroupAsync(groupId, name, description);
            return Ok();
        }

        // Xóa nhóm
        [HttpPost]
        public async Task<IActionResult> DeleteGroup(int groupId)
        {
            await _chatService.DeleteGroupAsync(groupId);
            return Ok();
        }

        // Tìm kiếm người dùng để thêm vào nhóm (đã chuyển lọc thành viên sang service)
        [HttpGet]
        public async Task<IActionResult> SearchUsers(string searchTerm, int? groupId = null)
        {
            int currentUserId = HttpContext.Session.GetInt32("UserId") ?? 0;
            var users = await _chatService.SearchUsersWithGroupFilterAsync(searchTerm, currentUserId, groupId);
            return Json(users);
        }

        // Trả về HTML cho sidebar right (dùng service dựng view model)
        [HttpGet]
        public async Task<IActionResult> GetGroupSidebar(int groupId)
        {
            var groupViewModel = await _chatService.BuildGroupSidebarViewModelAsync(groupId, 12);
            if (groupViewModel == null)
                return NotFound();
            return PartialView("_SidebarRight", groupViewModel);
        }

        // Trả về HTML cho sidebar media (dùng service dựng view model)
        [HttpGet]
        public async Task<IActionResult> GetGroupSidebarMedia(int groupId)
        {
            var groupViewModel = await _chatService.BuildGroupSidebarMediaViewModelAsync(groupId, 100);
            if (groupViewModel == null)
                return NotFound();
            return PartialView("_SidebarMedia", groupViewModel);
        }

        // Message Reaction Endpoints
        [HttpPost]
        public async Task<IActionResult> AddReaction([FromBody] AddReactionRequest request)
        {
            try
            {
                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                if (userId == 0)
                {
                    return Unauthorized(new { message = "Không tìm thấy thông tin người dùng" });
                }

                var reaction = await _chatService.AddReactionAsync(request.MessageId, userId, request.ReactionType);
                
                if (reaction == null)
                {
                    // Reaction đã bị xóa
                    return Json(new { success = true, action = "removed" });
                }
                else
                {
                    // Reaction đã được thêm hoặc cập nhật
                    return Json(new { success = true, action = "added", reaction = reaction });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding reaction: {ex.Message}");
                return StatusCode(500, new { message = "Có lỗi xảy ra khi thêm biểu tượng cảm xúc" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetMessageReactions(long messageId)
        {
            try
            {
                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                if (userId == 0)
                {
                    return Unauthorized(new { message = "Không tìm thấy thông tin người dùng" });
                }

                var reactions = await _chatService.GetMessageReactionsAsync(messageId, userId);
                return Json(new { success = true, reactions = reactions });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting reactions: {ex.Message}");
                return StatusCode(500, new { message = "Có lỗi xảy ra khi lấy biểu tượng cảm xúc" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> RemoveReaction([FromBody] RemoveReactionRequest request)
        {
            try
            {
                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                if (userId == 0)
                {
                    return Unauthorized(new { message = "Không tìm thấy thông tin người dùng" });
                }

                await _chatService.RemoveReactionAsync(request.MessageId, userId);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error removing reaction: {ex.Message}");
                return StatusCode(500, new { message = "Có lỗi xảy ra khi xóa biểu tượng cảm xúc" });
            }
        }
    }
} 