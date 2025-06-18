using Microsoft.EntityFrameworkCore;
using Zela.DbContext;
using Zela.Models;
using Zela.ViewModels;

namespace Zela.Services
{
    public class FriendshipService : IFriendshipService
    {
        private readonly ApplicationDbContext _dbContext;

        public FriendshipService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // ---------------------------------------------
        // Author : A–DUY
        // Date   : 2025-05-31
        // Task   : Helper: Tìm record Friendship giữa hai user, bất kể thứ tự UserId1/UserId2
        // ---------------------------------------------
        /// <summary>
        /// Helper method để tìm một record Friendship giữa hai user bất kỳ:
        /// 1) Include bảng Status để lấy thông tin StatusName nếu cần.
        /// 2) Include bảng Requester để load thông tin user (đã gửi lời mời).
        /// 3) Include bảng Addressee để load thông tin user (người nhận lời mời).
        /// 4) Sử dụng điều kiện tìm kiếm: 
        ///    - (f.UserId1 == userAId && f.UserId2 == userBId) OR 
        ///    - (f.UserId1 == userBId && f.UserId2 == userAId)
        ///    để không phụ thuộc vào thứ tự tham số (Requester/Addressee).
        /// </summary>
        /// <param name="userAId">ID của user thứ nhất.</param>
        /// <param name="userBId">ID của user thứ hai.</param>
        /// <returns>
        ///     Task&lt;Friendship&gt; chứa record Friendship nếu tồn tại, 
        ///     hoặc null nếu không tìm thấy.
        /// </returns>
        private Task<Friendship?> GetFriendshipBetweenAsync(int userAId, int userBId)
        {
            return _dbContext.Friendships
                // Include bảng Status để có thể truy xuất f.Status.StatuName bên ngoài nếu cần
                .Include(f => f.Status)
                // Include bảng User cho cột Requester (UserId1) để truy xuất thông tin user gửi
                .Include(f => f.Requester)
                // Include bảng User cho cột Addressee (UserId2) để truy xuất thông tin user nhận
                .Include(f => f.Addressee)
                // FirstOrDefaultAsync sẽ trả về phần tử đầu tiên thỏa điều kiện hoặc null
                .FirstOrDefaultAsync(f =>
                    // Trường hợp userAId là Requester và userBId là Addressee
                    (f.UserId1 == userAId && f.UserId2 == userBId) ||
                    // Hoặc ngược lại userBId là Requester và userAId là Addressee
                    (f.UserId1 == userBId && f.UserId2 == userAId));
        }

