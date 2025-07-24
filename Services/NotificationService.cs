using Microsoft.EntityFrameworkCore;
using Zela.DbContext;
using Zela.Models;
using Zela.Services.Interface;
using Zela.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Zela.Enum;

namespace Zela.Services
{
    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;

        public NotificationService(ApplicationDbContext context)
        {
            _context = context;
        }

        // Tạo notification mới khi có sự kiện (gửi tin nhắn, v.v.)
        public async Task CreateNotificationAsync(int senderId, int receiverId, string content, MessageType messageType)
        {
            // Không tạo notification nếu tự gửi cho chính mình
            if (senderId == receiverId) return;
            // Tạo entity Notification mới
            var notification = new Notification
            {
                SenderUserId = senderId,
                ReceiverUserId = receiverId,
                Content = content,
                MessageType = messageType,
                Timestamp = DateTime.UtcNow,
                IsRead = false
            };
            // Thêm vào DB và lưu thay đổi
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }

        // Lấy 10 thông báo mới nhất cho user
        public async Task<List<NotificationViewModel>> GetLatestNotificationsAsync(int userId)
        {
            return await _context.Notifications
                .Where(n => n.ReceiverUserId == userId)
                .OrderByDescending(n => n.Timestamp)
                .Take(10)
                .Select(n => new NotificationViewModel
                {
                    NotificationId = n.NotificationId,
                    SenderName = n.Sender.FullName,
                    Content = n.Content,
                    MessageType = n.MessageType,
                    Timestamp = n.Timestamp,
                    IsRead = n.IsRead
                })
                .ToListAsync();
        }

        // Đánh dấu một notification là đã đọc
        public async Task MarkAsReadAsync(int notificationId)
        {
            // Tìm notification theo id
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification != null)
            {
                // Đánh dấu đã đọc và lưu thay đổi
                notification.IsRead = true;
                await _context.SaveChangesAsync();
            }
        }

        // Đánh dấu tất cả notification của user là đã đọc
        public async Task MarkAllAsReadAsync(int userId)
        {
            // Lấy tất cả notification chưa đọc của user
            var notifications = _context.Notifications.Where(n => n.ReceiverUserId == userId && !n.IsRead);
            // Đánh dấu tất cả là đã đọc
            await notifications.ForEachAsync(n => n.IsRead = true);
            await _context.SaveChangesAsync();
        }
    }
} 