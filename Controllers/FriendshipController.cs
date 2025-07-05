/*
 * File   : FriendshipController.cs
 * Author : A–DUY
 * Date   : 2025-05-31
 * Desc   : Xử lý các yêu cầu liên quan đến tính năng kết bạn
 */

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zela.Services;
using Zela.ViewModels;
using ZelaTestDemo.ViewModels;
using Zela.Models;


namespace Zela.Controllers
{
    // Chỉ cho phép user đã xác thực (đăng nhập) truy cập vào tất cả action trong controller này.
    [Authorize]
    public class FriendshipController : Controller
    {
        // Dependency injection: service xử lý nghiệp vụ liên quan đến kết bạn
        private readonly IFriendshipService _friendshipService;

        /// <summary>
        /// Constructor: nhận vào implement của IFriendshipService để xử lý logic nghiệp vụ.
        /// </summary>
        /// <param name="friendshipService">
        ///    Đối tượng service đã được cấu hình trong DI container, 
        ///    cung cấp các phương thức như GetFriendsAsync, SendFriendRequestAsync, v.v.
        /// </param>
        public FriendshipController(IFriendshipService friendshipService)
        {
            _friendshipService = friendshipService;
        }

        // ---------------------------------------------
        // Author : A–DUY
        // Date   : 2025-05-31
        // Task   : Phương thức tiện ích lấy currentUserId từ Session
        // ---------------------------------------------
        /// <summary>
        /// Cố gắng lấy userId của người dùng hiện tại từ Session.
        /// Khi user login bằng Google, trong AccountController.GoogleResponse() 
        /// đã lưu user.UserId vào Session với key "UserId".
        /// </summary>
        /// <param name="currentUserId">
        ///    (out) đầu ra: chứa giá trị userId nếu Session có lưu trữ, 
        ///    ngược lại bằng 0.
        /// </param>
        /// <returns>
        ///    True nếu Session có chứa UserId (và currentUserId được gán đúng), 
        ///    False nếu Session không có hoặc đã hết hạn.
        /// </returns>
        private bool TryGetCurrentUserId(out int currentUserId)
        {
            // Lấy UserId (kiểu int?) từ Session; nếu session hết hạn hoặc chưa login, giá trị là null.
            var id = HttpContext.Session.GetInt32("UserId");
            if (id.HasValue)
            {
                // Gán giá trị của userId vào biến out và trả về true để báo lấy thành công.
                currentUserId = id.Value;  // gán userId thực sự vào biến out
                return true;              // báo là đã lấy được
            }
            
            // Nếu không tìm thấy trong Session, gán 0 và trả về false.
            currentUserId = 0;   // không lấy được thì gán 0
            return false;       // báo là không có UserId trong Session
        }

