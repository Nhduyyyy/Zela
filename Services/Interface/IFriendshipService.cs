using Zela.Models;
using Zela.ViewModels;

namespace Zela.Services
{
    public interface IFriendshipService
    {
        /// <summary>
        /// Gửi lời mời kết bạn (currentUser gửi đến targetUser).
        /// Trả về true nếu thành công, false nếu đã có record liên quan hoặc lỗi.
        /// </summary>
        Task<bool> SendFriendRequestAsync(int currentUserId, int targetUserId);

        /// <summary>
        /// Hủy lời mời đã gửi (currentUserId là người gửi, targetUserId là người nhận, status = Pending)
        /// </summary>
        Task<bool> CancelFriendRequestAsync(int currentUserId, int targetUserId);

        /// <summary>
        /// Chấp nhận lời mời (currentUserId là người nhận, requesterId là người gửi).
        /// </summary>
        Task<bool> AcceptFriendRequestAsync(int currentUserId, int requesterId);

        /// <summary>
        /// Từ chối lời mời (currentUserId là người nhận, requesterId là người gửi).
        /// </summary>
        Task<bool> RejectFriendRequestAsync(int currentUserId, int requesterId);

        /// <summary>
        /// Hủy kết bạn (currentUserId hoặc friendId có thể là requester hoặc addressee,
        /// nhưng chỉ thực hiện khi status = Accepted).
        /// </summary>
        Task<bool> RemoveFriendAsync(int currentUserId, int friendUserId);

        /// <summary>
        /// Lấy danh sách bạn bè (Status = Accepted) của currentUserId.
        /// </summary>
        Task<IEnumerable<User>> GetFriendsAsync(int currentUserId);

        /// <summary>
        /// Lấy danh sách lời mời đến (chờ phản hồi) của currentUserId (Status = Pending).
        /// </summary>
        Task<IEnumerable<FriendRequestInfo>> GetIncomingRequestsAsync(int currentUserId);

        /// <summary>
        /// Lấy danh sách lời mời đã gửi (Status = Pending, currentUser là người gửi).
        /// </summary>
        Task<IEnumerable<FriendRequestInfo>> GetOutgoingRequestsAsync(int currentUserId);

        /// <summary>
        /// Lấy tất cả user (trừ currentUser), kèm quan hệ hiện tại giữa currentUser và mỗi user đó.
        /// </summary>
        Task<IEnumerable<UserWithFriendshipStatus>> GetAllUsersExceptCurrentAsync(int currentUserId);
        
        
        
        
        Task<List<FriendViewModel>> SearchFriendsAsync(int currentUserId, string keyword);
        
        Task<List<FriendViewModel>> GetFriendListAsync(int userId);

        // ---------------------------------------------
        // MUTUAL FRIENDS FEATURES
        // ---------------------------------------------
        
        /// <summary>
        /// Lấy danh sách bạn chung giữa currentUser và targetUser
        /// </summary>
        Task<IEnumerable<User>> GetMutualFriendsAsync(int currentUserId, int targetUserId);
        
        /// <summary>
        /// Đếm số lượng bạn chung giữa currentUser và targetUser
        /// </summary>
        Task<int> GetMutualFriendsCountAsync(int currentUserId, int targetUserId);
        
        /// <summary>
        /// Lấy gợi ý kết bạn dựa trên số lượng bạn chung (top N users)
        /// </summary>
        Task<IEnumerable<UserWithMutualFriendsCount>> GetFriendSuggestionsAsync(int currentUserId, int limit = 10);
        
        /// <summary>
        /// Lấy tất cả user (trừ currentUser), kèm quan hệ và số lượng bạn chung
        /// </summary>
        Task<IEnumerable<UserWithFriendshipStatus>> GetAllUsersWithMutualFriendsAsync(int currentUserId);
    }
}
