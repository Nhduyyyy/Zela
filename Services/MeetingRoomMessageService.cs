using Microsoft.EntityFrameworkCore;
using Zela.DbContext;
using Zela.Models;
using Zela.Services.Interface;
using Zela.ViewModels;

namespace Zela.Services;

public class MeetingRoomMessageService : IMeetingRoomMessageService
{
    private readonly ApplicationDbContext _context;

    public MeetingRoomMessageService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<MeetingRoomMessageViewModel>> GetRoomMessagesAsync(int roomId, Guid sessionId, int currentUserId)
    {
        var messages = await _context.RoomMessages
            .Include(rm => rm.Sender)
            .Include(rm => rm.Recipient)
            .Where(rm => rm.RoomId == roomId && rm.SessionId == sessionId && !rm.IsDeleted)
            .OrderBy(rm => rm.SentAt)
            .ToListAsync();

        return messages.Select(m => new MeetingRoomMessageViewModel
        {
            MessageId = m.MessageId,
            Content = m.Content,
            SenderId = m.SenderId,
            SenderName = m.Sender?.FullName ?? "Unknown",
            SenderAvatar = m.Sender?.AvatarUrl,
            RoomId = m.RoomId,
            SessionId = m.SessionId,
            MessageType = m.MessageType,
            IsPrivate = m.IsPrivate,
            RecipientId = m.RecipientId,
            RecipientName = m.Recipient?.FullName,
            SentAt = m.SentAt,
            IsEdited = m.IsEdited,
            EditedAt = m.EditedAt,
            EditReason = m.EditReason,
            IsDeleted = m.IsDeleted,
            DeletedAt = m.DeletedAt,
            DeleteReason = m.DeleteReason,
            IsOwnMessage = m.SenderId == currentUserId
        }).ToList();
    }

    public async Task<MeetingRoomMessageViewModel?> GetMessageByIdAsync(long messageId)
    {
        var message = await _context.RoomMessages
            .Include(rm => rm.Sender)
            .Include(rm => rm.Recipient)
            .FirstOrDefaultAsync(rm => rm.MessageId == messageId);

        if (message == null) return null;

        return new MeetingRoomMessageViewModel
        {
            MessageId = message.MessageId,
            Content = message.Content,
            SenderId = message.SenderId,
            SenderName = message.Sender?.FullName ?? "Unknown",
            SenderAvatar = message.Sender?.AvatarUrl,
            RoomId = message.RoomId,
            SessionId = message.SessionId,
            MessageType = message.MessageType,
            IsPrivate = message.IsPrivate,
            RecipientId = message.RecipientId,
            RecipientName = message.Recipient?.FullName,
            SentAt = message.SentAt,
            IsEdited = message.IsEdited,
            EditedAt = message.EditedAt,
            EditReason = message.EditReason,
            IsDeleted = message.IsDeleted,
            DeletedAt = message.DeletedAt,
            DeleteReason = message.DeleteReason
        };
    }

    public async Task<MeetingRoomMessageViewModel> SendMessageAsync(MeetingSendMessageViewModel model, int senderId)
    {
        // Kiểm tra quyền gửi tin nhắn
        if (!await CanUserSendMessageAsync(senderId, model.RoomId))
        {
            throw new InvalidOperationException("Bạn không có quyền gửi tin nhắn trong phòng này");
        }

        var message = new RoomMessage
        {
            RoomId = model.RoomId,
            SenderId = senderId,
            SessionId = model.SessionId,
            Content = model.Content,
            MessageType = model.MessageType,
            IsPrivate = model.IsPrivate,
            RecipientId = model.RecipientId,
            SentAt = DateTime.UtcNow,
            IsEdited = false,
            IsDeleted = false
        };

        _context.RoomMessages.Add(message);
        await _context.SaveChangesAsync();

        // Cập nhật số lượng tin nhắn trong session
        var session = await _context.CallSessions.FindAsync(model.SessionId);
        if (session != null)
        {
            session.MessageCount++;
            await _context.SaveChangesAsync();
        }

        return await GetMessageByIdAsync(message.MessageId) ?? new MeetingRoomMessageViewModel();
    }

    public async Task<MeetingRoomMessageViewModel?> EditMessageAsync(MeetingEditMessageViewModel model, int userId)
    {
        var message = await _context.RoomMessages.FindAsync(model.MessageId);
        if (message == null || message.SenderId != userId || message.IsDeleted)
        {
            return null;
        }

        message.Content = model.Content;
        message.IsEdited = true;
        message.EditedAt = DateTime.UtcNow;
        message.EditReason = model.EditReason;

        await _context.SaveChangesAsync();

        return await GetMessageByIdAsync(message.MessageId);
    }

    public async Task<bool> DeleteMessageAsync(long messageId, int userId)
    {
        var message = await _context.RoomMessages.FindAsync(messageId);
        if (message == null || message.SenderId != userId || message.IsDeleted)
        {
            return false;
        }

        message.IsDeleted = true;
        message.DeletedAt = DateTime.UtcNow;
        message.DeleteReason = "Deleted by user";

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<UserViewModel>> GetRoomParticipantsAsync(int roomId)
    {
        var participants = await _context.RoomParticipants
            .Include(rp => rp.User)
            .Where(rp => rp.RoomId == roomId && rp.LeftAt == null)
            .ToListAsync();

        return participants.Select(p => new UserViewModel
        {
            UserId = p.UserId,
            FullName = p.User?.FullName ?? "Unknown",
            AvatarUrl = p.User?.AvatarUrl,
            IsModerator = p.IsModerator,
            IsHost = p.IsHost
        }).ToList();
    }

    public async Task<bool> IsUserInRoomAsync(int userId, int roomId)
    {
        return await _context.RoomParticipants
            .AnyAsync(rp => rp.UserId == userId && rp.RoomId == roomId && rp.LeftAt == null);
    }

    public async Task<bool> CanUserSendMessageAsync(int userId, int roomId)
    {
        // Kiểm tra phòng có tồn tại không
        var room = await _context.VideoRooms.FindAsync(roomId);
        if (room == null)
        {
            return false;
        }

        // Nếu phòng chưa có AllowChat, set default là true
        if (!room.AllowChat)
        {
            room.AllowChat = true;
            await _context.SaveChangesAsync();
        }

        // Kiểm tra user có trong phòng không (linh hoạt hơn)
        var participant = await _context.RoomParticipants
            .FirstOrDefaultAsync(rp => rp.UserId == userId && rp.RoomId == roomId);
        
        // Nếu user chưa có trong RoomParticipants, cho phép gửi tin nhắn
        // (sẽ được thêm vào khi join chat)
        if (participant == null)
        {
            return true;
        }

        // Kiểm tra user có bị mute hoặc đã rời phòng không
        return participant.LeftAt == null && !participant.IsMutedByHost;
    }
} 