        // ---------------------------------------------
        // Author : A–DUY
        // Date   : 2025-05-31
        // Task   : Hiển thị trang chính (Index) với ba phần: Friends, IncomingRequests, OutgoingRequests
        // ---------------------------------------------
        /// <summary>
        /// Hiển thị trang chính quản lý bạn bè:
        ///  1) Danh sách bạn bè hiện tại (Friends) - những người đã chấp nhận kết bạn (status = Accepted)
        ///  2) Danh sách lời mời đến (IncomingRequests) - những user gửi request đến user hiện tại, status = Pending
        ///  3) Danh sách lời mời đã gửi (OutgoingRequests) - những user hiện tại gửi request đến, status = Pending
        /// Các bảng sẽ được gộp trong một ViewModel duy nhất để view dễ hiển thị.
        /// </summary>
        /// <returns>
        ///    View với model là FriendshipIndexViewModel chứa ba danh sách (List).
        /// </returns>
        public async Task<IActionResult> Index()
        {
            // Nếu không lấy được currentUserId (Session không có hoặc hết hạn), trả về Forbid (HTTP 403).
            if (!TryGetCurrentUserId(out var currentUserId))
                return Forbid();
            
            // ----- Lấy danh sách bạn bè đã chấp nhận -----
            // Service.GetFriendsAsync trả về danh sách FriendInfo (ID và tên của bạn bè).
            // Chỉ những record có status == "Accepted" giữa currentUserId và friendId mới được include.
            var friends = (await _friendshipService.GetFriendsAsync(currentUserId)).ToList();
            
            // ----- Lấy danh sách lời mời đến (IncomingRequests) -----
            // Service.GetIncomingRequestsAsync trả về list FriendRequestInfo cho user hiện tại 
            // làm Addressee, status = "Pending". Mỗi phần tử chứa thông tin requesterId và requesterName.
            var incoming = (await _friendshipService.GetIncomingRequestsAsync(currentUserId)).ToList();
            
            // ----- Lấy danh sách lời mời đã gửi (OutgoingRequests) -----
            // Service.GetOutgoingRequestsAsync trả về list FriendRequestInfo cho user hiện tại 
            // làm Requester, status = "Pending". Mỗi phần tử chứa thông tin addresseeId và addresseeName.
            var outgoing = (await _friendshipService.GetOutgoingRequestsAsync(currentUserId)).ToList();
            
            // Tạo ViewModel tổng hợp ba danh sách để view dễ render:
            //      - Friends: List<FriendInfo>
            //      - IncomingRequests: List<FriendRequestInfo>
            //      - OutgoingRequests: List<FriendRequestInfo>
            var vm = new FriendshipIndexViewModel
            {
                Friends = friends,
                IncomingRequests = incoming,
                OutgoingRequests = outgoing
            };
            
            // Trả về view Index.cshtml, view sẽ dùng vm để hiển thị ba bảng tương ứng.
            return View(vm);
        }

        // ---------------------------------------------
        // Author : A–DUY
        // Date   : 2025-05-31
        // Task   : Hiển thị trang "Find" - danh sách tất cả user trừ currentUser
        // ---------------------------------------------
        /// <summary>
        /// Hiển thị trang "Find" để user có thể xem toàn bộ user khác (không bao gồm chính mình)
        /// và biết ngay trạng thái quan hệ (RelationStatus) giữa họ hiện tại và currentUser:
        ///   - Nếu chưa có quan hệ (None): hiển thị nút "Add Friend".
        ///   - Nếu đang chờ (Pending) do currentUser gửi: hiển thị "Đã gửi" hoặc nút Cancel.
        ///   - Nếu đang chờ (Pending) do user khác gửi đến currentUser: hiển thị "Chờ phản hồi" hoặc nút Accept/Reject.
        ///   - Nếu đã là bạn (Accepted): hiển thị "Đã là bạn" hoặc nút Unfriend.
        /// </summary>
        /// <returns>
        ///    View với model là IEnumerable<UserWithFriendshipStatus/>,
        ///    mỗi phần tử chứa: UserId, UserName, RelationStatusId, RelationStatusName.
        /// </returns>
        public async Task<IActionResult> Find()
        {
            // Nếu Session không chứa UserId, ngăn truy cập
            if (!TryGetCurrentUserId(out var currentUserId))
                return Forbid();
            
            // Gọi service để lấy danh sách các user khác,
            // kèm trạng thái quan hệ và thông tin bạn chung so với currentUserId.
            var allUsers = await _friendshipService.GetAllUsersWithMutualFriendsAsync(currentUserId);
            
            // Trả về view Find.cshtml với model chứa list user, status và mutual friends count.
            return View(allUsers);
        }
        
        // ---------------------------------------------
        // Author : A–DUY
        // Date   : 2025-05-31
        // Task   : Gửi lời mời kết bạn (Send Request)
        // ---------------------------------------------
        /// <summary>
        /// Khi user nhấn nút "Add Friend" trên trang Find.cshtml.
        /// Thao tác này chỉ có thể thực hiện khi user đã đăng nhập.
        /// Gọi service để lưu record mới trong bảng Friendship với:
        ///    RequesterId = currentUserId, AddresseeId = userId2, Status = "Pending".
        /// Nếu đã tồn tại record pending/accepted, service sẽ trả về false.
        /// </summary>
        /// <param name="userId2">ID của user được gửi lời mời (Addressee)</param>
        /// <returns>
        ///    Redirect về trang Find để view hiển thị trạng thái mới.
        ///    Nếu không thành công (service trả false), TempData["Error"] chứa thông báo lỗi.
        /// </returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendRequest(int userId2)
        {
            // Kiểm tra Session, lấy ID của người gửi (currentUserId)
            if (!TryGetCurrentUserId(out var userId1))
                return Forbid();
            
            // Gọi service gửi friend request.
            // Service sẽ kiểm tra:
            //   - userId1 != userId2 (không thể tự gửi cho chính mình)
            //   - Chưa có record pending/accepted giữa hai ID
            //   - Tạo record mới với Status = "Pending"
            var success = await _friendshipService.SendFriendRequestAsync(userId1, userId2);
            
            // Kiểm tra nếu request là AJAX
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                if (!success)
                    return Json(new { success = false, message = "Không thể gửi lời mời (đã gửi hoặc đã là bạn)." });
                
                return Json(new { success = true });
            }
            
