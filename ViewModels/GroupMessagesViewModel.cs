using System.Collections.Generic;
using Zela.Models;

namespace Zela.ViewModels
{
    public class GroupMessagesViewModel
    {
        public int GroupId { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public List<GroupMessageViewModel> Messages { get; set; } = new List<GroupMessageViewModel>();
        public ChatGroup? Group { get; set; }
    }
} 