        // ---------------------------------------------
        // Author : A–DUY
        // Date   : 2025-05-31
        // Task   : Gửi yêu cầu kết bạn từ currentUser đến targetUser
        // ---------------------------------------------
        /// <summary>
        /// 1) Kiểm tra xem currentUserId và targetUserId có giống nhau không; nếu giống, không thể tự gửi lời mời cho chính mình => trả về false.
        /// 2) Kiểm tra xem targetUser có tồn tại trong bảng Users không; nếu không, không thể gửi lời mời => trả về false.
        /// 3) Gọi GetFriendshipBetweenAsync để xác định xem đã tồn tại một record Friendship giữa hai user chưa:
        ///    - Nếu đã tồn tại và status = Pending hoặc Accepted (hoặc bất kỳ status nào khác), không cho phép gửi lại => trả về false.
        /// 4) Nếu chưa tồn tại bất kỳ record nào, tạo một đối tượng Friendship mới với:
        ///    - UserId1 = currentUserId   (currentUser là Requester)
        ///    - UserId2 = targetUserId    (targetUser là Addressee)
        ///    - CreatedAt = DateTime.UtcNow (thời điểm tạo lời mời)
        ///    - StatusId = 3              (3 tương đương Pending)
        ///   Sau đó thêm vào DbContext và gọi SaveChangesAsync để lưu vào database.
        /// 5) Nếu tất cả các bước thành công, trả về true (nghĩa là lời mời đã được tạo).
        /// </summary>
        /// <param name="currentUserId">ID của user hiện tại, người muốn gửi lời mời kết bạn.</param>
        /// <param name="targetUserId">ID của user đích, người sẽ nhận lời mời.</param>
        /// <returns>
        ///     True nếu lời mời đã được tạo thành công; False nếu bất kỳ bước kiểm tra nào thất bại.
        /// </returns>
        public async Task<bool> SendFriendRequestAsync(int currentUserId, int targetUserId)
        {
            // Nếu currentUser và targetUser cùng một ID => không thể gửi lời mời cho chính mình
            if (currentUserId == targetUserId)
                return false;

            // BƯỚC 1: Kiểm tra xem targetUser tồn tại trong bảng Users hay không
            // FindAsync trả về User nếu tìm thấy, ngược lại trả về null
            var targetUser = await _dbContext.Users.FindAsync(targetUserId);

            // Nếu targetUser null => user này không tồn tại => không thể gửi lời mời
            if (targetUser == null)
                return false;

            // BƯỚC 2: Kiểm tra xem đã tồn tại record Friendship giữa hai user không
            // Gọi phương thức helper GetFriendshipBetweenAsync (đã định nghĩa) để tìm record
            var existing = await GetFriendshipBetweenAsync(currentUserId, targetUserId);
            // Nếu existing != null nghĩa là đã có record Friendship (Pending/Accepted/Rejected/etc.)
            if (existing != null)
            {
                // Nếu đã tồn tại record với bất kỳ trạng thái nào (Pending, Accepted, Rejected),
                // theo logic hiện tại không cho phép gửi lại => trả false.
                return false;
            }

            // BƯỚC 3: Tạo record Friendship mới với trạng thái Pending (StatusId = 3)
            var newRequest = new Friendship
            {
                // currentUserId đóng vai Requester
                UserId1 = currentUserId,
                // targetUserId đóng vai Addressee
                UserId2 = targetUserId,
                // Đánh dấu thời điểm lời mời được tạo
                CreatedAt = DateTime.UtcNow,
                // Gán status = Pending (ví dụ Pending tương ứng với StatusId = 3)
                StatusId = 1
            };

            // Thêm đối tượng mới vào DbContext; nó sẽ chờ SaveChangesAsync mới thực sự ghi vào DB
            _dbContext.Friendships.Add(newRequest);

            // BƯỚC 4: Lưu các thay đổi vào database
            await _dbContext.SaveChangesAsync();
            return true;
        }

        // ---------------------------------------------
        // Author : A–DUY
        // Date   : 2025-06-04
        // Task   : Hủy lời mời kết bạn đã gửi khi lời mời vẫn còn ở trạng thái Pending
        // ---------------------------------------------
        /// <summary>
        /// 1) Kiểm tra xem có tồn tại lời mời kết bạn từ currentUserId đến targetUserId hay không,
        ///    và lời mời này phải đang ở trạng thái Pending (StatusId = 3).
        /// 2) Nếu không tìm thấy lời mời thỏa điều kiện => trả về false.
        /// 3) Nếu tìm thấy, tiến hành hủy lời mời bằng cách xoá bản ghi Friendship tương ứng.
        /// 4) Lưu thay đổi vào database.
        /// 5) Trả về true nếu lời mời đã được hủy thành công.
        /// </summary>
        /// <param name="currentUserId">ID của user hiện tại, người đã gửi lời mời.</param>
        /// <param name="targetUserId">ID của user nhận lời mời.</param>
        /// <returns>
        ///     True nếu lời mời được hủy thành công; False nếu không tìm thấy lời mời hợp lệ để hủy.
        /// </returns>
        public async Task<bool> CancelFriendRequestAsync(int currentUserId, int targetUserId)
        {
            // BƯỚC 1: Tìm lời mời kết bạn từ currentUserId đến targetUserId
            // Điều kiện: user hiện tại là người gửi (UserId1), người nhận là target (UserId2),
            // và trạng thái lời mời vẫn là Pending (StatusId = 3)
            var existing = await _dbContext.Friendships
                .FirstOrDefaultAsync(f =>
                    f.UserId1 == currentUserId &&
                    f.UserId2 == targetUserId &&
                    f.StatusId == 1); // 3 = Pending

            // BƯỚC 2: Nếu không tồn tại lời mời hợp lệ => không thể hủy => trả về false
            if (existing == null)
                return false;

            // BƯỚC 3: Nếu tồn tại, tiến hành hủy bằng cách xoá bản ghi Friendship
            _dbContext.Friendships.Remove(existing);

            // BƯỚC 4: Ghi thay đổi vào database
            await _dbContext.SaveChangesAsync();

            // BƯỚC 5: Trả về true để xác nhận đã hủy thành công
            return true;
        }

