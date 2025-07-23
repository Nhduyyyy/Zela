/*
 * File   : FriendRequestInfo.cs
 * Author : A–DUY
 * Date   : 2025-05-31
 * Desc   : ViewModel chứa đầy đủ thông tin của một lời mời kết bạn 
 *          (cả trường hợp đang chờ lẫn đã xử lý) để hiển thị lên View.
 */

namespace Zela.ViewModels
{
    /// <summary>
    /// Thông tin một lời mời kết bạn (đang hoặc đã xử lý).
    /// Mỗi đối tượng chứa đầy đủ dữ liệu cần thiết để hiển thị hoặc xử lý trong View:
    ///   - FriendshipId: khóa chính của record trong bảng Friendship
    ///   - RequesterId / RequesterName: thông tin người gửi lời mời
    ///   - AddresseeId / AddresseeName: thông tin người nhận lời mời
    ///   - StatusId / StatusName    : trạng thái hiện tại (Pending, Accepted, etc.)
    ///   - RequestedAt              : thời điểm lời mời được tạo
    /// </summary>
    public class FriendRequestInfo
    {
        /// <summary>
        /// Khóa chính của record trong bảng Friendship. 
        /// Cần dùng khi Accept/Reject/Cancel một lời mời (controller sẽ nhận tham số này để xác định record cần xử lý).
        /// </summary>
        public int FriendshipId { get; set; }

        /// <summary>
        /// ID của user đã gửi lời mời (Requester). 
        /// Khi currentUser xem danh sách IncomingRequests, giá trị này giúp xác định ai đã gửi.
        /// </summary>
        public int RequesterId { get; set; }

        /// <summary>
        /// Tên hiển thị (FullName hoặc Email) của người gửi. 
        /// Ví dụ, nếu Requester.FullName khác null, dùng FullName; ngược lại dùng Email.
        /// </summary>
        public string RequesterName { get; set; }

        /// <summary>
        /// ID của user nhận lời mời (Addressee). 
        /// Trong danh sách OutgoingRequests, giá trị này là user mà currentUser đã gửi lời mời.
        /// </summary>
        public int AddresseeId { get; set; }

        /// <summary>
        /// Tên hiển thị (FullName hoặc Email) của người nhận. 
        /// Thông tin này thường không cần hiển thị khi currentUser là Addressee, nhưng vẫn giữ để dễ debug hoặc hiển thị full info.
        /// </summary>
        public string AddresseeName { get; set; }

        /// <summary>
        /// Mã trạng thái (ví dụ 3 = Pending, 4 = Accepted, ...). 
        /// Dùng để logic bên View (ví dụ chọn nút nào, màu hiển thị nào).
        /// </summary>
        public int StatusId { get; set; }

        /// <summary>
        /// Tên trạng thái (ví dụ "Pending", "Accepted", "Removed", ...).
        /// Dùng để hiển thị trực tiếp trong View cho người dùng dễ hiểu.
        /// </summary>
        public string StatusName { get; set; }

        /// <summary>
        /// Thời điểm (DateTime) mà lời mời được tạo (tương ứng với cột CreatedAt trong bảng Friendship).
        /// Hiển thị cho người dùng biết ngày giờ gửi lời mời.
        /// </summary>
        public DateTime RequestedAt { get; set; }
    }
}