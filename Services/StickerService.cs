using Microsoft.EntityFrameworkCore;
using Zela.DbContext;
using Zela.Models;
using Zela.ViewModels;

namespace Zela.Services;

public class StickerService : IStickerService
{
    private readonly ApplicationDbContext _dbContext;

    public StickerService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    // Lấy lịch sử sticker giữa 2 user
    public async Task<List<MessageViewModel>> GetStickerAsync(int userId, int friendId)
    {
        return await _dbContext.Messages
            .Include(m => m.Sender)
            .Include(m => m.Sticker)
            .Where(m => 
                ((m.SenderId == userId && m.RecipientId == friendId) ||
                 (m.SenderId == friendId && m.RecipientId == userId)) &&
                m.Sticker.Any()) // Chỉ lấy những message có sticker
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
                StickerUrl = m.Sticker.FirstOrDefault().StickerUrl,
                StickerType = m.Sticker.FirstOrDefault().StickerType
            })
            .ToListAsync();
    }
    public async Task<List<StickerViewModel>> GetAvailableStickersAsync()
    {
        // Đọc tất cả file sticker từ thư mục /sticker
        var stickerPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "sticker");
        var stickerFiles = new List<StickerViewModel>();
        
        if (Directory.Exists(stickerPath))
        {
            var files = Directory.GetFiles(stickerPath, "*.*", SearchOption.AllDirectories)
                .Where(file => file.ToLower().EndsWith(".png") || 
                               file.ToLower().EndsWith(".jpg") || 
                               file.ToLower().EndsWith(".jpeg") || 
                               file.ToLower().EndsWith(".gif") ||
                               file.ToLower().EndsWith(".webp"))
                .ToArray();

            for (int i = 0; i < files.Length; i++)
            {
                var fileName = Path.GetFileNameWithoutExtension(files[i]);
                var relativePath = "/" + files[i]
                    .Replace(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "")
                    .Replace("\\", "/")
                    .TrimStart('/');
                
                stickerFiles.Add(new StickerViewModel
                {
                    StickerId = i + 1,
                    StickerName = fileName,
                    StickerUrl = relativePath
                });
            }
        }

        return await Task.FromResult(stickerFiles);
    }

    public async Task<MessageViewModel> SendStickerAsync(int senderId, int recipientId, string stickerUrl)
    {
        // Validate input
        if (string.IsNullOrEmpty(stickerUrl))
        {
            throw new ArgumentException("Sticker URL and name cannot be empty");
        }

        // 1. Tạo message với content đặc biệt cho sticker
        var message = new Message
        {
            SenderId = senderId,
            RecipientId = recipientId,
            Content = "Sticker",
            SentAt = DateTime.Now,
            IsEdited = false
        };

        _dbContext.Messages.Add(message);
        await _dbContext.SaveChangesAsync();

        // 2. Tạo sticker
        var sticker = new Sticker
        {
            MessageId = message.MessageId,
            StickerUrl = stickerUrl,
            StickerType = "Sticker",
            SentAt = DateTime.Now
        };

        _dbContext.Stickers.Add(sticker);
        await _dbContext.SaveChangesAsync();

        // 3. Lấy thông tin sender
        var sender = await _dbContext.Users.FindAsync(senderId);
        if (sender == null)
        {
            throw new InvalidOperationException($"Sender with ID {senderId} not found");
        }

        // 4. Trả về MessageViewModel thay vì StickerViewModel để phù hợp với giao diện chat
        return new MessageViewModel
        {
            MessageId = message.MessageId,
            SenderId = senderId,
            RecipientId = recipientId,
            SenderName = sender.FullName,
            AvatarUrl = sender.AvatarUrl,
            Content = "Sticker",
            SentAt = message.SentAt,
            IsMine = true,
            IsEdited = false,
            StickerUrl = stickerUrl,
            StickerType = "Sticker"
        };
    }
}