        // ---------------------------------------------
        // Author : A–DUY
        // Date   : 2025-06-04
        // Task   : Chấp nhận lời mời kết bạn được gửi từ requesterId đến currentUserId
        // ---------------------------------------------
        /// <summary>
        /// 1) Tìm record lời mời kết bạn có trạng thái Pending (StatusId = 3),
        ///    với requesterId là người gửi (UserId1), và currentUserId là người nhận (UserId2).
        /// 2) Nếu không tồn tại record hợp lệ => trả về false.
        /// 3) Nếu tồn tại, cập nhật StatusId = 4 (Accepted).
        /// 4) Gọi SaveChangesAsync để lưu thay đổi vào database.
        /// 5) Trả về true nếu chấp nhận lời mời thành công.
        /// </summary>
        /// <param name="currentUserId">ID của user hiện tại, người nhận lời mời và muốn chấp nhận.</param>
        /// <param name="requesterId">ID của người đã gửi lời mời kết bạn.</param>
        /// <returns>
        ///     True nếu lời mời được chấp nhận thành công; False nếu không tìm thấy lời mời hợp lệ.
        /// </returns>
        public async Task<bool> AcceptFriendRequestAsync(int currentUserId, int requesterId)
        {
            // BƯỚC 1: Tìm lời mời kết bạn từ requesterId đến currentUserId với trạng thái Pending
            // Điều kiện: requester là người gửi (UserId1), currentUser là người nhận (UserId2)
            var existing = await _dbContext.Friendships
                .FirstOrDefaultAsync(f =>
                    f.UserId1 == requesterId &&
                    f.UserId2 == currentUserId &&
                    f.StatusId == 1); // 3 = Pending

            // BƯỚC 2: Nếu không tìm thấy lời mời => không thể chấp nhận => trả false
            if (existing == null)
                return false;

            // BƯỚC 3: Cập nhật trạng thái thành Accepted (StatusId = 2)
            existing.StatusId = 2;

            // GỢI Ý: Nếu hệ thống có cột RespondedAt (thời điểm phản hồi), có thể cập nhật tại đây:
            // existing.RespondedAt = DateTime.UtcNow;

            // BƯỚC 4: Lưu thay đổi vào database
            await _dbContext.SaveChangesAsync();

            // BƯỚC 5: Trả về true để xác nhận đã chấp nhận lời mời
            return true;
        }

        // ---------------------------------------------
        // Author : A–DUY
        // Date   : 2025-06-04
        // Task   : Từ chối lời mời kết bạn từ requesterId đến currentUserId
        // ---------------------------------------------
        /// <summary>
        /// 1) Tìm record lời mời kết bạn có trạng thái Pending (StatusId = 3),
        ///    với requesterId là người gửi (UserId1), currentUserId là người nhận (UserId2).
        /// 2) Nếu không tồn tại lời mời hợp lệ => trả về false.
        /// 3) Nếu tồn tại, cập nhật trạng thái của lời mời thành Rejected (StatusId = 5).
        /// 4) Gọi SaveChangesAsync để lưu thay đổi vào database.
        /// 5) Trả về true nếu từ chối thành công.
        /// </summary>
        /// <param name="currentUserId">ID của user hiện tại, người nhận lời mời.</param>
        /// <param name="requesterId">ID của người gửi lời mời kết bạn.</param>
        /// <returns>
        ///     True nếu lời mời bị từ chối thành công; False nếu không có lời mời phù hợp để từ chối.
        /// </returns>
        public async Task<bool> RejectFriendRequestAsync(int currentUserId, int requesterId)
        {
            // BƯỚC 1: Tìm lời mời kết bạn từ requesterId đến currentUserId với trạng thái Pending
            var existing = await _dbContext.Friendships
                .FirstOrDefaultAsync(f =>
                    f.UserId1 == requesterId &&
                    f.UserId2 == currentUserId &&
                    f.StatusId == 1); // 3 = Pending

            // BƯỚC 2: Nếu không tồn tại record hợp lệ => không thể từ chối => trả false
            if (existing == null)
                return false;

            // BƯỚC 3: Cập nhật trạng thái thành Rejected (StatusId = 5)
            existing.StatusId = 3;

            // GỢI Ý: Nếu hệ thống có cột thời điểm phản hồi (RespondedAt), cập nhật tại đây:
            // existing.RespondedAt = DateTime.UtcNow;

            // BƯỚC 4: Lưu thay đổi vào database
            await _dbContext.SaveChangesAsync();

            // BƯỚC 5: Trả về true để xác nhận đã từ chối thành công
            return true;
        }

