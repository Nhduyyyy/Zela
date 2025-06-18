using Microsoft.AspNetCore.Mvc;
using Zela.Services;
using Zela.ViewModels;
using Microsoft.Extensions.Logging;

namespace Zela.Controllers;

public class ProfileController : Controller
{
    private readonly IProfileService _profileService;
    private readonly ILogger<ProfileController> _logger;

    public ProfileController(IProfileService profileService, ILogger<ProfileController> logger)
    {
        _profileService = profileService;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        _logger.LogInformation("Profile Index called. UserId from session: {UserId}", userId);
        
        if (userId == null)
        {
            _logger.LogWarning("No UserId found in session");
            return RedirectToAction("Login", "Account");
        }

        var profile = await _profileService.GetUserProfileAsync(userId.Value);
        if (profile == null)
        {
            _logger.LogWarning("No profile found for UserId: {UserId}", userId);
            return NotFound();
        }

        _logger.LogInformation("Profile found for UserId: {UserId}", userId);
        return View(profile);
    }

    [HttpPost]
    public async Task<IActionResult> UpdateProfile(ProfileViewModel model)
    {
        _logger.LogInformation("UpdateProfile called for UserId: {UserId}", model.UserId);
        
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Invalid model state for UserId: {UserId}", model.UserId);
            return Json(new { success = false, message = "Dữ liệu không hợp lệ" });
        }

        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null || userId != model.UserId)
        {
            _logger.LogWarning("Unauthorized update attempt. Session UserId: {SessionUserId}, Model UserId: {ModelUserId}", 
                userId, model.UserId);
            return Json(new { success = false, message = "Không có quyền thực hiện" });
        }

        var result = await _profileService.UpdateUserProfileAsync(model);
        if (!result)
        {
            _logger.LogWarning("Failed to update profile for UserId: {UserId}", model.UserId);
            return Json(new { success = false, message = "Cập nhật thất bại" });
        }

        _logger.LogInformation("Successfully updated profile for UserId: {UserId}", model.UserId);
        return Json(new { success = true, message = "Cập nhật thành công" });
    }

    [HttpGet]
    public async Task<IActionResult> GetProfileData()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        _logger.LogInformation("GetProfileData called. UserId from session: {UserId}", userId);
        
        if (userId == null)
        {
            _logger.LogWarning("No UserId found in session");
            return Json(new { success = false, message = "Không tìm thấy thông tin người dùng" });
        }

        var profile = await _profileService.GetUserProfileAsync(userId.Value);
        if (profile == null)
        {
            _logger.LogWarning("No profile found for UserId: {UserId}", userId);
            return Json(new { success = false, message = "Không tìm thấy thông tin profile" });
        }

        _logger.LogInformation("Profile data returned for UserId: {UserId}", userId);
        return Json(new { success = true, data = profile });
    }
} 