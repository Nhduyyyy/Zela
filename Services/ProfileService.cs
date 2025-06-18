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

    public async Task<ProfileViewModel> GetUserProfileAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return null;

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

    public async Task<bool> UpdateUserProfileAsync(ProfileViewModel model)
    {
        var user = await _context.Users.FindAsync(model.UserId);
        if (user == null)
            return false;

        user.Email = model.Email;
        user.FullName = model.FullName;
        user.AvatarUrl = model.AvatarUrl;

        await _context.SaveChangesAsync();
        return true;
    }
} 