        // ---------------------------------------------
        // Author : A–DUY
        // Date   : 2025-06-04
        // Task   : Xóa mối quan hệ bạn bè giữa currentUserId và friendUserId khi đã là bạn bè (Accepted)
        // ---------------------------------------------
        /// <summary>
        /// 1) Tìm record Friendship giữa currentUserId và friendUserId có trạng thái Accepted (StatusId = 2).
        ///    – Không phân biệt ai là người gửi/nhận lời mời ban đầu.
        /// 2) Nếu không tồn tại => trả về false.
        /// 3) Nếu tồn tại, xoá record khỏi DbContext.
        /// 4) Lưu thay đổi vào database.
        /// 5) Trả về true nếu xóa bạn thành công.
        /// </summary>
        /// <param name="currentUserId">ID của user hiện tại, người muốn xoá bạn.</param>
        /// <param name="friendUserId">ID của người bạn bị xoá khỏi danh sách bạn bè.</param>
        /// <returns>
        ///     True nếu mối quan hệ được xoá thành công; False nếu không có quan hệ bạn bè tồn tại.
        /// </returns>
        public async Task<bool> RemoveFriendAsync(int currentUserId, int friendUserId)
        {
            // BƯỚC 1: Tìm mối quan hệ bạn bè giữa currentUser và friendUser với trạng thái đã chấp nhận (Accepted)
            // Lưu ý: Quan hệ bạn bè là hai chiều nên cần kiểm tra cả hai chiều UserId1/UserId2
            var existing = await _dbContext.Friendships
                .FirstOrDefaultAsync(f =>
                    ((f.UserId1 == currentUserId && f.UserId2 == friendUserId) ||
                     (f.UserId1 == friendUserId && f.UserId2 == currentUserId)) &&
                    f.StatusId == 2); // 2 = Accepted

            // BƯỚC 2: Nếu không tồn tại quan hệ bạn bè => không thể xoá => trả về false
            if (existing == null)
                return false;

            // BƯỚC 3: Xoá record Friendship đã tìm thấy
            _dbContext.Friendships.Remove(existing);

            // BƯỚC 4: Lưu thay đổi vào database
            await _dbContext.SaveChangesAsync();

            // BƯỚC 5: Trả về true để xác nhận xoá bạn thành công
            return true;
        }

