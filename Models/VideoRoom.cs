/*
 * File:    VideoRoom.cs
 * Author:  A-DUY
 * Date:    2025-05-31
 * Desc:    Lớp đại diện cho phòng video call (VideoRoom).
 *          Mô tả chi tiết cách EF Core ánh xạ giữa model và database,
 *          giải thích từng cột trong bảng VideoRooms và các navigation properties liên quan.
 */

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Zela.Models;

/// <summary>
/// Phòng video call.
/// </summary>
public class VideoRoom
{
    /// <summary>
    /// Khóa chính (Primary Key) của bảng VideoRooms.
    /// Đây là cột Identity trong database với kiểu INT IDENTITY(1,1).
    /// EF Core sử dụng [Key] để đánh dấu RoomId là PK và tự động tạo cột VideoRooms.RoomId.
    /// </summary>
    [Key]
    public int RoomId { get; set; } // PK: INT IDENTITY(1,1)

    /// <summary>
    /// Khóa ngoại (FK) liên kết đến User tạo phòng.
    /// Trong database, đây là cột CreatorId (INT).
    /// EF Core sẽ tạo ràng buộc FOREIGN KEY: VideoRooms.CreatorId → Users.UserId.
    /// Nếu thêm [Required], CreatorId sẽ không cho phép null; hiện tại có thể để null khi không khởi tạo.
    /// </summary>
    public int CreatorId { get; set; }

    /// <summary>
    /// Trạng thái phòng có đang mở (IsOpen) hay không.
    /// Ánh xạ thành cột IsOpen (BIT) trong database.
    /// TRUE nghĩa là phòng đang có thể tham gia, FALSE nghĩa là phòng đã đóng.
    /// </summary>
    public bool IsOpen { get; set; }

    /// <summary>
    /// Ngày giờ tạo phòng.
    /// Ánh xạ thành cột CreatedAt (DATETIME2) trong database.
    /// Lưu thông tin chính xác đến phần thập phân của giây.
    /// </summary>
    public DateTime CreatedAt { get; set; } // DATETIME2

    /// <summary>
    /// Tên của phòng video call, tối đa 50 ký tự.
    /// Ánh xạ thành cột Name (NVARCHAR(50)) trong database.
    /// Có thể null nếu không đặt tên phòng.
    /// </summary>
    [MaxLength(50)]
    public string Name { get; set; }

    /// <summary>
    /// Mật khẩu (nếu có) để tham gia phòng, tối đa 50 ký tự.
    /// Ánh xạ thành cột Password (NVARCHAR(50)) trong database.
    /// Nếu null thì phòng không yêu cầu mật khẩu.
    /// </summary>
    [Required, MaxLength(10)]
    public string Password { get; set; } // NVARCHAR(50) NULL

    /*
     * ====== Navigation Properties ======
     * Các thuộc tính dưới đây biểu thị quan hệ giữa VideoRoom và các thực thể khác.
     * Chúng KHÔNG tương ứng trực tiếp thành cột trong bảng VideoRooms,
     * mà EF Core sử dụng để thiết lập mối quan hệ (1 VideoRoom có nhiều RoomParticipant và nhiều CallSession)
     * dựa trên khóa ngoại ở phía bảng con (RoomParticipant.CreatorId? hoặc RoomParticipant.RoomId, v.v.).
     *
     * Khi migration được tạo, EF Core sẽ không tạo thêm cột nào cho những thuộc tính này,
     * mà chỉ tạo ràng buộc FOREIGN KEY trong các bảng RoomParticipant và CallSession trỏ về VideoRooms.RoomId.
     */

    /// <summary>
    /// Navigation property: User (Creator) đã tạo phòng.
    /// EF sử dụng [ForeignKey(nameof(CreatorId))] để biết cột CreatorId là khóa ngoại.
    /// Không tạo cột Creator trong database; chỉ liên kết đến bảng Users.
    /// </summary>
    [ForeignKey(nameof(CreatorId))]
    public User Creator { get; set; }

    /// <summary>
    /// Danh sách người tham gia (RoomParticipant) trong phòng này.
    /// Quan hệ một VideoRoom có thể có nhiều RoomParticipant (1:N).
    /// Bảng RoomParticipant sẽ có cột RoomId làm FK trỏ về VideoRooms.RoomId.
    /// </summary>
    public ICollection<RoomParticipant> Participants { get; set; }

