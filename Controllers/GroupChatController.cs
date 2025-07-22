using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Zela.Services;
using Zela.Models;
using Zela.ViewModels;
using System.IO;
using Zela.Services.Interface;

namespace Zela.Controllers
{
    [Authorize]
    public class GroupChatController : Controller
    {
        private readonly IChatService _chatService;
        private readonly IVoiceToTextService _voiceToTextService;

        public GroupChatController(IChatService chatService, IVoiceToTextService voiceToTextService)
        {
            _chatService = chatService;
            _voiceToTextService = voiceToTextService;
        }

        // Trang danh sách nhóm
        public async Task<IActionResult> Index()
        {
            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            var groups = await _chatService.GetUserGroupsAsync(userId);
            return View(groups);
        }

        // Trả về View chứa lịch sử chat nhóm
        [HttpGet]
        public async Task<IActionResult> GetGroupMessages(int groupId)
        {
            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            var messages = await _chatService.GetGroupMessagesWithUserContextAsync(groupId, userId);
            var group = await _chatService.GetGroupDetailsAsync(groupId);
            
            var viewModel = new GroupMessagesViewModel
            {
                GroupId = groupId,
                GroupName = group?.Name ?? "Unknown Group",
                Messages = messages,
                Group = group
            };
            
            // Kiểm tra nếu request là AJAX thì trả về JSON, ngược lại trả về View
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(messages);
            }
            
            return View("Messages", viewModel);
        }

        // Gửi tin nhắn nhóm với file
        [HttpPost]
        [RequestSizeLimit(50 * 1024 * 1024)] // 50MB limit
        public async Task<IActionResult> SendGroupMessage(int groupId, string content, List<IFormFile> files, long? replyToMessageId = null)
        {
            int senderId = HttpContext.Session.GetInt32("UserId") ?? 0;
            var (success, message, result) = await _chatService.SendGroupMessageWithValidationAsync(senderId, groupId, content, files, replyToMessageId);
            
            if (!success)
            {
                TempData["ErrorMessage"] = message;
            }
            else
            {
                TempData["SuccessMessage"] = "Tin nhắn đã được gửi thành công!";
            }
            
            return RedirectToAction("GetGroupMessages", new { groupId });
        }

        // Tạo nhóm chat mới (hỗ trợ avatar, password, friendIds)
        [HttpPost]
        public async Task<IActionResult> CreateGroup([FromForm] string name, [FromForm] string description, [FromForm] IFormFile avatar, [FromForm] string password, [FromForm] string friendIds)
        {
            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            var (success, message, groupVm) = await _chatService.CreateGroupWithAvatarAndFriendsAsync(userId, name, description, avatar, password, friendIds);
            
            // Nếu là request AJAX/fetch thì trả về JSON
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" || Request.Headers["Accept"].ToString().Contains("application/json"))
            {
                return Json(new { success, message, group = groupVm });
            }
            // Ngược lại, redirect như cũ
            if (!success)
            {
                TempData["ErrorMessage"] = message;
            }
            else
            {
                TempData["SuccessMessage"] = "Tạo nhóm thành công!";
            }
            
            return RedirectToAction("Index");
        }

        // Thêm thành viên vào nhóm
        [HttpPost]
        public async Task<IActionResult> AddMember(int groupId, int userId)
        {
            int currentUserId = HttpContext.Session.GetInt32("UserId") ?? 0;
            var (success, message) = await _chatService.AddMemberWithValidationAsync(groupId, userId, currentUserId);
            
            return Json(new { success, message });
        }

        // Xóa thành viên khỏi nhóm
        [HttpPost]
        public async Task<IActionResult> RemoveMember(int groupId, int userId)
        {
            try
            {
                await _chatService.RemoveMemberFromGroupAsync(groupId, userId);
                TempData["SuccessMessage"] = "Đã xóa thành viên khỏi nhóm!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xóa thành viên";
            }
            
            return RedirectToAction("GetGroupMessages", new { groupId });
        }

        // Lấy chi tiết nhóm
        [HttpGet]
        public async Task<IActionResult> GetGroupDetails(int groupId)
        {
            var group = await _chatService.GetGroupDetailsAsync(groupId);
            if (group == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy nhóm";
                return RedirectToAction("Index");
            }
            
            return View("Details", group);
        }

