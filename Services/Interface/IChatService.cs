

using Zela.ViewModels;

namespace Zela.Services;

public interface IChatService
{
    Task<List<FriendViewModel>> GetFriendListAsync(int userId);
    Task<List<MessageViewModel>> GetMessagesAsync(int userId, int friendId);
    Task<MessageViewModel> SendMessageAsync(int senderId, int recipientId, string content);
    Task<MessageViewModel> SaveMessageAsync(int senderId, int recipientId, string content);
}