    /// <summary>
    /// Danh sách các phiên gọi (CallSession) phát sinh trong phòng này.
    /// Quan hệ một VideoRoom có thể có nhiều CallSession (1:N).
    /// Bảng CallSession sẽ có cột RoomId làm FK trỏ về VideoRooms.RoomId.
    /// </summary>
    public ICollection<CallSession> CallSessions { get; set; }
}

/*
 * ====== Ghi chú tổng quát ======
 * 1. Trong database, bảng VideoRooms sẽ chỉ có các cột:
 *      RoomId     INT IDENTITY(1,1)  -- Primary Key
 *      CreatorId  INT NOT NULL       -- Foreign Key trỏ về Users.UserId
 *      IsOpen     BIT NOT NULL       -- Trạng thái phòng đang mở/đóng
 *      CreatedAt  DATETIME2 NOT NULL  -- Ngày giờ tạo phòng
 *      Name       NVARCHAR(50) NULL   -- Tên phòng
 *      Password   NVARCHAR(50) NULL   -- Mật khẩu phòng (nếu có)
 *
 * 2. Ràng buộc dựa trên annotation:
 *      [Key]            → RoomId là Primary Key.
 *      [ForeignKey]     → CreatorId liên kết tới Users.UserId.
 *      [MaxLength(50)]  → Giới hạn độ dài chuỗi Name và Password.
 *
 * 3. Navigation properties không sinh cột trong bảng VideoRooms:
 *    - EF Core xác định mối quan hệ 1 User (Creator) — N VideoRoom dựa trên CreatorId.
 *    - EF Core xác định mối quan hệ 1 VideoRoom — N RoomParticipant dựa trên RoomParticipant.RoomId.
 *    - EF Core xác định mối quan hệ 1 VideoRoom — N CallSession dựa trên CallSession.RoomId.
 *
 * 4. Khi query và include:
 *      var room = await _dbContext.VideoRooms
 *                       .Include(r => r.Creator)
 *                       .Include(r => r.Participants)
 *                       .Include(r => r.CallSessions)
 *                       .FirstOrDefaultAsync(r => r.RoomId == someRoomId);
 *    → EF Core sẽ sinh SQL JOIN:
 *      SELECT vr.*, u.*, rp.*, cs.*
 *      FROM VideoRooms AS vr
 *      LEFT JOIN Users AS u ON vr.CreatorId = u.UserId
 *      LEFT JOIN RoomParticipants AS rp ON vr.RoomId = rp.RoomId
 *      LEFT JOIN CallSessions AS cs ON vr.RoomId = cs.RoomId
 *    → Kết quả trả về, room.Creator, room.Participants và room.CallSessions đều đã có dữ liệu tương ứng.
 *
 * 5. Khi thêm một VideoRoom mới:
 *      var user = await _dbContext.Users.FindAsync(creatorId);
 *      var newRoom = new VideoRoom {
 *          Creator   = user,              // EF tự động set user.UserId → CreatorId
 *          IsOpen    = true,
 *          CreatedAt = DateTime.Now,
 *          Name      = "Học lập trình",
 *          Password  = "123456"
 *      };
 *      _dbContext.VideoRooms.Add(newRoom);
 *      await _dbContext.SaveChangesAsync();
 *    → EF Core sẽ INSERT vào VideoRooms(CreatorId, IsOpen, CreatedAt, Name, Password)
 *       với CreatorId tự động lấy từ user.UserId.
 *
 * 6. Nếu không có navigation property:
 *    - Bạn vẫn có thể query VideoRoom, nhưng để lấy thông tin Creator, Participants, CallSessions,
 *      bạn phải tự join hoặc query riêng:
 *          var room = _dbContext.VideoRooms.Find(roomId);
 *          var creator = _dbContext.Users.Find(room.CreatorId);
 *          var parts = _dbContext.RoomParticipants.Where(rp => rp.RoomId == room.RoomId).ToList();
 *          var sessions = _dbContext.CallSessions.Where(cs => cs.RoomId == room.RoomId).ToList();
 *    - Cách này kém hiệu quả và dài dòng hơn so với chỉ dùng room.Creator, room.Participants, v.v.
 *
 * 7. Tóm lại:
 *    - RoomId, CreatorId, IsOpen, CreatedAt, Name, Password tương ứng trực tiếp thành cột trong bảng VideoRooms.
 *    - Các navigation properties (Creator, Participants, CallSessions) giúp EF Core nắm được quan hệ
 *      giữa VideoRoom và các bảng liên quan, hỗ trợ truy xuất dữ liệu một cách thuận tiện trên tầng C#.
 *
 */