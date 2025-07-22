using Zela.Models;
using Zela.ViewModels;
using System.Collections.Generic;
using System.Threading.Tasks;
using Zela.Enum;

namespace Zela.Services.Interface
{
    public interface INotificationService
    {
        Task CreateNotificationAsync(int senderId, int receiverId, string content, MessageType messageType);
        Task<List<NotificationViewModel>> GetLatestNotificationsAsync(int userId);
        Task MarkAsReadAsync(int notificationId);
        Task MarkAllAsReadAsync(int userId);
    }
} 