using Microsoft.AspNetCore.Mvc;
using Zela.Services;
using Zela.ViewModels;
using Microsoft.Extensions.Logging;

namespace Zela.Controllers;

public class ProfileController : Controller
{
    private readonly IProfileService _profileService;
    private readonly ILogger<ProfileController> _logger;
    private readonly IFileUploadService _fileUploadService;

    public ProfileController(IProfileService profileService, ILogger<ProfileController> logger, IFileUploadService fileUploadService)
    {
        _profileService = profileService;
        _logger = logger;
        _fileUploadService = fileUploadService;
    }

    public async Task<IActionResult> Index()
    {
        // Lấy userId từ session
        var userId = HttpContext.Session.GetInt32("UserId");
        _logger.LogInformation("Profile Index called. UserId from session: {UserId}", userId);
        
        // Nếu chưa đăng nhập thì chuyển hướng về trang đăng nhập
        if (userId == null)
        {
            _logger.LogWarning("No UserId found in session");
            return RedirectToAction("Login", "Account");
        }

        // Lấy thông tin profile từ service
        var profile = await _profileService.GetUserProfileAsync(userId.Value);
        if (profile == null)
        {
            _logger.LogWarning("No profile found for UserId: {UserId}", userId);
            return NotFound();
        }

        _logger.LogInformation("Profile found for UserId: {UserId}", userId);
        // Trả về view hiển thị thông tin profile
        return View(profile);
    }

    [HttpPost]
    public async Task<IActionResult> UpdateProfile(ProfileViewModel model, IFormFile AvatarFile)
    {
        _logger.LogInformation("UpdateProfile called for UserId: {UserId}", model.UserId);
        Console.WriteLine($"[DEBUG] UpdateProfile called for UserId: {model.UserId}");
        
        // Kiểm tra dữ liệu model hợp lệ
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Invalid model state for UserId: {UserId}", model.UserId);
            Console.WriteLine($"[DEBUG] Invalid model state for UserId: {model.UserId}");
            return Json(new { success = false, message = "Dữ liệu không hợp lệ" });
        }

        // Kiểm tra fullname không được null, rỗng hoặc chỉ chứa khoảng trắng
        if (string.IsNullOrWhiteSpace(model.FullName))
        {
            _logger.LogWarning("FullName is empty or whitespace for UserId: {UserId}", model.UserId);
            return Json(new { success = false, message = "Họ tên không được để trống hoặc chỉ chứa khoảng trắng" });
        }

        // Kiểm tra userId trong session và model phải trùng khớp
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null || userId != model.UserId)
        {
            _logger.LogWarning("Unauthorized update attempt. Session UserId: {SessionUserId}, Model UserId: {ModelUserId}", userId, model.UserId);
            Console.WriteLine($"[DEBUG] Unauthorized update attempt. Session UserId: {userId}, Model UserId: {model.UserId}");
            return Json(new { success = false, message = "Không có quyền thực hiện" });
        }

        // Xử lý upload avatar nếu có file gửi lên
        if (AvatarFile != null && AvatarFile.Length > 0)
        {
            Console.WriteLine($"[DEBUG] Avatar file received: {AvatarFile.FileName}, size: {AvatarFile.Length}");
            // Kiểm tra định dạng file hợp lệ
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
            var ext = System.IO.Path.GetExtension(AvatarFile.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(ext))
            {
                Console.WriteLine($"[DEBUG] Invalid file extension: {ext}");
                return Json(new { success = false, message = "Chỉ cho phép ảnh JPG, JPEG, PNG" });
            }
            // Kiểm tra dung lượng file tối đa 2MB
            if (AvatarFile.Length > 2 * 1024 * 1024)
            {
                Console.WriteLine($"[DEBUG] File size too large: {AvatarFile.Length}");
                return Json(new { success = false, message = "Kích thước ảnh tối đa 2MB" });
            }
            // Lấy thông tin profile hiện tại để xóa avatar cũ nếu có
            var profile = await _profileService.GetUserProfileAsync(userId.Value);
            string oldAvatarUrl = profile?.AvatarUrl;
            try
            {
                Console.WriteLine($"[DEBUG] Uploading new avatar to Cloudinary...");
                // Upload avatar mới lên Cloudinary
                string newAvatarUrl = await _fileUploadService.UploadAsync(AvatarFile, "avatars");
                Console.WriteLine($"[DEBUG] New avatar uploaded: {newAvatarUrl}");
                model.AvatarUrl = newAvatarUrl;
                // Xóa avatar cũ nếu tồn tại và nằm trên Cloudinary
                if (!string.IsNullOrEmpty(oldAvatarUrl) && oldAvatarUrl.Contains("res.cloudinary.com"))
                {
                    try {
                        Console.WriteLine($"[DEBUG] Deleting old avatar: {oldAvatarUrl}");
                        await _fileUploadService.DeleteAsync(oldAvatarUrl);
                        Console.WriteLine($"[DEBUG] Old avatar deleted");
                    } catch (Exception ex) {
                        Console.WriteLine($"[DEBUG] Failed to delete old avatar: {ex.Message}");
                    }
                }
                // Cập nhật avatar mới vào session để giao diện cập nhật ngay
                HttpContext.Session.SetString("AvatarUrl", newAvatarUrl);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DEBUG] Avatar upload failed: {ex.Message}");
                return Json(new { success = false, message = "Lỗi khi upload ảnh đại diện" });
            }
        }

        // Gọi service để cập nhật thông tin profile
        var result = await _profileService.UpdateUserProfileAsync(model);
        if (!result)
        {
            _logger.LogWarning("Failed to update profile for UserId: {UserId}", model.UserId);
            return Json(new { success = false, message = "Cập nhật thất bại" });
        }

        // Cập nhật fullname vào session để hiển thị nhất quán
        HttpContext.Session.SetString("FullName", model.FullName);
        _logger.LogInformation("Successfully updated profile for UserId: {UserId}", model.UserId);
        // Trả về kết quả thành công và url avatar mới (nếu có)
        return Json(new { success = true, message = "Cập nhật thành công", avatarUrl = model.AvatarUrl });
    }

    [HttpGet]
    public async Task<IActionResult> GetProfileData()
    {
        // Lấy userId từ session
        var userId = HttpContext.Session.GetInt32("UserId");
        _logger.LogInformation("GetProfileData called. UserId from session: {UserId}", userId);
        
        // Nếu chưa đăng nhập thì trả về lỗi
        if (userId == null)
        {
            _logger.LogWarning("No UserId found in session");
            return Json(new { success = false, message = "Không tìm thấy thông tin người dùng" });
        }

        // Lấy thông tin profile từ service
        var profile = await _profileService.GetUserProfileAsync(userId.Value);
        if (profile == null)
        {
            _logger.LogWarning("No profile found for UserId: {UserId}", userId);
            return Json(new { success = false, message = "Không tìm thấy thông tin profile" });
        }

        _logger.LogInformation("Profile data returned for UserId: {UserId}", userId);
        // Trả về dữ liệu profile dạng JSON
        return Json(new { success = true, data = profile });
    }
} 