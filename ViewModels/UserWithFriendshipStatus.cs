namespace Zela.ViewModels
{
    /// <summary>
    /// Dùng trong trang Find để hiển thị trạng thái quan hệ giữa currentUser và người khác.
    /// </summary>
    public class UserWithFriendshipStatus
    {
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }

        /// <summary>
        /// StatusId: 0 = None, 1 = Pending, 2 = Accepted, 3 = Rejected (có thể không hiển thị)
        /// </summary>
        public int StatusId { get; set; }

        public FriendshipRole Role { get; set; }
        
        /// <summary>
        /// Số lượng bạn chung giữa currentUser và user này
        /// </summary>
        public int MutualFriendsCount { get; set; }
    }

    public enum FriendshipRole
    {
        None = 0,
        PendingSent = 1,       // currentUser đã gửi lời mời
        PendingReceived = 2,   // currentUser có lời mời đến
        Accepted = 3           // hai bên đã là bạn
    }
}