            // Nếu không phải AJAX request, xử lý như cũ
            if (!success)
                TempData["Error"] = "Không thể gửi lời mời (đã gửi hoặc đã là bạn).";
            
            return RedirectToAction(nameof(Find));
        }
        
        // ---------------------------------------------
        // Author : A–DUY
        // Date   : 2025-05-31
        // Task   : Hủy lời mời đã gửi (Cancel Outgoing Request)
        // ---------------------------------------------
        /// <summary>
        /// Khi user nhấn "Cancel" trên danh sách lời mời đã gửi (OutgoingRequests)
        /// trong trang Index.cshtml.
        /// Gọi service để cập nhật Status của record Friendship thành "Cancelled"
        /// (hoặc xóa luôn record, tùy thiết kế).
        /// </summary>
        /// <param name="userId2">ID của user mà lời mời đang gửi đến (Addressee)</param>
        /// <returns>
        ///    Redirect về trang Index để load lại danh sách;
        ///    Nếu thất bại, TempData["Error"] chứa thông báo lỗi.
        /// </returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelRequest(int userId2)
        {
            // Lấy ID người gửi (currentUser) từ Session
            if (!TryGetCurrentUserId(out var userId1))
                return Forbid();
            
            // Gọi service hủy request:
            //   - Tìm record với RequesterId = userId1, AddresseeId = userId2, Status = "Pending"
            //   - Nếu tồn tại, update Status = "Cancelled" (hoặc xóa)
            //   - Nếu không tìm thấy, trả về false
            var success = await _friendshipService.CancelFriendRequestAsync(userId1, userId2);
            
            // Nếu không thành công, lưu thông báo lỗi
            if (!success)
                TempData["Error"] = "Hủy lời mời không thành công.";
            
            // Quay về Index để cập nhật lại danh sách
            return RedirectToAction(nameof(Index));
        }
        
        // ---------------------------------------------
        // Author : A–DUY
        // Date   : 2025-05-31
        // Task   : Chấp nhận lời mời đến (Accept Incoming Request)
        // ---------------------------------------------
        /// <summary>
        /// Khi user nhấn "Accept" trên danh sách lời mời đến (IncomingRequests)
        /// trong trang Index.cshtml.
        /// Gọi service để update Status của record Friendship thành "Accepted".
        /// </summary>
        /// <param name="requesterId">ID của người đã gửi lời mời (Requester)</param>
        /// <returns>
        ///    Redirect về trang Index; Nếu thất bại (không tìm thấy record hoặc status không phù hợp),
        ///    TempData["Error"] sẽ chứa thông báo lỗi.
        /// </returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptRequest(int requesterId)
        {
            // Lấy ID của người nhận (currentUser) từ Session
            if (!TryGetCurrentUserId(out var addresseeId))
                return Forbid();
            
            // Gọi service accept request:
            //   - Tìm record với RequesterId = requesterId, AddresseeId = addresseeId, Status = "Pending"
            //   - Nếu tồn tại, update Status = "Accepted", cập nhật UpdatedAt
            //   - Nếu không tìm thấy hoặc status != "Pending", return false
            var success = await _friendshipService.AcceptFriendRequestAsync(addresseeId, requesterId);
            
            // Nếu không thành công, ghi lỗi vào TempData
            if (!success)
                TempData["Error"] = "Chấp nhận lời mời không thành công.";
            
            // Quay lại Index để cập nhật lại danh sách Friends/Incoming/Outgoing
            return RedirectToAction(nameof(Index));
        }
        
