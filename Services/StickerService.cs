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
        var stickerPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "sticker");
        var stickerFiles = new List<StickerViewModel>();

        if (Directory.Exists(stickerPath))
        {
            var files = Directory.GetFiles(stickerPath, "*.*", SearchOption.AllDirectories)
                .Where(file => file.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                               file.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                               file.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
                               file.EndsWith(".gif", StringComparison.OrdinalIgnoreCase) ||
                               file.EndsWith(".webp", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            for (int i = 0; i < files.Length; i++)
            {
                var relativePath = "/" + files[i]
                    .Replace(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "")
                    .Replace("\\", "/")
                    .TrimStart('/');

                var fileName = Path.GetFileNameWithoutExtension(files[i]);

                // Lấy folder sau "sticker"
                var parts = relativePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
                var stickerType = (parts.Length >= 2) ? parts[1] : "Unknown";

                stickerFiles.Add(new StickerViewModel
                {
                    StickerId = i + 1,
                    StickerName = fileName,
                    StickerUrl = relativePath,
                    StickerType = stickerType
                });
            }
        }

        return await Task.FromResult(stickerFiles);
    }

    public async Task<MessageViewModel> SendStickerAsync(int senderId, int recipientId, string stickerUrl)
    {
        if (string.IsNullOrEmpty(stickerUrl))
            throw new ArgumentException("Sticker URL cannot be empty");

        // VD: /sticker/stickerType_1/sticker1.gif
        var parts = stickerUrl.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var stickerIndex = Array.IndexOf(parts, "sticker");

        string stickerType = "Unknown";
        if (stickerIndex != -1 && parts.Length > stickerIndex + 1)
        {
            stickerType = parts[stickerIndex + 1]; // lấy folder sau "sticker"
        }

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

        var sticker = new Sticker
        {
            MessageId = message.MessageId,
            StickerUrl = stickerUrl,
            StickerType = stickerType,
            SentAt = DateTime.Now
        };
        _dbContext.Stickers.Add(sticker);
        await _dbContext.SaveChangesAsync();

        var sender = await _dbContext.Users.FindAsync(senderId);

        return new MessageViewModel
        {
            MessageId = message.MessageId,
            SenderId = senderId,
            RecipientId = recipientId,
            SenderName = sender?.FullName ?? "",
            AvatarUrl = sender?.AvatarUrl ?? "",
            Content = "",
            SentAt = message.SentAt,
            IsMine = true,
            IsEdited = false,
            StickerUrl = stickerUrl,
            StickerType = stickerType
        };
    }
}