        // ---------------------------------------------
        // Author : A–DUY
        // Date   : 2025-05-31
        // Task   : Lấy danh sách bạn bè đã chấp nhận của user hiện tại
        // ---------------------------------------------
        /// <summary>
        /// Trả về danh sách User đã là bạn (status = Accepted) với currentUserId.
        /// 1) Tìm tất cả record trong bảng Friendship có statusId == 2 (Accepted)
        ///    và một trong hai UserId1 hoặc UserId2 bằng currentUserId.
        /// 2) Dựa vào mỗi record đó, lấy ra Id của người còn lại (friendId).
        /// 3) Truy vấn bảng Users để trả về tất cả User có UserId nằm trong danh sách friendIds.
        /// </summary>
        /// <param name="currentUserId">
        ///    ID của user đang đăng nhập, muốn lấy danh sách bạn bè của họ.
        /// </param>
        /// <returns>
        ///     <see cref="IEnumerable{User}"/> chứa tất cả <see cref="User"/> đã chấp nhận kết bạn với currentUserId.
        /// </returns>
        public async Task<IEnumerable<User>> GetFriendsAsync(int currentUserId)
        {
            // BƯỚC 1: Truy vấn bảng Friendship để lấy các record đã chấp nhận (statusId =2)
            //        và liên quan đến currentUserId (có thể là cột UserId1 hoặc UserId2).
            // - f.StatusId == 2: lọc những quan hệ đã "Accepted".
            // - (f.UserId1 == currentUserId || f.UserId2 == currentUserId): 
            //   tìm cả trường hợp currentUserId ở cột UserId1 hoặc cột UserId2.
            var friends = await _dbContext.Friendships
                .Where(f =>
                    f.StatusId == 2 && // Accepted
                    (f.UserId1 == currentUserId || f.UserId2 == currentUserId))
                .ToListAsync();
            // ToListAsync() thực thi truy vấn và đưa kết quả về danh sách Friendship

            // BƯỚC 2: Từ mỗi bản ghi Friendship đã lấy, xác định userId của "người bạn"
            // Nếu currentUserId đang ở cột UserId1, thì friendId là f.UserId2.
            // Ngược lại, nếu currentUserId ở cột UserId2, thì friendId là f.UserId1.
            var friendIds = friends.Select(f =>
                (f.UserId1 == currentUserId) ? f.UserId2 : f.UserId1);

            // BƯỚC 3: Truy vấn bảng Users để lấy đối tượng User với UserId thuộc friendIds.
            //    - Contains(u.UserId): đảm bảo chỉ lấy những User đã chấp nhận kết bạn.
            //    - Kết quả trả về là List<User> chứa đối tượng User của từng friend.
            return await _dbContext.Users
                .Where(u => friendIds.Contains(u.UserId))
                .ToListAsync();
        }

        /// <summary>
        /// Truy vấn tất cả lời mời kết bạn mà currentUser đang là người nhận (Addressee),
        /// với trạng thái Pending (StatusId = 3).
        /// Kết quả trả về dưới dạng IEnumerable"FriendRequestInfo" để đưa lên View.
        /// </summary>
        /// <param name="currentUserId">
        ///     ID của user hiện tại, sẽ được khớp với cột UserId2 trong bảng Friendship,
        ///     tức currentUser nhận lời mời từ UserId1 (Requester).
        /// </param>
        /// <returns>
        ///     IEnumerable"FriendRequestInfo" bao gồm thông tin:
        ///       - FriendshipId: khóa chính của record Friendship
        ///       - RequesterId, RequesterName: user gửi lời mời
        ///       - AddresseeId, AddresseeName: currentUser (người nhận) – chỉ để hiển thị lại, hiếm khi dùng
        ///       - StatusId, StatusName: trạng thái hiện tại (Pending)
        ///       - RequestedAt: thời điểm tạo lời mời
        /// </returns>
        public async Task<IEnumerable<FriendRequestInfo>> GetIncomingRequestsAsync(int currentUserId)
        {
            // ──────────────────────────────────────────────────────────────────
            // BƯỚC 1: Truy vấn bảng Friendship để lấy các record "Pending" mà currentUser là Addressee
            // ──────────────────────────────────────────────────────────────────
            // .Include(f => f.Requester): Đảm bảo EF tải luôn đối tượng User tương ứng với UserId1
            // .Include(f => f.Addressee): Để có thể lấy tên của user nhận (mặc dù thường không cần hiển thị ở incoming)
            // .Include(f => f.Status): Bảng Status chứa thông tin status.Name (ví dụ "Pending")
            // .Where(...): 
            //    + f.UserId2 == currentUserId: currentUser chính là Addressee
            //    + f.StatusId  == 3            : Chỉ lấy những lời mời đang ở trạng thái Pending (3)
            //
            // Kết quả sẽ trả về List<Friendship> đã tải sẵn cả Requester, Addressee và Status để mapping tiếp.
            var incoming = await _dbContext.Friendships
                .Include(f => f.Requester) // tải thông tin user gửi (Requester)
                .Include(f => f.Addressee) // tải thông tin user nhận (Addressee)
                .Include(f => f.Status) // tải thông tin status để lấy status.StatuName
                .Where(f => f.UserId2 == currentUserId && f.StatusId == 1)
                .ToListAsync();

            // ──────────────────────────────────────────────────────────────────
            // BƯỚC 2: Chuyển đổi (mapping) từ entity Friendship sang ViewModel FriendRequestInfo
            // ──────────────────────────────────────────────────────────────────
            // Duyệt qua mỗi record 'f' trong incoming, tạo một FriendRequestInfo chứa:
            //   • FriendshipId  : Khóa chính của record, cần khi Accept/Reject/Cancel
            //   • RequesterId   : f.UserId1 (ID của người gửi lời mời)
            //   • RequesterName : f.Requester.FullName nếu có, ngược lại f.Requester.Email
            //   • AddresseeId   : f.UserId2 (ID của currentUser)
            //   • AddresseeName : f.Addressee.FullName nếu có, ngược lại f.Addressee.Email
            //   • StatusId      : f.StatusId (3 = Pending)
            //   • StatusName    : f.Status.StatuName (ví dụ "Pending")
            //   • RequestedAt   : f.CreatedAt (ngày giờ lời mời được tạo)
            return incoming.Select(f => new FriendRequestInfo
            {
                FriendshipId = f.FriendshipId,
                RequesterId = f.UserId1,

                // Toán tử "??" (null-coalescing) có ý nghĩa: 
                //   Nếu biểu thức bên trái (f.Requester.FullName) khác null, 
                //   thì kết quả lấy giá trị bên trái; 
                //   nếu bằng null, thì lấy giá trị bên phải (f.Requester.Email).
                // Ví dụ:
                //   f.Requester.FullName = "Nguyễn Văn A"  -> kết quả là "Nguyễn Văn A"
                //   f.Requester.FullName = null             -> kết quả là f.Requester.Email

                // Ưu tiên sử dụng tên đầy đủ (FullName) nếu user đã khai báo, ngược lại dùng Email để hiển thị
                RequesterName = f.Requester.FullName ?? f.Requester.Email,
                AddresseeId = f.UserId2,
                // AddresseeName thường là currentUser; hiển thị để đảm bảo consistency nếu cần log hoặc debug
                AddresseeName = f.Addressee.FullName ?? f.Addressee.Email,
                StatusId = f.StatusId,
                // f.Status.StatuName là tên trạng thái, ví dụ "Pending"; cần hiển thị ra view
                StatusName = f.Status.StatuName,
                RequestedAt = f.CreatedAt
            });
        }

