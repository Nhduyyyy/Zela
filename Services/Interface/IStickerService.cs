using Zela.ViewModels;

namespace Zela.Services;

public interface IStickerService
{
    Task<List<MessageViewModel>> GetStickerAsync(int userId, int friendId);
    Task<List<StickerViewModel>> GetAvailableStickersAsync();
    Task<MessageViewModel> SendStickerAsync(int senderId, int recipientId, string stickerUrl);
    
}