        // ---------------------------------------------
        // Author : A–DUY
        // Date   : 2025-05-31
        // Task   : Từ chối lời mời đến (Reject Incoming Request)
        // ---------------------------------------------
        /// <summary>
        /// Khi user nhấn "Reject" trên danh sách lời mời đến (IncomingRequests)
        /// trong trang Index.cshtml.
        /// Gọi service để update Status của record Friendship thành "Removed" (hoặc "Rejected").
        /// </summary>
        /// <param name="requesterId">ID của người đã gửi lời mời (Requester)</param>
        /// <returns>
        ///    Redirect về trang Index; Nếu thất bại, TempData["Error"] chứa thông báo lỗi.
        /// </returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectRequest(int requesterId)
        {
            // Lấy ID của người nhận (currentUser) từ Session
            if (!TryGetCurrentUserId(out var addresseeId))
                return Forbid();
            
            // Gọi service reject request:
            //   - Tìm record với RequesterId = requesterId, AddresseeId = addresseeId, Status = "Pending"
            //   - Nếu tồn tại, update Status = "Removed" (hoặc xóa)
            //   - Nếu không, return false
            var success = await _friendshipService.RejectFriendRequestAsync(addresseeId, requesterId);
            
            // Nếu không thành công, lưu lỗi
            if (!success)
                TempData["Error"] = "Từ chối lời mời không thành công.";
            
            // Quay lại Index để cập nhật lại danh sách
            return RedirectToAction(nameof(Index));
        }
        
        // ---------------------------------------------
        // Author : A–DUY
        // Date   : 2025-05-31
        // Task   : Hủy kết bạn (Remove Friend)
        // ---------------------------------------------
        /// <summary>
        /// Khi user nhấn "Unfriend" trong danh sách bạn bè (Friends)
        /// trên trang Index.cshtml.
        /// Gọi service để update Status của record Friendship thành "Removed".
        /// Nếu không tìm thấy record hoặc status != "Accepted", service trả false.
        /// </summary>
        /// <param name="friendId">ID của user bạn muốn hủy (Friend)</param>
        /// <returns>
        ///    Redirect về trang Index; Nếu thất bại, TempData["Error"] chứa thông báo lỗi.
        /// </returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFriend(int friendId)
        {
            // Lấy ID của currentUser từ Session
            if (!TryGetCurrentUserId(out var currentUserId))
                return Forbid();
            
            // Gọi service remove friend:
            //   - Tìm record với ((RequesterId = currentUserId && AddresseeId = friendId)
            //     OR (RequesterId = friendId && AddresseeId = currentUserId)) AND Status = "Accepted"
            //   - Nếu tồn tại, update Status = "Removed"
            //   - Nếu không, return false
            var success = await _friendshipService.RemoveFriendAsync(currentUserId, friendId);
            
            // Nếu không thành công, lưu lỗi
            if (!success)
                TempData["Error"] = "Hủy kết bạn không thành công.";
            
            // Quay lại Index để cập nhật lại danh sách
            return RedirectToAction(nameof(Index));
        }
        
        [HttpGet]
        public async Task<IActionResult> FilterFriends(string keyword)
        {
            // Check đăng nhập và Identity
            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                return Unauthorized(); // 401 nếu chưa đăng nhập
            }