        /// <summary>
        /// Trả về danh sách các lời mời kết bạn mà currentUser đã gửi đi,
        /// với trạng thái Pending (StatusId = 3).
        /// 1) Truy vấn bảng Friendship để lấy tất cả bản ghi mà:
        ///    - UserId1 == currentUserId (currentUser là người gửi Requester)
        ///    - StatusId == 3 (Pending)
        ///    - Đồng thời dùng Include để load luôn đối tượng Requester, Addressee và Status.
        /// 2) Chuyển (map) mỗi record thành ViewModel FriendRequestInfo để hiển thị lên View:
        ///    - FriendshipId, RequesterId, RequesterName, AddresseeId, AddresseeName, StatusId, StatusName, RequestedAt
        /// 3) Trả về IEnumerable<FriendRequestInfo> cho controller hoặc view sử dụng.
        /// </summary>
        /// <param name="currentUserId">
        ///     ID của user hiện tại (người gửi lời mời).
        /// </param>
        /// <returns>
        ///     Tập các FriendRequestInfo tương ứng với lời mời đang chờ mà currentUser đã gửi.
        /// </returns>
        public async Task<IEnumerable<FriendRequestInfo>> GetOutgoingRequestsAsync(int currentUserId)
        {
            // ──────────────────────────────────────────────────────────────────
            // BƯỚC 1: Truy vấn bảng Friendship để lấy record Pending mà currentUser là Requester
            // ──────────────────────────────────────────────────────────────────
            // Include(f => f.Requester)   : tải thông tin user gửi (Requester)
            // Include(f => f.Addressee)   : tải thông tin user nhận (Addressee)
            // Include(f => f.Status)      : tải thông tin status để lấy status.StatuName
            // Where(f => f.UserId1 == currentUserId && f.StatusId == 3): 
            //   - currentUserId phải khớp với cột UserId1 (Requester)
            //   - StatusId == 3 (Pending)
            // Kết quả ToListAsync() là một List<Friendship> đã load sẵn navigation properties.
            var outgoing = await _dbContext.Friendships
                .Include(f => f.Requester)
                .Include(f => f.Addressee)
                .Include(f => f.Status)
                .Where(f => f.UserId1 == currentUserId && f.StatusId == 1)
                .ToListAsync();

            // ──────────────────────────────────────────────────────────────────
            // BƯỚC 2: Mapping từ entity Friendship sang ViewModel FriendRequestInfo
            // ──────────────────────────────────────────────────────────────────
            // Duyệt qua mỗi record 'f' trong outgoing, tạo FriendRequestInfo với các trường:
            //   • FriendshipId  : Khóa chính của record, cần để Cancel request
            //   • RequesterId   : f.UserId1 (ID của người gửi, currentUserId)
            //   • RequesterName : f.Requester.FullName nếu khác null, ngược lại f.Requester.Email
            //   • AddresseeId   : f.UserId2 (ID của người nhận)
            //   • AddresseeName : f.Addressee.FullName nếu khác null, ngược lại f.Addressee.Email
            //   • StatusId      : f.StatusId (3 = Pending)
            //   • StatusName    : f.Status.StatuName (ví dụ "Pending")
            //   • RequestedAt   : f.CreatedAt (thời điểm tạo lời mời)
            return outgoing.Select(f => new FriendRequestInfo
            {
                FriendshipId = f.FriendshipId,
                RequesterId = f.UserId1,
                // Nếu FullName được lưu, dùng; nếu không, fallback sang Email
                RequesterName = f.Requester.FullName ?? f.Requester.Email,
                AddresseeId = f.UserId2,
                // Nếu FullName của Addressee tồn tại, dùng; nếu không, dùng Email
                AddresseeName = f.Addressee.FullName ?? f.Addressee.Email,
                StatusId = f.StatusId,
                // StatusName lấy từ bảng Status để view hiển thị rõ trạng thái
                StatusName = f.Status.StatuName,
                RequestedAt = f.CreatedAt
            });
        }

