using Zela.DbContext;
using Zela.Models;
using Zela.ViewModels;

namespace Zela.Services;

public class ProfileService : IProfileService
{
    private readonly ApplicationDbContext _context;

    public ProfileService(ApplicationDbContext context)
    {
        _context = context;
    }

    // Lấy thông tin profile của user theo userId
    public async Task<ProfileViewModel> GetUserProfileAsync(int userId)
    {
        // Tìm user trong DB
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            // Nếu không tìm thấy user, trả về null
            return null;

        // Map entity User sang ProfileViewModel để trả về cho client
        return new ProfileViewModel
        {
            UserId = user.UserId,
            Email = user.Email,
            FullName = user.FullName,
            AvatarUrl = user.AvatarUrl,
            IsPremium = user.IsPremium,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt
        };
    }

    // Cập nhật thông tin profile của user
    public async Task<bool> UpdateUserProfileAsync(ProfileViewModel model)
    {
        // Tìm user trong DB theo model.UserId
        var user = await _context.Users.FindAsync(model.UserId);
        if (user == null)
            // Nếu không tìm thấy user, trả về false
            return false;

        // Cập nhật các trường thông tin từ model
        user.Email = model.Email;
        user.FullName = model.FullName;
        user.AvatarUrl = model.AvatarUrl;

        // Lưu thay đổi vào DB
        await _context.SaveChangesAsync();
        return true;
    }
}