            // Lấy UserId từ claim (nếu có)
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int currentUserId))
            {
                return Unauthorized(); // hoặc return Forbid(); tùy bạn
            }

            // Nếu không nhập keyword (xóa hết chữ), trả về toàn bộ bạn bè
            if (string.IsNullOrWhiteSpace(keyword))
            {
                var allFriends = await _friendshipService.GetFriendListAsync(currentUserId);
                return PartialView("_FriendListPartial", allFriends);
            }

            // Có keyword -> tìm bạn theo keyword
            var filteredFriends = await _friendshipService.SearchFriendsAsync(currentUserId, keyword);
            return PartialView("_FriendListPartial", filteredFriends);
        }

        // ---------------------------------------------
        // MUTUAL FRIENDS FEATURES
        // Author : A–DUY  
        // Date   : 2025-01-XX
        // Task   : Các actions cho tính năng bạn chung
        // ---------------------------------------------

        /// <summary>
        /// Hiển thị danh sách bạn chung giữa currentUser và targetUser
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> MutualFriends(int targetUserId)
        {
            if (!TryGetCurrentUserId(out var currentUserId))
                return Forbid();

            // Lấy thông tin targetUser
            var targetUser = await _friendshipService.GetFriendsAsync(currentUserId);
            var target = targetUser.FirstOrDefault(u => u.UserId == targetUserId);
            
            if (target == null)
            {
                // Nếu không phải bạn bè, có thể lấy từ database
                // Hoặc return NotFound tùy business logic
                TempData["Error"] = "Không thể xem bạn chung với người này.";
                return RedirectToAction(nameof(Find));
            }

            // Lấy danh sách bạn chung
            var mutualFriends = await _friendshipService.GetMutualFriendsAsync(currentUserId, targetUserId);
            
            // Lấy thông tin currentUser để hiển thị
            var currentUser = await _friendshipService.GetFriendsAsync(currentUserId);
            
            var viewModel = new MutualFriendsViewModel
            {
                TargetUser = target,
                MutualFriends = mutualFriends.ToList(),
                TotalCount = mutualFriends.Count(),
                CurrentUser = currentUser.FirstOrDefault() // Cần fix logic này
            };

            return View(viewModel);
        }

        /// <summary>
        /// API lấy số lượng bạn chung (cho AJAX calls)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetMutualFriendsCount(int targetUserId)
        {
            if (!TryGetCurrentUserId(out var currentUserId))
                return Json(new { success = false, message = "Chưa đăng nhập" });

            try
            {
                var count = await _friendshipService.GetMutualFriendsCountAsync(currentUserId, targetUserId);
                return Json(new { success = true, count = count });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi lấy thông tin bạn chung" });
            }
        }

        /// <summary>
        /// Trang gợi ý kết bạn dựa trên bạn chung
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Suggestions()
        {
            if (!TryGetCurrentUserId(out var currentUserId))
                return Forbid();

            var suggestions = await _friendshipService.GetFriendSuggestionsAsync(currentUserId, 20);
            
            return View(suggestions);
        }

        /// <summary>
        /// Cập nhật Find action để hiển thị thông tin bạn chung
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> FindWithMutualFriends()
        {
            if (!TryGetCurrentUserId(out var currentUserId))
                return Forbid();

            // Sử dụng method mới có thông tin bạn chung
            var users = await _friendshipService.GetAllUsersWithMutualFriendsAsync(currentUserId);
            
            return View("Find", users);
        }

        /// <summary>
        /// API lấy danh sách bạn chung (cho modal popup)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetMutualFriendsList(int targetUserId)
        {
            if (!TryGetCurrentUserId(out var currentUserId))
                return Json(new { success = false, message = "Chưa đăng nhập" });

            try
            {
                var mutualFriends = await _friendshipService.GetMutualFriendsAsync(currentUserId, targetUserId);
                var friendsList = mutualFriends.Select(f => new {
                    userId = f.UserId,
                    name = f.FullName ?? f.Email,
                    avatarUrl = f.AvatarUrl ?? "/images/default-avatar.jpeg"
                }).ToList();

                return Json(new { success = true, friends = friendsList });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi lấy danh sách bạn chung" });
            }
        }

        // API trả về danh sách bạn bè dạng JSON cho modal tạo nhóm
        [HttpGet]
        public async Task<IActionResult> GetMyFriends()
        {
            if (!TryGetCurrentUserId(out var currentUserId))
                return Json(new { success = false, message = "Chưa đăng nhập" });
            var friends = await _friendshipService.GetFriendListAsync(currentUserId);
            var result = friends.Select(f => new {
                userId = f.UserId,
                name = f.FullName ?? f.Email,
                avatarUrl = f.AvatarUrl ?? "/images/default-avatar.jpeg"
            }).ToList();
            return Json(result);
        }
    }
}