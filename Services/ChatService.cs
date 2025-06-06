

using Microsoft.EntityFrameworkCore;
using Zela.DbContext;
using Zela.Models;
using Zela.ViewModels;

namespace Zela.Services;

public class ChatService : IChatService
{
    private readonly ApplicationDbContext _dbContext;

    public ChatService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    
    public async Task<MessageViewModel> SendMessageAsync(int senderId, int recipientId, string content)
    {
        // 1. Tạo đối tượng Message
        var msg = new Message
        {
            SenderId = senderId,
            RecipientId = recipientId,
            Content = content,
            SentAt = DateTime.Now,
            IsEdited = false
        };

        _dbContext.Messages.Add(msg);
        await _dbContext.SaveChangesAsync();

        // 2. Lấy info để trả về (avatar, tên người gửi v.v.)
        var sender = await _dbContext.Users.FindAsync(senderId);

        // 3. Tạo viewmodel trả về cho client (phù hợp JS render)
        return new MessageViewModel
        {
            SenderId = senderId,
            RecipientId = recipientId,
            Content = content,
            SentAt = msg.SentAt,
            AvatarUrl = sender.AvatarUrl,
            IsMine = true // Vì đây là người gửi
        };
    }

    // Lấy bạn bè đã kết bạn (status = accepted)
    public async Task<List<FriendViewModel>> GetFriendListAsync(int userId)
    {
        // Giả sử bạn có bảng Friendships (UserId1, UserId2, StatusId)
        var friendIds = await _dbContext.Friendships
            .Where(f => (f.UserId1 == userId || f.UserId2 == userId) && f.StatusId == 2) // 2 = Accepted
            .Select(f => f.UserId1 == userId ? f.UserId2 : f.UserId1)
            .ToListAsync();

        var friends = await _dbContext.Users
            .Where(u => friendIds.Contains(u.UserId))
            .Select(u => new FriendViewModel
            {
                UserId = u.UserId,
                FullName = u.FullName,
                AvatarUrl = u.AvatarUrl,
                IsOnline = u.LastLoginAt > DateTime.Now.AddMinutes(-3),
                LastMessage = _dbContext.Messages
                    .Where(m =>
                        (m.SenderId == userId && m.RecipientId == u.UserId) ||
                        (m.SenderId == u.UserId && m.RecipientId == userId))
                    .OrderByDescending(m => m.SentAt)
                    .Select(m => m.Content)
                    .FirstOrDefault(),
                LastTime = _dbContext.Messages
                    .Where(m =>
                        (m.SenderId == userId && m.RecipientId == u.UserId) ||
                        (m.SenderId == u.UserId && m.RecipientId == userId))
                    .OrderByDescending(m => m.SentAt)
                    .Select(m => m.SentAt.ToString("HH:mm"))
                    .FirstOrDefault()
            })
            .ToListAsync();

        return friends;
    }

    // Lấy lịch sử chat 1-1
    public async Task<List<MessageViewModel>> GetMessagesAsync(int userId, int friendId)
    {
        return await _dbContext.Messages
            .Include(m => m.Sender)
            .Include(m => m.Recipient)
            .Where(m =>
                (m.SenderId == userId && m.RecipientId == friendId) ||
                (m.SenderId == friendId && m.RecipientId == userId))
            .OrderBy(m => m.SentAt)
            .Select(m => new MessageViewModel
            {
                MessageId = m.MessageId,
                SenderId = m.SenderId,
                RecipientId = m.RecipientId ?? 0,
                SenderName = m.Sender.FullName,
                AvatarUrl = m.Sender.AvatarUrl,
                Content = m.Content,
                SentAt = m.SentAt,
                IsMine = m.SenderId == userId,
                IsEdited = m.IsEdited
            })
            .ToListAsync();
    }

    // Lưu message mới
    public async Task<MessageViewModel> SaveMessageAsync(int senderId, int recipientId, string content)
    {
        var sender = await _dbContext.Users.FindAsync(senderId);

        var message = new Message
        {
            SenderId = senderId,
            RecipientId = recipientId,
            SentAt = DateTime.Now,
            Content = content,
            IsEdited = false
        };
        _dbContext.Messages.Add(message);
        await _dbContext.SaveChangesAsync();

        return new MessageViewModel
        {
            MessageId = message.MessageId,
            SenderId = senderId,
            RecipientId = recipientId,
            SenderName = sender.FullName,
            AvatarUrl = sender.AvatarUrl,
            Content = content,
            SentAt = message.SentAt,
            IsMine = true,
            IsEdited = false
        };
    }
}
