/*
 * File   : FriendshipIndexViewModel.cs
 * Author : A–DUY
 * Date   : 2025-05-31
 * Desc   : ViewModel tổng hợp dữ liệu cho trang chính quản lý bạn bè.
 *          Bao gồm:
 *            - Friends           : danh sách User đã là bạn (Accepted)
 *            - IncomingRequests  : danh sách lời mời đang chờ (Pending) mà currentUser nhận được
 *            - OutgoingRequests  : danh sách lời mời đang chờ (Pending) mà currentUser đã gửi
 */

using Zela.Models;
using Zela.ViewModels;

namespace ZelaTestDemo.ViewModels
{
    /// <summary>
    /// ViewModel dùng cho action Index() của FriendshipController.
    /// Gộp ba danh sách chính:
    ///   1) Friends           : List<User/> - những người currentUser đã chấp nhận kết bạn.
    ///   2) IncomingRequests  : List<FriendRequestInfo/> - lời mời đến, currentUser là Addressee, status = Pending.
    ///   3) OutgoingRequests  : List<FriendRequestInfo/> - lời mời đã gửi, currentUser là Requester, status = Pending.
    /// </summary>
    public class FriendshipIndexViewModel
    {
        /// <summary>
        /// Danh sách User đã chấp nhận kết bạn với currentUser.
        /// Mỗi User trong này có thể được hiển thị tên, avatar, v.v.
        /// </summary>
        public List<User> Friends { get; set; }
        
        /// <summary>
        /// Danh sách lời mời đến (Incoming Requests) mà currentUser nhận được.
        /// Mỗi phần tử chứa thông tin FriendRequestInfo, bao gồm:
        ///   - FriendshipId, RequesterId, RequesterName
        ///   - AddresseeId, AddresseeName (currentUser)
        ///   - StatusId, StatusName (ví dụ "Pending")
        ///   - RequestedAt (ngày giờ tạo lời mời)
        /// </summary>
        public List<FriendRequestInfo> IncomingRequests { get; set; }
        
        /// <summary>
        /// Danh sách lời mời đã gửi (Outgoing Requests) mà currentUser khởi tạo.
        /// Mỗi phần tử chứa thông tin FriendRequestInfo, bao gồm:
        ///   - FriendshipId, RequesterId (currentUser), RequesterName
        ///   - AddresseeId, AddresseeName
        ///   - StatusId, StatusName (ví dụ "Pending")
        ///   - RequestedAt (ngày giờ tạo lời mời)
        /// </summary>
        public List<FriendRequestInfo> OutgoingRequests { get; set; }
    }
}