using Zela.Models;
using Zela.ViewModels;

namespace Zela.Services;

public interface IChatService
{
    Task<List<FriendViewModel>> GetFriendListAsync(int userId);
    Task<List<MessageViewModel>> GetMessagesAsync(int userId, int friendId);
    Task<MessageViewModel> SendMessageAsync(int senderId, int recipientId, string content, List<IFormFile>? files = null);
    Task<MessageViewModel> SaveMessageAsync(int senderId, int recipientId, string content);
    
    Task<User> FindUserByIdAsync(int userId);
    
    // Group chat methods
    Task<GroupMessageViewModel> SendGroupMessageAsync(int senderId, int groupId, string content, List<IFormFile>? files = null, long? replyToMessageId = null);
    Task<ChatGroup> CreateGroupAsync(int creatorId, string name, string description);
    Task AddMemberToGroupAsync(int groupId, int userId);
    Task RemoveMemberFromGroupAsync(int groupId, int userId);
    Task<List<GroupMessageViewModel>> GetGroupMessagesAsync(int groupId);
    Task<List<GroupViewModel>> GetUserGroupsAsync(int userId);
    Task<ChatGroup> GetGroupDetailsAsync(int groupId);
    Task UpdateGroupAsync(int groupId, string name, string description);
    Task DeleteGroupAsync(int groupId);
    Task<List<UserViewModel>> SearchUsersAsync(string searchTerm, int currentUserId);
    Task ToggleModeratorAsync(int groupId, int userId);
    
    // Message reaction methods
    Task<MessageReactionViewModel> AddReactionAsync(long messageId, int userId, string reactionType);
    Task<List<MessageReactionSummaryViewModel>> GetMessageReactionsAsync(long messageId, int currentUserId);
    Task RemoveReactionAsync(long messageId, int userId);
    Task<bool> HasUserReactionAsync(long messageId, int userId, string reactionType);

    Task<GroupMessageViewModel> SendGroupStickerAsync(int senderId, int groupId, string stickerUrl);
    
    // Group media methods
    Task<(List<MediaViewModel> Images, List<MediaViewModel> Videos, List<MediaViewModel> Files)> GetGroupMediaAsync(int groupId, int limit = 20);
}