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
    Task<ChatGroup> CreateGroupAsync(int creatorId, string name, string description, string avatarUrl, string password, List<int> friendIds);
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

    // New methods for moving controller logic to service
    Task<(bool Success, string Message, GroupViewModel Group)> CreateGroupWithAvatarAndFriendsAsync(
        int creatorId, string name, string description, IFormFile avatar, string password, string friendIdsJson);

    Task<(bool Success, string Message)> AddMemberWithValidationAsync(int groupId, int userIdToAdd, int currentUserId);

    Task<List<UserViewModel>> SearchUsersWithGroupFilterAsync(string searchTerm, int currentUserId, int? groupId);

    Task<GroupViewModel> BuildGroupSidebarViewModelAsync(int groupId, int mediaLimit);
    Task<GroupViewModel> BuildGroupSidebarMediaViewModelAsync(int groupId, int mediaLimit);
    
    Task<List<MessageViewModel>> SearchMessagesAsync(int userId, int friendId, string keyword);
    Task<List<long>> MarkAsSeenAsync(long messageId, int userId);
    Task<List<long>> MarkAsDeliveredAsync(long messageId, int userId);
    Task<string> UploadGroupAvatarAsync(IFormFile avatarFile);
    Task UpdateGroupFullAsync(int groupId, string name, string? description, string? avatarUrl, string? password);
    
    // Group edit methods
    Task<EditGroupViewModel> GetGroupInfoForEditAsync(int groupId);
    Task<(bool Success, string Message)> EditGroupAsync(EditGroupViewModel model, IFormFile? avatarFile);
    
    // Group join methods
    Task<(bool Success, string Message)> JoinGroupAsync(int groupId, int userId);
    
    // Message reaction methods with validation
    Task<(bool Success, string Message, object Result)> AddReactionWithValidationAsync(long messageId, int userId, string reactionType);
    Task<(bool Success, string Message, object Result)> GetMessageReactionsWithValidationAsync(long messageId, int userId);
    Task<(bool Success, string Message)> RemoveReactionWithValidationAsync(long messageId, int userId);
    
    // Group message methods with validation
    Task<(bool Success, string Message, GroupMessageViewModel Result)> SendGroupMessageWithValidationAsync(int senderId, int groupId, string content, List<IFormFile> files, long? replyToMessageId);
    
    // Group messages with user context
    Task<List<GroupMessageViewModel>> GetGroupMessagesWithUserContextAsync(int groupId, int userId);
}