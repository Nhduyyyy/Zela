using Zela.ViewModels;

namespace Zela.Services.Interface;

public interface IMeetingRoomMessageService
{
    Task<List<MeetingRoomMessageViewModel>> GetRoomMessagesAsync(int roomId, Guid sessionId, int currentUserId);
    Task<MeetingRoomMessageViewModel?> GetMessageByIdAsync(long messageId);
    Task<MeetingRoomMessageViewModel> SendMessageAsync(MeetingSendMessageViewModel model, int senderId);
    Task<MeetingRoomMessageViewModel?> EditMessageAsync(MeetingEditMessageViewModel model, int userId);
    Task<bool> DeleteMessageAsync(long messageId, int userId);
    Task<List<UserViewModel>> GetRoomParticipantsAsync(int roomId);
    Task<bool> IsUserInRoomAsync(int userId, int roomId);
    Task<bool> CanUserSendMessageAsync(int userId, int roomId);
} 