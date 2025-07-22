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

        public async Task CreateNotificationAsync(int senderId, int receiverId, string content, MessageType messageType)
        {
            if (senderId == receiverId) return;
            var notification = new Notification
            {
                SenderUserId = senderId,
                ReceiverUserId = receiverId,
                Content = content,
                MessageType = messageType,
                Timestamp = DateTime.UtcNow,
                IsRead = false
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }

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

        public async Task MarkAsReadAsync(int notificationId)
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification != null)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
            }
        }

        public async Task MarkAllAsReadAsync(int userId)
        {
            var notifications = _context.Notifications.Where(n => n.ReceiverUserId == userId && !n.IsRead);
            await notifications.ForEachAsync(n => n.IsRead = true);
            await _context.SaveChangesAsync();
        }
    }
} 