namespace Zela.ViewModels
{
    /// <summary>
    /// ViewModel cho tính năng gợi ý kết bạn dựa trên bạn chung
    /// </summary>
    public class UserWithMutualFriendsCount
    {
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string? AvatarUrl { get; set; }
        public string? Email { get; set; }
        
        /// <summary>
        /// Số lượng bạn chung với user hiện tại
        /// </summary>
        public int MutualFriendsCount { get; set; }
        
        /// <summary>
        /// Trạng thái quan hệ hiện tại với user này
        /// </summary>
        public FriendshipRole Role { get; set; }
        
        /// <summary>
        /// Danh sách tên một vài bạn chung (để hiển thị preview)
        /// </summary>
        public List<string> MutualFriendsPreview { get; set; } = new List<string>();
    }
} 