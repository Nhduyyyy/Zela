using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Zela.Services;
using Zela.Models;
using Zela.ViewModels;

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

        // Trả về partial chứa lịch sử chat nhóm
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
            
            return PartialView("_GroupMessagesPartial", messages);
        }

        // Gửi tin nhắn nhóm với file
        [HttpPost]
        public async Task<IActionResult> SendGroupMessage(int groupId, string content, List<IFormFile> files)
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

                var message = await _chatService.SendGroupMessageAsync(senderId, groupId, content, files);
                return Ok(new { success = true, message = message });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending group message: {ex.Message}");
                return StatusCode(500, new { message = "Có lỗi xảy ra khi gửi tin nhắn. Vui lòng thử lại." });
            }
        }

        // Tạo nhóm chat mới
        [HttpPost]
        public async Task<IActionResult> CreateGroup(string name, string description)
        {
            try
            {
                if (string.IsNullOrEmpty(name?.Trim()))
                {
                    return BadRequest(new { message = "Tên nhóm không được để trống" });
                }

                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                if (userId == 0)
                {
                    return Unauthorized(new { message = "Không tìm thấy thông tin người dùng" });
                }

                var group = await _chatService.CreateGroupAsync(userId, name.Trim(), description?.Trim());
                return Json(new { success = true, group = group });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                // Log the error here
                Console.WriteLine($"Error creating group: {ex.Message}");
                return StatusCode(500, new { message = "Có lỗi xảy ra khi tạo nhóm. Vui lòng thử lại." });
            }
        }

        // Thêm thành viên vào nhóm
        [HttpPost]
        public async Task<IActionResult> AddMember(int groupId, int userId)
        {
            try
            {
                int currentUserId = HttpContext.Session.GetInt32("UserId") ?? 0;
                if (currentUserId == 0)
                {
                    return Unauthorized(new { message = "Không tìm thấy thông tin người dùng" });
                }

                // Kiểm tra xem người dùng hiện tại có phải là thành viên của nhóm không
                var group = await _chatService.GetGroupDetailsAsync(groupId);
                if (group == null)
                {
                    return NotFound(new { message = "Không tìm thấy nhóm" });
                }

                var isMember = group.Members.Any(m => m.UserId == currentUserId);
                if (!isMember)
                {
                    return BadRequest(new { message = "Bạn không phải thành viên của nhóm này" });
                }

                // Kiểm tra xem người dùng đã là thành viên chưa
                var existingMember = group.Members.FirstOrDefault(m => m.UserId == userId);
                if (existingMember != null)
                {
                    return BadRequest(new { message = "Người dùng đã là thành viên của nhóm" });
                }

                await _chatService.AddMemberToGroupAsync(groupId, userId);
                return Ok(new { message = "Thêm thành viên thành công" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding member to group: {ex.Message}");
                return StatusCode(500, new { message = "Có lỗi xảy ra khi thêm thành viên. Vui lòng thử lại." });
            }
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

        // Tìm kiếm người dùng để thêm vào nhóm
        [HttpGet]
        public async Task<IActionResult> SearchUsers(string searchTerm, int? groupId = null)
        {
            try
            {
                int currentUserId = HttpContext.Session.GetInt32("UserId") ?? 0;
                if (currentUserId == 0)
                {
                    return Unauthorized(new { message = "Không tìm thấy thông tin người dùng" });
                }

                var users = await _chatService.SearchUsersAsync(searchTerm, currentUserId);
                
                // Nếu có groupId, loại bỏ những người dùng đã là thành viên
                if (groupId.HasValue)
                {
                    var group = await _chatService.GetGroupDetailsAsync(groupId.Value);
                    if (group != null)
                    {
                        var existingMemberIds = group.Members.Select(m => m.UserId).ToHashSet();
                        users = users.Where(u => !existingMemberIds.Contains(u.UserId)).ToList();
                    }
                }
                
                return Json(users);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error searching users: {ex.Message}");
                return StatusCode(500, new { message = "Có lỗi xảy ra khi tìm kiếm người dùng" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetGroupSidebar(int groupId)
        {
            var group = await _chatService.GetGroupDetailsAsync(groupId);
            if (group == null)
            {
                return NotFound();
            }

            var members = group.Members?.Select(m => new UserViewModel
            {
                UserId = m.UserId,
                FullName = m.User?.FullName ?? "Unknown",
                Email = m.User?.Email ?? "",
                AvatarUrl = m.User?.AvatarUrl ?? "/images/default-avatar.jpeg",
                IsOnline = m.User != null && m.User.LastLoginAt > DateTime.Now.AddMinutes(-3),
                LastLoginAt = m.User?.LastLoginAt
            }).ToList() ?? new List<UserViewModel>();

            var groupViewModel = new GroupViewModel()
            {
                GroupId = (int)group.GroupId,
                Name = group.Name,
                Description = group.Description,
                AvatarUrl = group.AvatarUrl ?? "/images/default-group-avatar.png",
                MemberCount = group.Members?.Count ?? 0,
                CreatedAt = group.CreatedAt,
                CreatorId = group.CreatorId,
                CreatorName = group.Creator?.FullName ?? "Unknown",
                Members = members
            };

            return PartialView("_SidebarRight", groupViewModel);
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