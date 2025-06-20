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
            }
            
            return PartialView("_GroupMessagesPartial", messages);
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
            await _chatService.AddMemberToGroupAsync(groupId, userId);
            return Ok();
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
        public async Task<IActionResult> SearchUsers(string searchTerm)
        {
            int currentUserId = HttpContext.Session.GetInt32("UserId") ?? 0;
            var users = await _chatService.SearchUsersAsync(searchTerm, currentUserId);
            return Json(users);
        }

        [HttpGet]
        public async Task<IActionResult> GetGroupSidebar(int groupId)
        {
            var group = await _chatService.GetGroupDetailsAsync(groupId);
            if (group == null)
            {
                return NotFound();
            }

            var groupViewModel = new GroupViewModel()
            {
                GroupId = (int)group.GroupId,
                Name = group.Name,
                Description = group.Description,
                AvatarUrl = group.AvatarUrl ?? "/images/default-group-avatar.png",
                MemberCount = group.Members?.Count ?? 0,
                CreatedAt = group.CreatedAt,
                CreatorName = group.Creator?.FullName ?? "Unknown"
            };

            return PartialView("_SidebarRight", groupViewModel);
        }
    }
} 