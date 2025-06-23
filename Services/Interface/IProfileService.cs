using Zela.ViewModels;

namespace Zela.Services;

public interface IProfileService
{
    Task<ProfileViewModel> GetUserProfileAsync(int userId);
    Task<bool> UpdateUserProfileAsync(ProfileViewModel model);
}