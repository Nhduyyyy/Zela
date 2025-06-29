using Zela.Models;

namespace Zela.ViewModels
{
    /// <summary>
    /// ViewModel cho trang hiển thị danh sách bạn chung giữa hai user
    /// </summary>
    public class MutualFriendsViewModel
    {
        /// <summary>
        /// Thông tin user mà ta đang xem bạn chung
        /// </summary>
        public User TargetUser { get; set; }
        
        /// <summary>
        /// Danh sách bạn chung
        /// </summary>
        public List<User> MutualFriends { get; set; } = new List<User>();
        
        /// <summary>
        /// Tổng số bạn chung
        /// </summary>
        public int TotalCount { get; set; }
        
        /// <summary>
        /// User hiện tại (để hiển thị context)
        /// </summary>
        public User CurrentUser { get; set; }
    }
} 