        // Cập nhật thông tin nhóm
        [HttpPost]
        public async Task<IActionResult> UpdateGroup(int groupId, string name, string description)
        {
            try
            {
                await _chatService.UpdateGroupAsync(groupId, name, description);
                TempData["SuccessMessage"] = "Cập nhật nhóm thành công!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi cập nhật nhóm";
            }
            
            return RedirectToAction("GetGroupDetails", new { groupId });
        }

        // Xóa nhóm
        [HttpPost]
        public async Task<IActionResult> DeleteGroup(int groupId)
        {
            try
            {
                await _chatService.DeleteGroupAsync(groupId);
                TempData["SuccessMessage"] = "Đã xóa nhóm thành công!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xóa nhóm";
            }
            
            return RedirectToAction("Index");
        }

        // Lấy thông tin nhóm cho edit
        [HttpGet]
        public async Task<IActionResult> GetGroupInfoForEdit(int groupId)
        {
            var groupInfo = await _chatService.GetGroupInfoForEditAsync(groupId);
            if (groupInfo == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy nhóm";
                return RedirectToAction("Index");
            }
            
            return View("Edit", groupInfo);
        }

        [HttpPost]
        public async Task<IActionResult> EditGroup(EditGroupViewModel model, IFormFile? avatarFile)
        {
            var (success, message) = await _chatService.EditGroupAsync(model, avatarFile);
            // Cập nhật loại phòng nếu có thay đổi
            await _chatService.UpdateGroupRoomTypeAsync(model.GroupId, model.RoomType);
            if (success)
            {
                TempData["SuccessMessage"] = message;
            }
            else
            {
                TempData["ErrorMessage"] = message;
            }
            return RedirectToAction("Index", new { groupId = model.GroupId });
        }

        // Tìm kiếm người dùng để thêm vào nhóm
        [HttpGet]
        public async Task<IActionResult> SearchUsers(string searchTerm, int? groupId = null)
        {
            int currentUserId = HttpContext.Session.GetInt32("UserId") ?? 0;
            var users = await _chatService.SearchUsersWithGroupFilterAsync(searchTerm, currentUserId, groupId);
            
            return Json(users);
        }

        // Trả về HTML cho sidebar right
        [HttpGet]
        public async Task<IActionResult> GetGroupSidebar(int groupId)
        {
            var groupViewModel = await _chatService.BuildGroupSidebarViewModelAsync(groupId, 12);
            if (groupViewModel == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy nhóm";
                return RedirectToAction("Index");
            }
            return PartialView("_SidebarRight", groupViewModel);
        }

        // Trả về HTML cho sidebar media
        [HttpGet]
        public async Task<IActionResult> GetGroupSidebarMedia(int groupId)
        {
            var groupViewModel = await _chatService.BuildGroupSidebarMediaViewModelAsync(groupId, 100);
            if (groupViewModel == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy nhóm";
                return RedirectToAction("Index");
            }
            return PartialView("_SidebarMedia", groupViewModel);
        }

        // Message Reaction Actions
        [HttpPost]
        public async Task<IActionResult> AddReaction(int messageId, string reactionType, int groupId)
        {
            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            var (success, message, result) = await _chatService.AddReactionWithValidationAsync(messageId, userId, reactionType);
            
            if (!success)
            {
                TempData["ErrorMessage"] = message;
            }
            else
            {
                TempData["SuccessMessage"] = "Đã thêm biểu tượng cảm xúc!";
            }
            
            return RedirectToAction("GetGroupMessages", new { groupId });
        }

        [HttpGet]
        public async Task<IActionResult> GetMessageReactions(long messageId, int groupId)
        {
            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            var (success, message, result) = await _chatService.GetMessageReactionsWithValidationAsync(messageId, userId);
            
            if (!success)
            {
                TempData["ErrorMessage"] = message;
                return RedirectToAction("GetGroupMessages", new { groupId });
            }
            
            // Chuyển đến trang hiển thị reactions
            return View("MessageReactions", new { MessageId = messageId, GroupId = groupId, Reactions = result });
        }

