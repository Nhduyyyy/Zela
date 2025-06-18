using Zela.Models;
using Microsoft.EntityFrameworkCore;
using Zela.DbContext;
using Zela.ViewModels;

namespace Zela.Services;

public class ChatService : IChatService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IFileUploadService _fileUploadService;

    public ChatService(ApplicationDbContext dbContext, IFileUploadService fileUploadService)
    {
        _dbContext = dbContext;
        _fileUploadService = fileUploadService;
    }

    public async Task<MessageViewModel> SendMessageAsync(int senderId, int recipientId, string content, IFormFile? file = null)
    {
        var msg = new Message
        {
            SenderId = senderId,
            RecipientId = recipientId,
            Content = content ?? "",
            SentAt = DateTime.Now,
            IsEdited = false,
            Media = new List<Media>()
        };

        if (file != null && file.Length > 0)
        {
            Console.WriteLine($"[ChatService] Uploading file: {file.FileName}, type: {file.ContentType}, size: {file.Length}");
            var url = await _fileUploadService.UploadAsync(file, "chat");
            Console.WriteLine($"[ChatService] Uploaded file url: {url}");
            msg.Media.Add(new Media
            {
                MediaType = file.ContentType,
                Url = url
            });
        }

        _dbContext.Messages.Add(msg);
        await _dbContext.SaveChangesAsync();

        var sender = await _dbContext.Users.FindAsync(senderId);
        return new MessageViewModel
        {
            MessageId = msg.MessageId,
            SenderId = senderId,
            RecipientId = recipientId,
            SenderName = sender?.FullName ?? "Unknown",
            AvatarUrl = sender?.AvatarUrl ?? "",
            Content = msg.Content ?? "",
            SentAt = msg.SentAt,
            IsMine = true,
            IsEdited = false,
            Media = msg.Media.Select(md => new MediaViewModel
            {
                Url = md.Url,
                MediaType = md.MediaType
            }).ToList()
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
            .Include(m => m.Media)
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
                IsEdited = m.IsEdited,
                Media = m.Media.Select(md => new MediaViewModel
                {
                    Url = md.Url,
                    MediaType = md.MediaType
                }).ToList()
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
