using Zela.Models;
using System;
using Zela.Enum;

namespace Zela.ViewModels
{
    public class NotificationViewModel
    {
        public int NotificationId { get; set; }
        public string SenderName { get; set; }
        public string Content { get; set; }
        public MessageType MessageType { get; set; }
        public DateTime Timestamp { get; set; }
        public bool IsRead { get; set; }
    }
} 