        [HttpPost]
        public async Task<IActionResult> RemoveReaction(int messageId, int groupId)
        {
            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            var (success, message) = await _chatService.RemoveReactionWithValidationAsync(messageId, userId);
            
            if (!success)
            {
                TempData["ErrorMessage"] = message;
            }
            else
            {
                TempData["SuccessMessage"] = "Đã xóa biểu tượng cảm xúc!";
            }
            
            return RedirectToAction("GetGroupMessages", new { groupId });
        }
        
        // Tham gia nhóm bằng link
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Join(int groupId)
        {
            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            var group = await _chatService.GetGroupDetailsAsync(groupId);
            if (group == null)
            {
                TempData["ErrorMessage"] = "Nhóm không tồn tại.";
                return RedirectToAction("Index", new { groupId });
            }
            if (group.RoomType == Zela.Enum.RoomType.Private)
            {
                TempData["ErrorMessage"] = "Phòng này là riêng tư, không thể tham gia bằng link.";
                return RedirectToAction("Index", new { groupId });
            }
            var (success, message) = await _chatService.JoinGroupAsync(groupId, userId);
            
            if (!success && message.Contains("đăng nhập"))
            {
                TempData["ErrorMessage"] = message;
                return RedirectToAction("Login", "Account");
            }
            
            if (success)
            {
                TempData["SuccessMessage"] = message;
            }
            else
            {
                TempData["InfoMessage"] = message;
            }
            
            return RedirectToAction("Index", new { groupId });
        }

        [HttpPost]
        public async Task<IActionResult> VoiceToText(IFormFile audio, string language)
        {
            if (audio == null || audio.Length == 0)
                return BadRequest(new { text = "" });
            using var stream = audio.OpenReadStream();
            var text = await _voiceToTextService.ConvertVoiceToTextAsync(stream, language);
            return Json(new { text });
        }

        [HttpPost]
        public async Task<IActionResult> KickMember(int groupId, int userId)
        {
            int currentUserId = HttpContext.Session.GetInt32("UserId") ?? 0;
            var group = await _chatService.GetGroupDetailsAsync(groupId);
            var currentMember = group?.Members?.FirstOrDefault(m => m.UserId == currentUserId);
            if (group == null || currentMember == null || (group.CreatorId != currentUserId && !currentMember.IsModerator))
                return Json(new { success = false, message = "Bạn không có quyền thực hiện thao tác này." });
            var result = await _chatService.KickMemberAsync(groupId, userId);
            if (result)
                return Json(new { success = true, message = "Đã kick thành viên khỏi nhóm." });
            return Json(new { success = false, message = "Không thể kick thành viên." });
        }

        [HttpPost]
        public async Task<IActionResult> BanMember(int groupId, int userId, int banHours)
        {
            int currentUserId = HttpContext.Session.GetInt32("UserId") ?? 0;
            var group = await _chatService.GetGroupDetailsAsync(groupId);
            var currentMember = group?.Members?.FirstOrDefault(m => m.UserId == currentUserId);
            if (group == null || currentMember == null || (group.CreatorId != currentUserId && !currentMember.IsModerator))
                return Json(new { success = false, message = "Bạn không có quyền thực hiện thao tác này." });
            var banUntil = DateTime.Now.AddHours(banHours);
            var result = await _chatService.BanMemberAsync(groupId, userId, banUntil);
            if (result)
                return Json(new { success = true, message = $"Đã ban thành viên trong {banHours} giờ." });
            return Json(new { success = false, message = "Không thể ban thành viên." });
        }

        [HttpPost]
        public async Task<IActionResult> UnbanMember(int groupId, int userId)
        {
            int currentUserId = HttpContext.Session.GetInt32("UserId") ?? 0;
            var group = await _chatService.GetGroupDetailsAsync(groupId);
            var currentMember = group?.Members?.FirstOrDefault(m => m.UserId == currentUserId);
            if (group == null || currentMember == null || (group.CreatorId != currentUserId && !currentMember.IsModerator))
                return Json(new { success = false, message = "Bạn không có quyền thực hiện thao tác này." });
            var result = await _chatService.UnbanMemberAsync(groupId, userId);
            if (result)
                return Json(new { success = true, message = "Đã gỡ ban thành viên." });
            return Json(new { success = false, message = "Không thể gỡ ban thành viên." });
        }
    }
} 