        // ---------------------------------------------
        // Author : A–DUY
        // Date   : 2025-05-31
        // Task   : Lấy danh sách tất cả user trừ user hiện tại, kèm trạng thái quan hệ bạn bè
        // ---------------------------------------------
        /// <summary>
        /// Lấy danh sách tất cả user (ngoại trừ user hiện tại) kèm trạng thái quan hệ bạn bè.
        /// Logic:
        /// 1) Truy vấn tất cả user trong bảng Users mà UserId != currentUserId.
        /// 2) Truy vấn tất cả record trong bảng Friendship có liên quan đến currentUserId
        ///    (dưới vai trò Requester hoặc Addressee) để biết những mối quan hệ đã tồn tại.
        /// 3) Với mỗi user trong allUsers, kiểm tra xem có record quan hệ nào trong `related` không:
        ///      • Nếu không có, StatusId = 0 (None), Role = FriendshipRole.None.
        ///      • Nếu có và StatusId == 1 (Pending):
        ///          - Nếu currentUserId là UserId1 (đã gửi), gán Role = PendingSent.
        ///          - Nếu currentUserId là UserId2 (đã nhận), gán Role = PendingReceived.
        ///      • Nếu có và StatusId == 2 (Accepted), gán Role = FriendshipRole.Accepted.
        /// 4) Tạo đối tượng UserWithFriendshipStatus chứa:
        ///      - UserId, UserName (FullName hoặc Email nếu FullName null),
        ///      - StatusId (nếu record null thì 0),
        ///      - Role (xác định vai trò PendingSent, PendingReceived, Accepted, hoặc None).
        /// 5) Trả về danh sách các UserWithFriendshipStatus để Controller hoặc View sử dụng.
        /// </summary>
        /// <param name="currentUserId">ID của user hiện tại, sẽ không hiển thị trong danh sách trả về.</param>
        /// <returns>
        ///     IEnumerable<UserWithFriendshipStatus/> chứa từng user còn lại và trạng thái quan hệ tương ứng.
        /// </returns>
        public async Task<IEnumerable<UserWithFriendshipStatus>> GetAllUsersExceptCurrentAsync(int currentUserId)
        {
            // BƯỚC 1: Truy vấn bảng Users để lấy toàn bộ user, loại bỏ currentUser
            // - .Where(u => u.UserId != currentUserId): lọc ra tất cả user mà UserId khác currentUserId.
            // - ToListAsync(): thực thi truy vấn lên database, trả về List<User> chứa tất cả user còn lại.
            var allUsers = await _dbContext.Users
                .Where(u => u.UserId != currentUserId)
                .ToListAsync();
            // Ở đây, allUsers chứa các đối tượng User, ví dụ: [User(Id=2), User(Id=3), User(Id=4), ...]

            // BƯỚC 2: Truy vấn bảng Friendship để lấy tất cả record liên quan đến currentUserId
            // - Điều kiện f.UserId1 == currentUserId || f.UserId2 == currentUserId:
            //   + Nếu currentUserId ở cột UserId1: currentUser đóng vai Requester.
            //   + Nếu currentUserId ở cột UserId2: currentUser đóng vai Addressee.
            // - ToListAsync(): lấy về danh sách Friendship chứa các record đó.
            var related = await _dbContext.Friendships
                .Where(f => f.UserId1 == currentUserId || f.UserId2 == currentUserId)
                .ToListAsync();
            // Ví dụ, nếu Friendship có (UserId1=1, UserId2=2) và currentUserId=1,
            // record này được đưa vào `related`. Tương tự với (UserId1=3, UserId2=1),...

            // Khởi tạo danh sách ViewModel để lưu kết quả cuối cùng
            var result = new List<UserWithFriendshipStatus>();

            // BƯỚC 3: Duyệt từng user trong allUsers để xác định trạng thái quan hệ
            foreach (var user in allUsers)
            {
                // Tìm record Friendship giữa currentUser và user này (nếu tồn tại)
                // - Nếu record.UserId1 == currentUserId && record.UserId2 == user.UserId:
                //    + currentUser đã gửi lời mời đến user (Requester = currentUser).
                // - Nếu record.UserId1 == user.UserId && record.UserId2 == currentUserId:
                //    + user đã gửi lời mời đến currentUser (Requester = user).
                var record = related.FirstOrDefault(f =>
                    (f.UserId1 == currentUserId && f.UserId2 == user.UserId) ||
                    (f.UserId1 == user.UserId && f.UserId2 == currentUserId));

                // Khởi tạo ViewModel chứa dữ liệu cơ bản cho user này
                // - UserId: ID gốc của user
                // - UserName: Nếu có FullName thì dùng, ngược lại hiển thị Email làm fallback
                // - StatusId: Nếu không có record (record == null), gán 0 (None); nếu có, lấy record.StatusId
                // - Role: Mặc định gán FriendshipRole.None, sẽ cập nhật bên dưới nếu record tồn tại
                var vm = new UserWithFriendshipStatus
                {
                    UserId = user.UserId,
                    UserName = user.FullName ?? user.Email,
                    StatusId = record?.StatusId ?? 0,
                    Role = FriendshipRole.None
                };

                // Nếu tồn tại record Friendship giữa currentUser và user
                if (record != null)
                {
                    // Kiểm tra trạng thái Pending (StatusId = 3)
                    if (record.StatusId == 1)
                    {
                        // Nếu currentUserId khớp record.UserId1 => currentUser đã gửi lời mời (PendingSent)
                        if (record.UserId1 == currentUserId)
                            vm.Role = FriendshipRole.PendingSent;
                        else
                            // Ngược lại, currentUserId == record.UserId2 => user đã gửi đến currentUser (PendingReceived)
                            vm.Role = FriendshipRole.PendingReceived;
                    }
                    // Kiểm tra trạng thái Accepted (StatusId = 2)
                    else if (record.StatusId == 2)
                    {
                        // Hai người đã chấp nhận kết bạn
                        vm.Role = FriendshipRole.Accepted;
                    }
                    // Bạn có thể thêm xử lý cho các StatusId khác (ví dụ Removed = 5, Cancelled = 6) nếu cần
                }

                // Thêm ViewModel đã gán xong trạng thái vào kết quả
                result.Add(vm);
            }

            // BƯỚC 4: Trả về danh sách ViewModel hoàn thiện
            // IEnumerable<UserWithFriendshipStatus> sẽ được controller nhận và đẩy lên View
            return result;
        }
    }
}