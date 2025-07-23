using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zela.DbContext;
using Zela.Models;

namespace Zela.Controllers;

[Authorize(Roles = "Admin")] // Chỉ cho phép người dùng có Role = "Admin"
public class AdminController : Controller
{
    private readonly ApplicationDbContext _db;

    public AdminController(ApplicationDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Trang dashboard: thống kê nhanh.
    /// </summary>
    public async Task<IActionResult> Index()
    {
        // Số liệu động
        var now = DateTime.UtcNow;
        var today = now.Date;
        var weekAgo = now.AddDays(-7);
        var monthAgo = now.AddMonths(-1);

        // Tổng số user
        ViewBag.UserCount = await _db.Users.CountAsync();
        // User online (LastLoginAt trong 5 phút gần nhất)
        ViewBag.UserOnline = await _db.Users.CountAsync(u => u.LastLoginAt > now.AddMinutes(-5));
        // User mới hôm nay/tuần/tháng
        ViewBag.UserNewToday = await _db.Users.CountAsync(u => u.CreatedAt >= today);
        ViewBag.UserNewWeek = await _db.Users.CountAsync(u => u.CreatedAt >= weekAgo);
        ViewBag.UserNewMonth = await _db.Users.CountAsync(u => u.CreatedAt >= monthAgo);

        // Tổng số nhóm chat
        ViewBag.GroupCount = await _db.ChatGroups.CountAsync();
        // Tổng số phòng họp
        ViewBag.RoomCount = await _db.VideoRooms.CountAsync();
        // Tổng số sticker
        ViewBag.StickerCount = await _db.Stickers.CountAsync();
        // Tổng số tin nhắn
        ViewBag.MessageCount = await _db.Messages.CountAsync();

        // Tổng doanh thu
        ViewBag.TotalRevenue = await _db.PaymentTransactions.Where(t => t.Status == "Success").SumAsync(t => (decimal?)t.Amount) ?? 0;
        // Doanh thu hôm nay/tuần/tháng
        ViewBag.RevenueToday = await _db.PaymentTransactions.Where(t => t.Status == "Success" && t.CreatedAt >= today).SumAsync(t => (decimal?)t.Amount) ?? 0;
        ViewBag.RevenueWeek = await _db.PaymentTransactions.Where(t => t.Status == "Success" && t.CreatedAt >= weekAgo).SumAsync(t => (decimal?)t.Amount) ?? 0;
        ViewBag.RevenueMonth = await _db.PaymentTransactions.Where(t => t.Status == "Success" && t.CreatedAt >= monthAgo).SumAsync(t => (decimal?)t.Amount) ?? 0;

        // Tổng số file và dung lượng
        ViewBag.FileCount = await _db.Recordings.CountAsync();
        ViewBag.FileSize = await _db.Recordings.SumAsync(r => (long?)r.FileSize) ?? 0;

        // Recent activity - sử dụng một class chung để tránh lỗi binding
        var recentActivity = new List<object>();
        
        // Recent Users
        var recentUsers = await _db.Users.OrderByDescending(u => u.CreatedAt).Take(3).ToListAsync();
        foreach (var user in recentUsers)
        {
            recentActivity.Add(new { 
                Type = "User", 
                Title = user.Email, 
                Description = user.FullName,
                CreatedAt = user.CreatedAt,
                Icon = "bi-person-plus"
            });
        }

        // Recent Groups
        var recentGroups = await _db.ChatGroups.OrderByDescending(g => g.CreatedAt).Take(3).ToListAsync();
        foreach (var group in recentGroups)
        {
            recentActivity.Add(new { 
                Type = "Group", 
                Title = group.Name, 
                Description = "Nhóm chat mới",
                CreatedAt = group.CreatedAt,
                Icon = "bi-people"
            });
        }

        // Recent Rooms
        var recentRooms = await _db.VideoRooms.OrderByDescending(r => r.CreatedAt).Take(3).ToListAsync();
        foreach (var room in recentRooms)
        {
            recentActivity.Add(new { 
                Type = "Room", 
                Title = room.Name, 
                Description = "Phòng họp mới",
                CreatedAt = room.CreatedAt,
                Icon = "bi-camera-video"
            });
        }

        // Recent Payments
        var recentPayments = await _db.PaymentTransactions.Where(t => t.Status == "Success").Include(t => t.User).OrderByDescending(t => t.CreatedAt).Take(3).ToListAsync();
        foreach (var payment in recentPayments)
        {
            recentActivity.Add(new { 
                Type = "Payment", 
                Title = $"{payment.Amount:N0} VNĐ", 
                Description = payment.User?.Email ?? "Unknown",
                CreatedAt = payment.CreatedAt,
                Icon = "bi-credit-card"
            });
        }

        // Sort by CreatedAt and take top 10
        ViewBag.RecentActivity = recentActivity.OrderByDescending(x => ((dynamic)x).CreatedAt).Take(10).ToList();

        // System status (mockup, có thể mở rộng kiểm tra thực tế)
        ViewBag.SystemStatus = new[] {
            new { Name = "Database Server", Status = "Online", Level = "success" },
            new { Name = "SignalR Hub", Status = "Hoạt động", Level = "success" },
            new { Name = "File Upload Service", Status = "Bình thường", Level = "success" },
            new { Name = "Email Service", Status = "Chậm", Level = "warning" }
        };

        // Quick reports
        ViewBag.TopUsers = await _db.Users.OrderByDescending(u => u.LastLoginAt).Take(5).ToListAsync();
        ViewBag.TopGroups = await _db.ChatGroups.Include(g => g.Members).OrderByDescending(g => g.Members.Count).Take(5).ToListAsync();
        ViewBag.TopQuiz = await _db.Quizzes.Include(q => q.Attempts).OrderByDescending(q => q.Attempts.Count).Take(5).ToListAsync();

        return View();
    }

    /// <summary>
    /// Danh sách người dùng kèm role.
    /// </summary>
    public async Task<IActionResult> Users()
    {
        var users = await _db.Users
            .Include(u => u.Roles)
            .OrderBy(u => u.UserId)
            .ToListAsync();

        return View(users);
    }

    /// <summary>
    /// Gán quyền Admin cho 1 user.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Promote(int userId)
    {
        // Kiểm tra đã có role Admin chưa
        var exists = await _db.Roles.AnyAsync(r => r.UserId == userId && r.RoleName == "Admin");
        if (!exists)
        {
            _db.Roles.Add(new Role
            {
                UserId = userId,
                RoleName = "Admin",
                CreateAt = DateTime.Now
            });
            await _db.SaveChangesAsync();
        }

        return RedirectToAction("Index");
    }

    /// <summary>
    /// Gỡ bỏ quyền Admin khỏi user.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Demote(int userId)
    {
        var role = await _db.Roles.FirstOrDefaultAsync(r => r.UserId == userId && r.RoleName == "Admin");
        if (role != null)
        {
            _db.Roles.Remove(role);
            await _db.SaveChangesAsync();
        }
        return RedirectToAction("Index");
    }

    /// <summary>
    /// Xem chi tiết user với lịch sử hoạt động và thống kê
    /// </summary>
    public async Task<IActionResult> UserDetails(int id)
    {
        var user = await _db.Users
            .Include(u => u.Roles)
            .Include(u => u.GroupMemberships).ThenInclude(gm => gm.ChatGroup)
            .Include(u => u.SentMessages)
            .Include(u => u.PaymentTransactions)
            .Include(u => u.QuizAttempts)
            .Include(u => u.Subscriptions)
            .FirstOrDefaultAsync(u => u.UserId == id);

        if (user == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy người dùng";
            return RedirectToAction("Users");
        }

        // Thống kê hoạt động của user
        ViewBag.MessageCount = user.SentMessages.Count;
        ViewBag.GroupCount = user.GroupMemberships.Count;
        ViewBag.QuizAttemptCount = user.QuizAttempts.Count;
        ViewBag.TotalSpent = user.PaymentTransactions.Where(t => t.Status == "PAID").Sum(t => t.Amount);
        
        // Hoạt động gần đây
        var recentMessages = await _db.Messages.Where(m => m.SenderId == id).OrderByDescending(m => m.SentAt).Take(10).ToListAsync();
        var recentPayments = user.PaymentTransactions.OrderByDescending(t => t.CreatedAt).Take(5).ToList();
        var recentQuizAttempts = user.QuizAttempts.OrderByDescending(a => a.StartedAt).Take(5).ToList();
        
        ViewBag.RecentMessages = recentMessages;
        ViewBag.RecentPayments = recentPayments;
        ViewBag.RecentQuizAttempts = recentQuizAttempts;

        return View(user);
    }

    /// <summary>
    /// Khóa tài khoản user
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> LockUser(int userId, string reason = "")
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null)
        {
            return Json(new { success = false, message = "Không tìm thấy người dùng" });
        }

        // Thêm role "Locked" để đánh dấu user bị khóa
        var lockedRole = await _db.Roles.FirstOrDefaultAsync(r => r.UserId == userId && r.RoleName == "Locked");
        if (lockedRole == null)
        {
            _db.Roles.Add(new Role
            {
                UserId = userId,
                RoleName = "Locked",
                CreateAt = DateTime.Now
            });

            // Ghi log audit
            _db.AnalyticsEvents.Add(new AnalyticsEvent
            {
                UserId = int.Parse(User.FindFirst("UserId")?.Value ?? "0"),
                EventType = "AdminAction",
                Metadata = $"Locked user {user.Email}. Reason: {reason}",
                OccurredAt = DateTime.Now
            });

            await _db.SaveChangesAsync();
        }

        return Json(new { success = true, message = "Đã khóa tài khoản người dùng" });
    }

    /// <summary>
    /// Mở khóa tài khoản user
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> UnlockUser(int userId)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null)
        {
            return Json(new { success = false, message = "Không tìm thấy người dùng" });
        }

        var lockedRole = await _db.Roles.FirstOrDefaultAsync(r => r.UserId == userId && r.RoleName == "Locked");
        if (lockedRole != null)
        {
            _db.Roles.Remove(lockedRole);

            // Ghi log audit
            _db.AnalyticsEvents.Add(new AnalyticsEvent
            {
                UserId = int.Parse(User.FindFirst("UserId")?.Value ?? "0"),
                EventType = "AdminAction",
                Metadata = $"Unlocked user {user.Email}",
                OccurredAt = DateTime.Now
            });

            await _db.SaveChangesAsync();
        }

        return Json(new { success = true, message = "Đã mở khóa tài khoản người dùng" });
    }

    /// <summary>
    /// Xóa user (soft delete)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> DeleteUser(int userId, string reason = "")
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null)
        {
            return Json(new { success = false, message = "Không tìm thấy người dùng" });
        }

        // Kiểm tra không được xóa admin cuối cùng
        var adminCount = await _db.Roles.CountAsync(r => r.RoleName == "Admin");
        var isAdmin = await _db.Roles.AnyAsync(r => r.UserId == userId && r.RoleName == "Admin");
        if (isAdmin && adminCount <= 1)
        {
            return Json(new { success = false, message = "Không thể xóa admin cuối cùng trong hệ thống" });
        }

        // Thêm role "Deleted" thay vì xóa thật
        _db.Roles.Add(new Role
        {
            UserId = userId,
            RoleName = "Deleted",
            CreateAt = DateTime.Now
        });

        // Ghi log audit
        _db.AnalyticsEvents.Add(new AnalyticsEvent
        {
            UserId = int.Parse(User.FindFirst("UserId")?.Value ?? "0"),
            EventType = "AdminAction",
            Metadata = $"Deleted user {user.Email}. Reason: {reason}",
            OccurredAt = DateTime.Now
        });

        await _db.SaveChangesAsync();

        return Json(new { success = true, message = "Đã xóa người dùng thành công" });
    }

    /// <summary>
    /// Lấy thống kê hoạt động user theo thời gian (cho biểu đồ)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetUserActivityChart(int userId, string range = "month")
    {
        var now = DateTime.UtcNow;
        int days = range == "week" ? 7 : 30;
        var fromDate = now.Date.AddDays(-days + 1);

        var labels = Enumerable.Range(0, days)
            .Select(i => fromDate.AddDays(i).ToString("dd/MM"))
            .ToList();

        // Messages sent by user
        var messageData = await _db.Messages
            .Where(m => m.SenderId == userId && m.SentAt >= fromDate)
            .GroupBy(m => m.SentAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync();
        var messageSeries = labels.Select(l => {
            var d = DateTime.ParseExact(l, "dd/MM", null);
            return messageData.FirstOrDefault(x => x.Date == d)?.Count ?? 0;
        }).ToList();

        // Quiz attempts by user
        var quizData = await _db.QuizAttempts
            .Where(a => a.UserId == userId && a.StartedAt >= fromDate)
            .GroupBy(a => a.StartedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync();
        var quizSeries = labels.Select(l => {
            var d = DateTime.ParseExact(l, "dd/MM", null);
            return quizData.FirstOrDefault(x => x.Date == d)?.Count ?? 0;
        }).ToList();

        return Json(new { labels, messageSeries, quizSeries });
    }

    /// <summary>
    /// Danh sách nhóm chat với phân trang và tìm kiếm
    /// </summary>
    public async Task<IActionResult> Groups(string search = "", int page = 1, int pageSize = 20, string sortBy = "CreatedAt", string sortOrder = "desc")
    {
        IQueryable<ChatGroup> query = _db.ChatGroups
            .Include(g => g.Members).ThenInclude(m => m.User)
            .Include(g => g.Messages);

        // Tìm kiếm
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(g => g.Name.Contains(search) || g.Description.Contains(search));
        }

        // Sắp xếp
        switch (sortBy.ToLower())
        {
            case "name":
                query = sortOrder == "asc" ? query.OrderBy(g => g.Name) : query.OrderByDescending(g => g.Name);
                break;
            case "membercount":
                query = sortOrder == "asc" ? query.OrderBy(g => g.Members.Count) : query.OrderByDescending(g => g.Members.Count);
                break;
            case "messagecount":
                query = sortOrder == "asc" ? query.OrderBy(g => g.Messages.Count) : query.OrderByDescending(g => g.Messages.Count);
                break;
            default:
                query = sortOrder == "asc" ? query.OrderBy(g => g.CreatedAt) : query.OrderByDescending(g => g.CreatedAt);
                break;
        }

        var totalGroups = await query.CountAsync();
        var groups = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        ViewBag.Search = search;
        ViewBag.Page = page;
        ViewBag.PageSize = pageSize;
        ViewBag.TotalPages = (int)Math.Ceiling((double)totalGroups / pageSize);
        ViewBag.SortBy = sortBy;
        ViewBag.SortOrder = sortOrder;
        ViewBag.TotalGroups = totalGroups;

        return View(groups);
    }

    /// <summary>
    /// Chi tiết nhóm chat với thống kê và lịch sử
    /// </summary>
    public async Task<IActionResult> GroupDetails(int id)
    {
        var group = await _db.ChatGroups
            .Include(g => g.Members).ThenInclude(m => m.User)
            .Include(g => g.Messages).ThenInclude(m => m.Sender)
            .Include(g => g.Creator)
            .FirstOrDefaultAsync(g => g.GroupId == id);

        if (group == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy nhóm chat";
            return RedirectToAction("Groups");
        }

        // Thống kê nhóm
        var now = DateTime.UtcNow;
        var today = now.Date;
        var weekAgo = now.AddDays(-7);
        var monthAgo = now.AddMonths(-1);

        ViewBag.TotalMessages = group.Messages.Count;
        ViewBag.MessagesToday = group.Messages.Count(m => m.SentAt >= today);
        ViewBag.MessagesWeek = group.Messages.Count(m => m.SentAt >= weekAgo);
        ViewBag.MessagesMonth = group.Messages.Count(m => m.SentAt >= monthAgo);

        ViewBag.ActiveMembers = group.Members.Count(m => group.Messages.Any(msg => msg.SenderId == m.UserId && msg.SentAt >= weekAgo));
        ViewBag.NewMembersWeek = group.Members.Count(m => m.JoinedAt >= weekAgo);

        // Top contributors - sử dụng structure thống nhất
        var topContributorsList = new List<object>();
        var topContributorsData = group.Messages
            .GroupBy(m => m.Sender)
            .Select(g => new { User = g.Key, MessageCount = g.Count() })
            .OrderByDescending(x => x.MessageCount)
            .Take(5)
            .ToList();

        foreach (var contributor in topContributorsData)
        {
            topContributorsList.Add(new {
                UserId = contributor.User.UserId,
                FullName = contributor.User.FullName,
                Email = contributor.User.Email,
                AvatarUrl = contributor.User.AvatarUrl,
                MessageCount = contributor.MessageCount,
                IsPremium = contributor.User.IsPremium
            });
        }
        ViewBag.TopContributors = topContributorsList;

        // Recent messages
        var recentMessages = group.Messages.OrderByDescending(m => m.SentAt).Take(20).ToList();
        ViewBag.RecentMessages = recentMessages;

        return View(group);
    }

    /// <summary>
    /// Xóa nhóm chat (soft delete)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> DeleteGroup(int groupId, string reason = "")
    {
        var group = await _db.ChatGroups.FindAsync(groupId);
        if (group == null)
        {
            return Json(new { success = false, message = "Không tìm thấy nhóm chat" });
        }

        // Xóa nhóm khỏi database (có thể thay đổi thành soft delete nếu model hỗ trợ)
        _db.ChatGroups.Remove(group);

        // Ghi log audit
        _db.AnalyticsEvents.Add(new AnalyticsEvent
        {
            UserId = int.Parse(User.FindFirst("UserId")?.Value ?? "0"),
            EventType = "AdminAction",
            Metadata = $"Deleted group '{group.Name}' (ID: {groupId}). Reason: {reason}",
            OccurredAt = DateTime.Now
        });

        await _db.SaveChangesAsync();

        return Json(new { success = true, message = "Đã xóa nhóm chat thành công" });
    }

    /// <summary>
    /// Thêm thành viên vào nhóm
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> AddGroupMember(int groupId, int userId)
    {
        var group = await _db.ChatGroups.FindAsync(groupId);
        var user = await _db.Users.FindAsync(userId);

        if (group == null || user == null)
        {
            return Json(new { success = false, message = "Không tìm thấy nhóm hoặc người dùng" });
        }

        // Kiểm tra đã là thành viên chưa
        var existingMember = await _db.GroupMembers.FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.UserId == userId);
        if (existingMember != null)
        {
            return Json(new { success = false, message = "Người dùng đã là thành viên của nhóm" });
        }

        _db.GroupMembers.Add(new GroupMember
        {
            GroupId = groupId,
            UserId = userId,
            JoinedAt = DateTime.Now,
            IsModerator = false
        });

        // Ghi log audit
        _db.AnalyticsEvents.Add(new AnalyticsEvent
        {
            UserId = int.Parse(User.FindFirst("UserId")?.Value ?? "0"),
            EventType = "AdminAction",
            Metadata = $"Added user {user.Email} to group '{group.Name}'",
            OccurredAt = DateTime.Now
        });

        await _db.SaveChangesAsync();

        return Json(new { success = true, message = "Đã thêm thành viên vào nhóm" });
    }

    /// <summary>
    /// Xóa thành viên khỏi nhóm
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> RemoveGroupMember(int groupId, int userId, string reason = "")
    {
        var membership = await _db.GroupMembers
            .Include(gm => gm.User)
            .Include(gm => gm.ChatGroup)
            .FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.UserId == userId);

        if (membership == null)
        {
            return Json(new { success = false, message = "Không tìm thấy thành viên trong nhóm" });
        }

        _db.GroupMembers.Remove(membership);

        // Ghi log audit
        _db.AnalyticsEvents.Add(new AnalyticsEvent
        {
            UserId = int.Parse(User.FindFirst("UserId")?.Value ?? "0"),
            EventType = "AdminAction",
            Metadata = $"Removed user {membership.User.Email} from group '{membership.ChatGroup.Name}'. Reason: {reason}",
            OccurredAt = DateTime.Now
        });

        await _db.SaveChangesAsync();

        return Json(new { success = true, message = "Đã xóa thành viên khỏi nhóm" });
    }

    /// <summary>
    /// Cập nhật quyền moderator cho thành viên
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> UpdateMemberRole(int groupId, int userId, bool isModerator)
    {
        var membership = await _db.GroupMembers
            .Include(gm => gm.User)
            .Include(gm => gm.ChatGroup)
            .FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.UserId == userId);

        if (membership == null)
        {
            return Json(new { success = false, message = "Không tìm thấy thành viên trong nhóm" });
        }

        membership.IsModerator = isModerator;

        // Ghi log audit
        _db.AnalyticsEvents.Add(new AnalyticsEvent
        {
            UserId = int.Parse(User.FindFirst("UserId")?.Value ?? "0"),
            EventType = "AdminAction",
            Metadata = $"{(isModerator ? "Granted" : "Revoked")} moderator role for {membership.User.Email} in group '{membership.ChatGroup.Name}'",
            OccurredAt = DateTime.Now
        });

        await _db.SaveChangesAsync();

        return Json(new { success = true, message = $"Đã {(isModerator ? "cấp" : "gỡ")} quyền moderator" });
    }

    /// <summary>
    /// Xóa tin nhắn trong nhóm (content moderation)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> DeleteMessage(int messageId, string reason = "")
    {
        var message = await _db.Messages
            .Include(m => m.Sender)
            .FirstOrDefaultAsync(m => m.MessageId == messageId);

        if (message == null)
        {
            return Json(new { success = false, message = "Không tìm thấy tin nhắn" });
        }

        // Thay đổi nội dung tin nhắn để đánh dấu đã bị xóa
        message.Content = "[Tin nhắn đã bị xóa bởi admin]";

        // Ghi log audit
        _db.AnalyticsEvents.Add(new AnalyticsEvent
        {
            UserId = int.Parse(User.FindFirst("UserId")?.Value ?? "0"),
            EventType = "AdminAction",
            Metadata = $"Deleted message from {message.Sender.Email}. Reason: {reason}",
            OccurredAt = DateTime.Now
        });

        await _db.SaveChangesAsync();

        return Json(new { success = true, message = "Đã xóa tin nhắn" });
    }

    /// <summary>
    /// Lấy thống kê hoạt động nhóm theo thời gian (cho biểu đồ)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetGroupActivityChart(int groupId, string range = "month")
    {
        var now = DateTime.UtcNow;
        int days = range == "week" ? 7 : 30;
        var fromDate = now.Date.AddDays(-days + 1);

        var labels = Enumerable.Range(0, days)
            .Select(i => fromDate.AddDays(i).ToString("dd/MM"))
            .ToList();

        // Messages in group by day
        var messageData = await _db.Messages
            .Where(m => m.GroupId == groupId && m.SentAt >= fromDate)
            .GroupBy(m => m.SentAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync();
        var messageSeries = labels.Select(l => {
            var d = DateTime.ParseExact(l, "dd/MM", null);
            return messageData.FirstOrDefault(x => x.Date == d)?.Count ?? 0;
        }).ToList();

        // New members by day
        var memberData = await _db.GroupMembers
            .Where(gm => gm.GroupId == groupId && gm.JoinedAt >= fromDate)
            .GroupBy(gm => gm.JoinedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync();
        var memberSeries = labels.Select(l => {
            var d = DateTime.ParseExact(l, "dd/MM", null);
            return memberData.FirstOrDefault(x => x.Date == d)?.Count ?? 0;
        }).ToList();

        return Json(new { labels, messageSeries, memberSeries });
    }

    /// <summary>
    /// Lấy dữ liệu biểu đồ hoạt động của meeting
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetMeetingActivityChart(int roomId, string range = "month")
    {
        try
        {
            var now = DateTime.UtcNow;
            int days = range == "week" ? 7 : 30;
            var fromDate = now.Date.AddDays(-days + 1);

            var labels = Enumerable.Range(0, days)
                .Select(i => fromDate.AddDays(i).ToString("dd/MM"))
                .ToList();

            // Participants by day
            var participantData = await _db.RoomParticipants
                .Where(p => p.RoomId == roomId && p.JoinedAt >= fromDate)
                .GroupBy(p => p.JoinedAt.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToListAsync();
            var participantSeries = labels.Select(l => {
                try
                {
                    var d = DateTime.ParseExact(l, "dd/MM", System.Globalization.CultureInfo.InvariantCulture);
                    return participantData.FirstOrDefault(x => x.Date == d)?.Count ?? 0;
                }
                catch
                {
                    return 0;
                }
            }).ToList();

            // Duration by day (total minutes)
            var durationData = await _db.RoomParticipants
                .Where(p => p.RoomId == roomId && p.JoinedAt >= fromDate)
                .GroupBy(p => p.JoinedAt.Date)
                .Select(g => new { 
                    Date = g.Key, 
                    Duration = g.Sum(p => p.LeftAt.HasValue ? 
                        (int)(p.LeftAt.Value - p.JoinedAt).TotalMinutes : 0) 
                })
                .ToListAsync();
            var durationSeries = labels.Select(l => {
                try
                {
                    var d = DateTime.ParseExact(l, "dd/MM", System.Globalization.CultureInfo.InvariantCulture);
                    return durationData.FirstOrDefault(x => x.Date == d)?.Duration ?? 0;
                }
                catch
                {
                    return 0;
                }
            }).ToList();

            var result = new { labels, participantSeries, durationSeries };
            return Json(result);
        }
        catch (Exception ex)
        {
            return Json(new { 
                error = ex.Message, 
                labels = new List<string>(), 
                participantSeries = new List<int>(), 
                durationSeries = new List<int>() 
            });
        }
    }

    /// <summary>
    /// Danh sách phòng họp với phân trang và tìm kiếm
    /// </summary>
    public async Task<IActionResult> Meetings(string search = "", int page = 1, int pageSize = 20, string sortBy = "CreatedAt", string sortOrder = "desc")
    {
        IQueryable<VideoRoom> query = _db.VideoRooms
            .Include(r => r.Creator)
            .Include(r => r.Participants);

        // Tìm kiếm
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(r => r.Name.Contains(search));
        }

        // Sắp xếp
        switch (sortBy.ToLower())
        {
            case "name":
                query = sortOrder == "asc" ? query.OrderBy(r => r.Name) : query.OrderByDescending(r => r.Name);
                break;
            case "participantcount":
                query = sortOrder == "asc" ? query.OrderBy(r => r.Participants.Count) : query.OrderByDescending(r => r.Participants.Count);
                break;
            case "maxparticipants":
                query = sortOrder == "asc" ? query.OrderBy(r => r.MaxParticipants) : query.OrderByDescending(r => r.MaxParticipants);
                break;
            default:
                query = sortOrder == "asc" ? query.OrderBy(r => r.CreatedAt) : query.OrderByDescending(r => r.CreatedAt);
                break;
        }

        var totalMeetings = await query.CountAsync();
        var meetings = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        ViewBag.Search = search;
        ViewBag.Page = page;
        ViewBag.PageSize = pageSize;
        ViewBag.TotalPages = (int)Math.Ceiling((double)totalMeetings / pageSize);
        ViewBag.SortBy = sortBy;
        ViewBag.SortOrder = sortOrder;
        ViewBag.TotalMeetings = totalMeetings;

        return View(meetings);
    }

    /// <summary>
    /// Chi tiết phòng họp với thống kê và lịch sử
    /// </summary>
    public async Task<IActionResult> MeetingDetails(int id)
    {
        var meeting = await _db.VideoRooms
            .Include(r => r.Creator)
            .Include(r => r.Participants).ThenInclude(p => p.User)
            .FirstOrDefaultAsync(r => r.RoomId == id);

        if (meeting == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy phòng họp";
            return RedirectToAction("Meetings");
        }

        // Thống kê phòng họp - dữ liệu thật
        var activeParticipants = meeting.Participants.Count(p => !p.LeftAt.HasValue); // Người vẫn còn trong phòng
        var totalParticipants = meeting.Participants.Count; // Tổng số người đã tham gia
        
        // Tính tổng thời gian tham gia (phút)
        var totalDuration = meeting.Participants
            .Where(p => p.LeftAt.HasValue)
            .Sum(p => (int)(p.LeftAt.Value - p.JoinedAt).TotalMinutes);
        
        // Lấy recordings thật của phòng này
        var recordings = await _db.Recordings
            .Where(r => r.MeetingCode == meeting.Name || r.MeetingCode == meeting.RoomId.ToString())
            .Where(r => r.IsActive && !r.DeletedAt.HasValue)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
            
        var recordingsCount = recordings.Count;

        // Lấy room events thật
        var roomEvents = await _db.RoomEvents
            .Where(e => e.RoomPassword == meeting.Password)
            .OrderByDescending(e => e.Timestamp)
            .Take(10)
            .ToListAsync();

        ViewBag.ActiveParticipants = activeParticipants;
        ViewBag.TotalParticipants = totalParticipants;
        ViewBag.TotalDuration = totalDuration;
        ViewBag.RecordingsCount = recordingsCount;
        ViewBag.Participants = meeting.Participants.ToList();
        ViewBag.Recordings = recordings;
        ViewBag.RoomEvents = roomEvents;

        return View(meeting);
    }

    /// <summary>
    /// Xóa phòng họp
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> DeleteMeeting(int meetingId, string reason = "")
    {
        var meeting = await _db.VideoRooms.FindAsync(meetingId);
        if (meeting == null)
        {
            return Json(new { success = false, message = "Không tìm thấy phòng họp" });
        }

        // Xóa phòng họp
        _db.VideoRooms.Remove(meeting);

        // Ghi log audit
        _db.AnalyticsEvents.Add(new AnalyticsEvent
        {
            UserId = int.Parse(User.FindFirst("UserId")?.Value ?? "0"),
            EventType = "AdminAction",
            Metadata = $"Deleted meeting '{meeting.Name}' (ID: {meetingId}). Reason: {reason}",
            OccurredAt = DateTime.Now
        });

        await _db.SaveChangesAsync();

        return Json(new { success = true, message = "Đã xóa phòng họp thành công" });
    }

    /// <summary>
    /// Kết thúc phòng họp đang diễn ra
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> EndMeeting(int meetingId, string reason = "")
    {
        var meeting = await _db.VideoRooms.FindAsync(meetingId);
        if (meeting == null)
        {
            return Json(new { success = false, message = "Không tìm thấy phòng họp" });
        }

        if (!meeting.IsOpen)
        {
            return Json(new { success = false, message = "Phòng họp đã kết thúc" });
        }

        // Kết thúc phòng họp
        meeting.IsOpen = false;

        // Ghi log audit
        _db.AnalyticsEvents.Add(new AnalyticsEvent
        {
            UserId = int.Parse(User.FindFirst("UserId")?.Value ?? "0"),
            EventType = "AdminAction",
            Metadata = $"Ended meeting '{meeting.Name}' (ID: {meetingId}). Reason: {reason}",
            OccurredAt = DateTime.Now
        });

        await _db.SaveChangesAsync();

        return Json(new { success = true, message = "Đã kết thúc phòng họp" });
    }

    /// <summary>
    /// Xóa participant khỏi phòng họp
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> RemoveParticipant(int meetingId, int userId, string reason = "")
    {
        var participant = await _db.RoomParticipants
            .Include(p => p.User)
            .Include(p => p.VideoRoom)
            .FirstOrDefaultAsync(p => p.RoomId == meetingId && p.UserId == userId);

        if (participant == null)
        {
            return Json(new { success = false, message = "Không tìm thấy participant" });
        }

        // Đánh dấu participant đã rời
        participant.LeftAt = DateTime.Now;

        // Ghi log audit
        _db.AnalyticsEvents.Add(new AnalyticsEvent
        {
            UserId = int.Parse(User.FindFirst("UserId")?.Value ?? "0"),
            EventType = "AdminAction",
            Metadata = $"Removed participant {participant.User.Email} from meeting '{participant.VideoRoom.Name}'. Reason: {reason}",
            OccurredAt = DateTime.Now
        });

        await _db.SaveChangesAsync();

        return Json(new { success = true, message = "Đã xóa participant khỏi phòng họp" });
    }

    /// <summary>
    /// Xóa recording
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> DeleteRecording(Guid recordingId, string reason = "")
    {
        var recording = await _db.Recordings
            .FirstOrDefaultAsync(r => r.Id == recordingId);

        if (recording == null)
        {
            return Json(new { success = false, message = "Không tìm thấy recording" });
        }

        // Xóa recording
        _db.Recordings.Remove(recording);

        // Ghi log audit
        _db.AnalyticsEvents.Add(new AnalyticsEvent
        {
            UserId = int.Parse(User.FindFirst("UserId")?.Value ?? "0"),
            EventType = "AdminAction",
            Metadata = $"Deleted recording '{recording.FileName}'. Reason: {reason}",
            OccurredAt = DateTime.Now
        });

        await _db.SaveChangesAsync();

        return Json(new { success = true, message = "Đã xóa recording thành công" });
    }

    /// <summary>
    /// Lấy thống kê phòng họp theo thời gian (cho biểu đồ)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetMeetingStatsChart(string range = "month")
    {
        var now = DateTime.UtcNow;
        int days = range == "week" ? 7 : 30;
        var fromDate = now.Date.AddDays(-days + 1);

        var labels = Enumerable.Range(0, days)
            .Select(i => fromDate.AddDays(i).ToString("dd/MM"))
            .ToList();

        // Meetings created by day
        var meetingData = await _db.VideoRooms
            .Where(r => r.CreatedAt >= fromDate)
            .GroupBy(r => r.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync();
        var meetingSeries = labels.Select(l => {
            var d = DateTime.ParseExact(l, "dd/MM", null);
            return meetingData.FirstOrDefault(x => x.Date == d)?.Count ?? 0;
        }).ToList();

        // Total participants by day
        var participantData = await _db.RoomParticipants
            .Where(p => p.JoinedAt >= fromDate)
            .GroupBy(p => p.JoinedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync();
        var participantSeries = labels.Select(l => {
            var d = DateTime.ParseExact(l, "dd/MM", null);
            return participantData.FirstOrDefault(x => x.Date == d)?.Count ?? 0;
        }).ToList();

        // Recording size by day (in MB)
        var recordingData = await _db.Recordings
            .Where(r => r.CreatedAt >= fromDate)
            .GroupBy(r => r.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Size = g.Sum(x => x.FileSize) / (1024 * 1024) })
            .ToListAsync();
        var recordingSeries = labels.Select(l => {
            var d = DateTime.ParseExact(l, "dd/MM", null);
            return recordingData.FirstOrDefault(x => x.Date == d)?.Size ?? 0;
        }).ToList();

        return Json(new { labels, meetingSeries, participantSeries, recordingSeries });
    }

    /// <summary>
    /// Danh sách quiz với phân trang và tìm kiếm
    /// </summary>
    public async Task<IActionResult> Quizzes(string search = "", int page = 1, int pageSize = 20, string sortBy = "CreatedAt", string sortOrder = "desc")
    {
        IQueryable<Quiz> query = _db.Quizzes
            .Include(q => q.Creator)
            .Include(q => q.Attempts)
            .Include(q => q.Questions);

        // Tìm kiếm
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(q => q.Title.Contains(search) || q.Description.Contains(search));
        }

        // Sắp xếp
        switch (sortBy.ToLower())
        {
            case "title":
                query = sortOrder == "asc" ? query.OrderBy(q => q.Title) : query.OrderByDescending(q => q.Title);
                break;
            case "attemptcount":
                query = sortOrder == "asc" ? query.OrderBy(q => q.Attempts.Count) : query.OrderByDescending(q => q.Attempts.Count);
                break;
            case "questioncount":
                query = sortOrder == "asc" ? query.OrderBy(q => q.Questions.Count) : query.OrderByDescending(q => q.Questions.Count);
                break;
            case "duration":
                query = sortOrder == "asc" ? query.OrderBy(q => q.TimeLimit) : query.OrderByDescending(q => q.TimeLimit);
                break;
            default:
                query = sortOrder == "asc" ? query.OrderBy(q => q.CreatedAt) : query.OrderByDescending(q => q.CreatedAt);
                break;
        }

        var totalQuizzes = await query.CountAsync();
        var quizzes = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        ViewBag.Search = search;
        ViewBag.Page = page;
        ViewBag.PageSize = pageSize;
        ViewBag.TotalPages = (int)Math.Ceiling((double)totalQuizzes / pageSize);
        ViewBag.SortBy = sortBy;
        ViewBag.SortOrder = sortOrder;
        ViewBag.TotalQuizzes = totalQuizzes;

        return View(quizzes);
    }

    /// <summary>
    /// Chi tiết quiz với thống kê và phân tích
    /// </summary>
    public async Task<IActionResult> QuizDetails(int id)
    {
        var quiz = await _db.Quizzes
            .Include(q => q.Creator)
            .Include(q => q.Questions)
            .Include(q => q.Attempts).ThenInclude(a => a.User)
            .Include(q => q.Attempts).ThenInclude(a => a.Details)
            .FirstOrDefaultAsync(q => q.QuizId == id);

        if (quiz == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy quiz";
            return RedirectToAction("Quizzes");
        }

        // Thống kê quiz
        var totalAttempts = quiz.Attempts.Count;
        var completedAttempts = quiz.Attempts.Count(a => a.EndedAt > a.StartedAt);
        var averageScore = quiz.Attempts.Where(a => a.EndedAt > a.StartedAt).DefaultIfEmpty().Average(a => a?.Score ?? 0);
        var highestScore = quiz.Attempts.Where(a => a.EndedAt > a.StartedAt).DefaultIfEmpty().Max(a => a?.Score ?? 0);
        var lowestScore = quiz.Attempts.Where(a => a.EndedAt > a.StartedAt).DefaultIfEmpty().Min(a => a?.Score ?? 0);
        var averageTime = quiz.Attempts.Where(a => a.EndedAt > a.StartedAt)
            .DefaultIfEmpty()
            .Average(a => a != null ? (a.EndedAt - a.StartedAt).TotalMinutes : 0);

        ViewBag.TotalAttempts = totalAttempts;
        ViewBag.CompletedAttempts = completedAttempts;
        ViewBag.AverageScore = Math.Round(averageScore, 2);
        ViewBag.HighestScore = Math.Round(highestScore, 2);
        ViewBag.LowestScore = Math.Round(lowestScore, 2);
        ViewBag.AverageTime = Math.Round(averageTime, 2);
        ViewBag.PassRate = completedAttempts > 0 ? Math.Round((double)quiz.Attempts.Count(a => a.Score >= 50) / completedAttempts * 100, 2) : 0;

        // Top performers
        var topPerformers = quiz.Attempts
            .Where(a => a.EndedAt > a.StartedAt)
            .OrderByDescending(a => a.Score)
            .Take(10)
            .ToList();
        ViewBag.TopPerformers = topPerformers;

        // Recent attempts
        var recentAttempts = quiz.Attempts
            .OrderByDescending(a => a.StartedAt)
            .Take(20)
            .ToList();
        ViewBag.RecentAttempts = recentAttempts;

        // Question analytics
        var questionStats = new List<dynamic>();
        foreach (var question in quiz.Questions)
        {
            var questionDetails = quiz.Attempts
                .SelectMany(a => a.Details)
                .Where(d => d.QuestionId == question.QuestionId)
                .ToList();

            var correctCount = questionDetails.Count(d => d.IsCorrect);
            var totalAnswers = questionDetails.Count;
            var accuracy = totalAnswers > 0 ? Math.Round((double)correctCount / totalAnswers * 100, 2) : 0;

            // Tính điểm tối đa cho câu hỏi dựa trên loại
            var maxScore = question.QuestionType switch
            {
                "MultipleChoice" => 1.0,
                "TrueFalse" => 1.0,
                "Essay" => 10.0, // Essay có thể có điểm cao hơn
                _ => 1.0
            };

            questionStats.Add(new
            {
                Question = question,
                TotalAnswers = totalAnswers,
                CorrectAnswers = correctCount,
                Accuracy = accuracy,
                MaxScore = maxScore
            });
        }
        ViewBag.QuestionStats = questionStats;

        return View(quiz);
    }

    /// <summary>
    /// Xóa quiz
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> DeleteQuiz(int quizId, string reason = "")
    {
        var quiz = await _db.Quizzes.FindAsync(quizId);
        if (quiz == null)
        {
            return Json(new { success = false, message = "Không tìm thấy quiz" });
        }

        // Xóa quiz
        _db.Quizzes.Remove(quiz);

        // Ghi log audit
        _db.AnalyticsEvents.Add(new AnalyticsEvent
        {
            UserId = int.Parse(User.FindFirst("UserId")?.Value ?? "0"),
            EventType = "AdminAction",
            Metadata = $"Deleted quiz '{quiz.Title}' (ID: {quizId}). Reason: {reason}",
            OccurredAt = DateTime.Now
        });

        await _db.SaveChangesAsync();

        return Json(new { success = true, message = "Đã xóa quiz thành công" });
    }

    /// <summary>
    /// Xóa attempt của quiz
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> DeleteQuizAttempt(Guid attemptId, string reason = "")
    {
        var attempt = await _db.QuizAttempts
            .Include(a => a.Quiz)
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.AttemptId == attemptId);

        if (attempt == null)
        {
            return Json(new { success = false, message = "Không tìm thấy attempt" });
        }

        // Xóa attempt
        _db.QuizAttempts.Remove(attempt);

        // Ghi log audit
        _db.AnalyticsEvents.Add(new AnalyticsEvent
        {
            UserId = int.Parse(User.FindFirst("UserId")?.Value ?? "0"),
            EventType = "AdminAction",
            Metadata = $"Deleted quiz attempt from {attempt.User.Email} for quiz '{attempt.Quiz.Title}'. Reason: {reason}",
            OccurredAt = DateTime.Now
        });

        await _db.SaveChangesAsync();

        return Json(new { success = true, message = "Đã xóa attempt thành công" });
    }

    /// <summary>
    /// Reset quiz attempt (cho phép làm lại)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> ResetQuizAttempt(Guid attemptId, string reason = "")
    {
        var attempt = await _db.QuizAttempts
            .Include(a => a.Quiz)
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.AttemptId == attemptId);

        if (attempt == null)
        {
            return Json(new { success = false, message = "Không tìm thấy attempt" });
        }

        // Reset attempt
        attempt.EndedAt = attempt.StartedAt; // Reset về chưa hoàn thành
        attempt.Score = 0;

        // Xóa tất cả details (nếu có trong DbContext)
        // var details = await _db.QuizAttemptDetails.Where(d => d.AttemptId == attemptId).ToListAsync();
        // _db.QuizAttemptDetails.RemoveRange(details);

        // Ghi log audit
        _db.AnalyticsEvents.Add(new AnalyticsEvent
        {
            UserId = int.Parse(User.FindFirst("UserId")?.Value ?? "0"),
            EventType = "AdminAction",
            Metadata = $"Reset quiz attempt for {attempt.User.Email} in quiz '{attempt.Quiz.Title}'. Reason: {reason}",
            OccurredAt = DateTime.Now
        });

        await _db.SaveChangesAsync();

        return Json(new { success = true, message = "Đã reset attempt thành công" });
    }

    /// <summary>
    /// Lấy thống kê quiz theo thời gian (cho biểu đồ)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetQuizStatsChart(string range = "month")
    {
        var now = DateTime.UtcNow;
        int days = range == "week" ? 7 : 30;
        var fromDate = now.Date.AddDays(-days + 1);

        var labels = Enumerable.Range(0, days)
            .Select(i => fromDate.AddDays(i).ToString("dd/MM"))
            .ToList();

        // Quiz attempts by day
        var attemptData = await _db.QuizAttempts
            .Where(a => a.StartedAt >= fromDate)
            .GroupBy(a => a.StartedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync();
        var attemptSeries = labels.Select(l => {
            var d = DateTime.ParseExact(l, "dd/MM", null);
            return attemptData.FirstOrDefault(x => x.Date == d)?.Count ?? 0;
        }).ToList();

        // Completed attempts by day (using EndedAt > StartedAt as completed indicator)
        var completedData = await _db.QuizAttempts
            .Where(a => a.EndedAt > a.StartedAt && a.EndedAt.Date >= fromDate)
            .GroupBy(a => a.EndedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync();
        var completedSeries = labels.Select(l => {
            var d = DateTime.ParseExact(l, "dd/MM", null);
            return completedData.FirstOrDefault(x => x.Date == d)?.Count ?? 0;
        }).ToList();

        // Average score by day
        var scoreData = await _db.QuizAttempts
            .Where(a => a.EndedAt > a.StartedAt && a.EndedAt.Date >= fromDate)
            .GroupBy(a => a.EndedAt.Date)
            .Select(g => new { Date = g.Key, AvgScore = g.Average(x => (double)x.Score) })
            .ToListAsync();
        var scoreSeries = labels.Select(l => {
            var d = DateTime.ParseExact(l, "dd/MM", null);
            return Math.Round(scoreData.FirstOrDefault(x => x.Date == d)?.AvgScore ?? 0.0, 2);
        }).ToList();

        return Json(new { labels, attemptSeries, completedSeries, scoreSeries });
    }

    /// <summary>
    /// Danh sách file/media với phân trang và tìm kiếm
    /// </summary>
    public async Task<IActionResult> Files(string search = "", string type = "", int page = 1, int pageSize = 20, string sortBy = "UploadedAt", string sortOrder = "desc")
    {
        // Lấy từ Media table (chỉ có các field: MediaId, MessageId, UploadedAt, Url, MediaType, FileName)
        IQueryable<Media> query = _db.Media
            .Include(m => m.Message)
            .ThenInclude(msg => msg.Sender);

        // Tìm kiếm theo tên file
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(m => m.FileName != null && m.FileName.Contains(search));
        }

        // Lọc theo loại file (dựa vào MediaType)
        if (!string.IsNullOrEmpty(type))
        {
            switch (type.ToLower())
            {
                case "image":
                    query = query.Where(m => m.MediaType.StartsWith("image/"));
                    break;
                case "video":
                    query = query.Where(m => m.MediaType.StartsWith("video/"));
                    break;
                case "audio":
                    query = query.Where(m => m.MediaType.StartsWith("audio/"));
                    break;
                case "document":
                    query = query.Where(m => m.MediaType.Contains("pdf") || m.MediaType.Contains("doc") || m.MediaType.Contains("text"));
                    break;
            }
        }

        // Sắp xếp
        switch (sortBy.ToLower())
        {
            case "filename":
                query = sortOrder == "asc" ? query.OrderBy(m => m.FileName) : query.OrderByDescending(m => m.FileName);
                break;
            case "mediatype":
                query = sortOrder == "asc" ? query.OrderBy(m => m.MediaType) : query.OrderByDescending(m => m.MediaType);
                break;
            default:
                query = sortOrder == "asc" ? query.OrderBy(m => m.UploadedAt) : query.OrderByDescending(m => m.UploadedAt);
                break;
        }

        var totalFiles = await query.CountAsync();
        var files = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        // Thống kê tổng quan (Media model không có FileSize, chỉ thống kê số lượng)
        var totalImages = await _db.Media.CountAsync(m => m.MediaType.StartsWith("image/"));
        var totalVideos = await _db.Media.CountAsync(m => m.MediaType.StartsWith("video/"));
        var totalAudios = await _db.Media.CountAsync(m => m.MediaType.StartsWith("audio/"));
        var totalDocuments = await _db.Media.CountAsync(m => m.MediaType.Contains("pdf") || m.MediaType.Contains("doc") || m.MediaType.Contains("text"));

        ViewBag.Search = search;
        ViewBag.Type = type;
        ViewBag.Page = page;
        ViewBag.PageSize = pageSize;
        ViewBag.TotalPages = (int)Math.Ceiling((double)totalFiles / pageSize);
        ViewBag.SortBy = sortBy;
        ViewBag.SortOrder = sortOrder;
        ViewBag.TotalFiles = totalFiles;
        ViewBag.TotalImages = totalImages;
        ViewBag.TotalVideos = totalVideos;
        ViewBag.TotalAudios = totalAudios;
        ViewBag.TotalDocuments = totalDocuments;

        return View(files);
    }

    /// <summary>
    /// Chi tiết file/media
    /// </summary>
    public async Task<IActionResult> FileDetails(Guid id)
    {
        var media = await _db.Media
            .Include(m => m.Message)
            .ThenInclude(msg => msg.Sender)
            .FirstOrDefaultAsync(m => m.MediaId == id);

        if (media == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy file";
            return RedirectToAction("Files");
        }

        // Lấy thông tin file summary nếu có
        var fileSummary = await _db.FileSummaries.FirstOrDefaultAsync(fs => fs.FileName == media.FileName);

        ViewBag.FileSummary = fileSummary;

        return View(media);
    }

    /// <summary>
    /// Xóa file/media
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> DeleteFile(Guid fileId, string reason = "")
    {
        var media = await _db.Media.FindAsync(fileId);
        if (media == null)
        {
            return Json(new { success = false, message = "Không tìm thấy file" });
        }

        try
        {
            // Xóa file vật lý (nếu tồn tại)
            var filePath = Path.Combine("wwwroot", media.Url.TrimStart('/'));
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }

            // Xóa bản ghi trong database
            _db.Media.Remove(media);

            // Ghi log audit
            _db.AnalyticsEvents.Add(new AnalyticsEvent
            {
                UserId = int.Parse(User.FindFirst("UserId")?.Value ?? "0"),
                EventType = "AdminAction",
                Metadata = $"Deleted file '{media.FileName}' (Type: {media.MediaType}). Reason: {reason}",
                OccurredAt = DateTime.Now
            });

            await _db.SaveChangesAsync();

            return Json(new { success = true, message = "Đã xóa file thành công" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Lỗi khi xóa file: " + ex.Message });
        }
    }

    /// <summary>
    /// Lấy thống kê file theo thời gian (cho biểu đồ)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetFileStatsChart(string range = "month")
    {
        var now = DateTime.UtcNow;
        int days = range == "week" ? 7 : 30;
        var fromDate = now.Date.AddDays(-days + 1);

        var labels = Enumerable.Range(0, days)
            .Select(i => fromDate.AddDays(i).ToString("dd/MM"))
            .ToList();

        // Files uploaded by day
        var fileData = await _db.Media
            .Where(m => m.UploadedAt >= fromDate)
            .GroupBy(m => m.UploadedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync();
        var fileSeries = labels.Select(l => {
            var d = DateTime.ParseExact(l, "dd/MM", null);
            return fileData.FirstOrDefault(x => x.Date == d)?.Count ?? 0;
        }).ToList();

        return Json(new { labels, fileSeries });
    }

    /// <summary>
    /// Lấy thống kê file theo loại
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetFileTypeStats()
    {
        var stats = new
        {
            Images = await _db.Media.CountAsync(m => m.MediaType.StartsWith("image/")),
            Videos = await _db.Media.CountAsync(m => m.MediaType.StartsWith("video/")),
            Audios = await _db.Media.CountAsync(m => m.MediaType.StartsWith("audio/")),
            Documents = await _db.Media.CountAsync(m => m.MediaType.Contains("pdf") || m.MediaType.Contains("doc") || m.MediaType.Contains("text")),
            Others = await _db.Media.CountAsync(m => !m.MediaType.StartsWith("image/") && !m.MediaType.StartsWith("video/") && !m.MediaType.StartsWith("audio/") && !m.MediaType.Contains("pdf") && !m.MediaType.Contains("doc") && !m.MediaType.Contains("text"))
        };

        return Json(stats);
    }

    /// <summary>
    /// Lấy top users upload file nhiều nhất
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetTopFileUsers()
    {
        var topUsers = await _db.Media
            .Include(m => m.Message)
            .ThenInclude(msg => msg.Sender)
            .Where(m => m.Message.Sender != null)
            .GroupBy(m => new { m.Message.SenderId, m.Message.Sender.FullName, m.Message.Sender.Email })
            .Select(g => new {
                UserId = g.Key.SenderId,
                UserName = g.Key.FullName ?? g.Key.Email,
                FileCount = g.Count()
            })
            .OrderByDescending(x => x.FileCount)
            .Take(10)
            .ToListAsync();

        return Json(topUsers);
    }

    /// <summary>
    /// Dọn dẹp file không sử dụng
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CleanupUnusedFiles()
    {
        try
        {
            var deletedCount = 0;

            // Tìm các file trong database nhưng không tồn tại trên disk
            var allFiles = await _db.Media.ToListAsync();
            var filesToRemove = new List<Media>();

            foreach (var media in allFiles)
            {
                var filePath = Path.Combine("wwwroot", media.Url.TrimStart('/'));
                if (!System.IO.File.Exists(filePath))
                {
                    filesToRemove.Add(media);
                }
            }

            // Xóa các bản ghi file không tồn tại
            _db.Media.RemoveRange(filesToRemove);
            deletedCount = filesToRemove.Count;

            // Ghi log audit
            _db.AnalyticsEvents.Add(new AnalyticsEvent
            {
                UserId = int.Parse(User.FindFirst("UserId")?.Value ?? "0"),
                EventType = "AdminAction",
                Metadata = $"Cleaned up {deletedCount} unused file records",
                OccurredAt = DateTime.Now
            });

            await _db.SaveChangesAsync();

            return Json(new { 
                success = true, 
                message = $"Đã dọn dẹp {deletedCount} file không tồn tại",
                deletedCount
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Lỗi khi dọn dẹp: " + ex.Message });
        }
    }

    /// <summary>
    /// Danh sách giao dịch thanh toán với phân trang và tìm kiếm
    /// </summary>
    public async Task<IActionResult> Payments(string search = "", string status = "", int page = 1, int pageSize = 20, string sortBy = "CreatedAt", string sortOrder = "desc")
    {
        IQueryable<PaymentTransaction> query = _db.PaymentTransactions
            .Include(p => p.User);

        // Tìm kiếm
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(p => p.User.FullName.Contains(search) || p.User.Email.Contains(search) || p.PayOSTransactionId.Contains(search));
        }

        // Lọc theo trạng thái
        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(p => p.Status == status);
        }

        // Sắp xếp
        switch (sortBy.ToLower())
        {
            case "amount":
                query = sortOrder == "asc" ? query.OrderBy(p => p.Amount) : query.OrderByDescending(p => p.Amount);
                break;
            case "status":
                query = sortOrder == "asc" ? query.OrderBy(p => p.Status) : query.OrderByDescending(p => p.Status);
                break;
            case "user":
                query = sortOrder == "asc" ? query.OrderBy(p => p.User.FullName) : query.OrderByDescending(p => p.User.FullName);
                break;
            default:
                query = sortOrder == "asc" ? query.OrderBy(p => p.CreatedAt) : query.OrderByDescending(p => p.CreatedAt);
                break;
        }

        var totalPayments = await query.CountAsync();
        var payments = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        // Thống kê tổng quan - xử lý null và debug
        var totalRevenue = await _db.PaymentTransactions
            .Where(p => p.Status == "PAID")
            .SumAsync(p => p.Amount);
            
        var totalSuccessful = await _db.PaymentTransactions.CountAsync(p => p.Status == "PAID");
        var totalPending = await _db.PaymentTransactions.CountAsync(p => p.Status == "Pending");
        var totalFailed = await _db.PaymentTransactions.CountAsync(p => p.Status == "Failed");
        var totalRefunded = await _db.PaymentTransactions.CountAsync(p => p.Status == "Refunded");
        
        // Debug: Log để kiểm tra dữ liệu
        var allPayments = await _db.PaymentTransactions.ToListAsync();
        var paymentStatuses = allPayments.Select(p => new { p.TransactionId, p.Status, p.Amount }).ToList();
        
        // Log để debug
        System.Diagnostics.Debug.WriteLine($"Total payments: {allPayments.Count}");
        System.Diagnostics.Debug.WriteLine($"Payment statuses: {string.Join(", ", paymentStatuses.Select(p => $"{p.TransactionId}:{p.Status}:{p.Amount}"))}");

        ViewBag.Search = search;
        ViewBag.Status = status;
        ViewBag.Page = page;
        ViewBag.PageSize = pageSize;
        ViewBag.TotalPages = (int)Math.Ceiling((double)totalPayments / pageSize);
        ViewBag.SortBy = sortBy;
        ViewBag.SortOrder = sortOrder;
        ViewBag.TotalPayments = totalPayments;
        ViewBag.TotalRevenue = totalRevenue;
        ViewBag.TotalSuccessful = totalSuccessful;
        ViewBag.TotalPending = totalPending;
        ViewBag.TotalFailed = totalFailed;
        ViewBag.TotalRefunded = totalRefunded;

        return View(payments);
    }

    /// <summary>
    /// Chi tiết giao dịch thanh toán
    /// </summary>
    public async Task<IActionResult> PaymentDetails(int id)
    {
        var payment = await _db.PaymentTransactions
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.TransactionId == id);

        if (payment == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy giao dịch";
            return RedirectToAction("Payments");
        }

        return View(payment);
    }

    /// <summary>
    /// Hoàn tiền giao dịch
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> RefundPayment(int paymentId, string reason = "")
    {
        var payment = await _db.PaymentTransactions.FindAsync(paymentId);
        if (payment == null)
        {
            return Json(new { success = false, message = "Không tìm thấy giao dịch" });
        }

        if (payment.Status != "PAID")
        {
            return Json(new { success = false, message = "Chỉ có thể hoàn tiền cho giao dịch thành công" });
        }

        try
        {
            // Cập nhật trạng thái giao dịch
            payment.Status = "Refunded";

            // Ghi log audit
            _db.AnalyticsEvents.Add(new AnalyticsEvent
            {
                UserId = int.Parse(User.FindFirst("UserId")?.Value ?? "0"),
                EventType = "AdminAction",
                Metadata = $"Refunded payment {payment.PayOSTransactionId} (Amount: {payment.Amount} VND). Reason: {reason}",
                OccurredAt = DateTime.Now
            });

            await _db.SaveChangesAsync();

            return Json(new { success = true, message = "Đã hoàn tiền thành công" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Lỗi khi hoàn tiền: " + ex.Message });
        }
    }

    /// <summary>
    /// Danh sách subscription với phân trang và tìm kiếm
    /// </summary>
    public async Task<IActionResult> Subscriptions(string search = "", string status = "", int page = 1, int pageSize = 20, string sortBy = "CreatedAt", string sortOrder = "desc")
    {
        IQueryable<Subscription> query = _db.Subscriptions
            .Include(s => s.User);

        // Tìm kiếm
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(s => s.User.FullName.Contains(search) || s.User.Email.Contains(search));
        }

        // Lọc theo trạng thái (dựa vào Status field thay vì IsActive)
        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(s => s.Status == status);
        }

        // Sắp xếp
        switch (sortBy.ToLower())
        {
            case "user":
                query = sortOrder == "asc" ? query.OrderBy(s => s.User.FullName) : query.OrderByDescending(s => s.User.FullName);
                break;
            case "plantype":
                query = sortOrder == "asc" ? query.OrderBy(s => s.PlanType) : query.OrderByDescending(s => s.PlanType);
                break;
            case "enddate":
                query = sortOrder == "asc" ? query.OrderBy(s => s.EndDate) : query.OrderByDescending(s => s.EndDate);
                break;
            default:
                query = sortOrder == "asc" ? query.OrderBy(s => s.CreatedAt) : query.OrderByDescending(s => s.CreatedAt);
                break;
        }

        var totalSubscriptions = await query.CountAsync();
        var subscriptions = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        // Thống kê tổng quan (dựa vào Status field)
        var currentTime = DateTime.Now;
        var activeSubscriptions = await _db.Subscriptions.CountAsync(s => s.Status == "Active" && s.EndDate > currentTime);
        var expiredSubscriptions = await _db.Subscriptions.CountAsync(s => s.EndDate <= currentTime);
        var cancelledSubscriptions = await _db.Subscriptions.CountAsync(s => s.Status == "Cancelled");

        ViewBag.Search = search;
        ViewBag.Status = status;
        ViewBag.Page = page;
        ViewBag.PageSize = pageSize;
        ViewBag.TotalPages = (int)Math.Ceiling((double)totalSubscriptions / pageSize);
        ViewBag.SortBy = sortBy;
        ViewBag.SortOrder = sortOrder;
        ViewBag.TotalSubscriptions = totalSubscriptions;
        ViewBag.ActiveSubscriptions = activeSubscriptions;
        ViewBag.ExpiredSubscriptions = expiredSubscriptions;
        ViewBag.CancelledSubscriptions = cancelledSubscriptions;

        return View(subscriptions);
    }

    /// <summary>
    /// Hủy subscription
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CancelSubscription(int subscriptionId, string reason = "")
    {
        var subscription = await _db.Subscriptions.FindAsync(subscriptionId);
        if (subscription == null)
        {
            return Json(new { success = false, message = "Không tìm thấy subscription" });
        }

        try
        {
            // Cập nhật trạng thái subscription
            subscription.Status = "Cancelled";
            subscription.UpdatedAt = DateTime.Now;

            // Ghi log audit
            _db.AnalyticsEvents.Add(new AnalyticsEvent
            {
                UserId = int.Parse(User.FindFirst("UserId")?.Value ?? "0"),
                EventType = "AdminAction",
                Metadata = $"Cancelled subscription for user {subscription.UserId} (Plan: {subscription.PlanType}). Reason: {reason}",
                OccurredAt = DateTime.Now
            });

            await _db.SaveChangesAsync();

            return Json(new { success = true, message = "Đã hủy subscription thành công" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Lỗi khi hủy subscription: " + ex.Message });
        }
    }

    /// <summary>
    /// Lấy thống kê doanh thu theo thời gian (cho biểu đồ)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetRevenueStatsChart(string range = "month")
    {
        var now = DateTime.UtcNow;
        int days = range == "week" ? 7 : 30;
        var fromDate = now.Date.AddDays(-days + 1);

        var labels = Enumerable.Range(0, days)
            .Select(i => fromDate.AddDays(i).ToString("dd/MM"))
            .ToList();

        // Revenue by day
        var revenueData = await _db.PaymentTransactions
            .Where(p => p.Status == "PAID" && p.CreatedAt >= fromDate)
            .GroupBy(p => p.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Revenue = g.Sum(x => x.Amount) })
            .ToListAsync();
        var revenueSeries = labels.Select(l => {
            var d = DateTime.ParseExact(l, "dd/MM", null);
            return (double)(revenueData.FirstOrDefault(x => x.Date == d)?.Revenue ?? 0);
        }).ToList();

        // Transactions by day
        var transactionData = await _db.PaymentTransactions
            .Where(p => p.CreatedAt >= fromDate)
            .GroupBy(p => p.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync();
        var transactionSeries = labels.Select(l => {
            var d = DateTime.ParseExact(l, "dd/MM", null);
            return transactionData.FirstOrDefault(x => x.Date == d)?.Count ?? 0;
        }).ToList();

        // Success rate by day
        var successData = await _db.PaymentTransactions
            .Where(p => p.CreatedAt >= fromDate)
            .GroupBy(p => p.CreatedAt.Date)
            .Select(g => new { 
                Date = g.Key, 
                Total = g.Count(), 
                Success = g.Count(x => x.Status == "PAID") 
            })
            .ToListAsync();
        var successRateSeries = labels.Select(l => {
            var d = DateTime.ParseExact(l, "dd/MM", null);
            var data = successData.FirstOrDefault(x => x.Date == d);
            return data != null && data.Total > 0 ? Math.Round((double)data.Success / data.Total * 100, 2) : 0.0;
        }).ToList();

        return Json(new { labels, revenueSeries, transactionSeries, successRateSeries });
    }

    /// <summary>
    /// Lấy thống kê subscription theo plan type
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetSubscriptionStats()
    {
        var currentTime = DateTime.Now;
        var stats = await _db.Subscriptions
            .Where(s => s.Status == "Active" && s.EndDate > currentTime)
            .GroupBy(s => s.PlanType)
            .Select(g => new {
                PlanType = g.Key,
                Count = g.Count(),
                Revenue = g.Sum(x => x.Amount)
            })
            .ToListAsync();

        return Json(stats);
    }

    /// <summary>
    /// Danh sách logs hệ thống với phân trang và tìm kiếm
    /// </summary>
    public async Task<IActionResult> SystemLogs(string search = "", string eventType = "", int page = 1, int pageSize = 20, string sortBy = "OccurredAt", string sortOrder = "desc")
    {
        IQueryable<AnalyticsEvent> query = _db.AnalyticsEvents
            .Include(a => a.User);

        // Tìm kiếm
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(a => a.Metadata.Contains(search) || a.User.FullName.Contains(search) || a.User.Email.Contains(search));
        }

        // Lọc theo loại event
        if (!string.IsNullOrEmpty(eventType))
        {
            query = query.Where(a => a.EventType == eventType);
        }

        // Sắp xếp
        switch (sortBy.ToLower())
        {
            case "eventtype":
                query = sortOrder == "asc" ? query.OrderBy(a => a.EventType) : query.OrderByDescending(a => a.EventType);
                break;
            case "user":
                query = sortOrder == "asc" ? query.OrderBy(a => a.User.FullName) : query.OrderByDescending(a => a.User.FullName);
                break;
            default:
                query = sortOrder == "asc" ? query.OrderBy(a => a.OccurredAt) : query.OrderByDescending(a => a.OccurredAt);
                break;
        }

        var totalLogs = await query.CountAsync();
        var logs = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        // Thống kê tổng quan
        var totalEvents = await _db.AnalyticsEvents.CountAsync();
        var adminActions = await _db.AnalyticsEvents.CountAsync(a => a.EventType == "AdminAction");
        var userActions = await _db.AnalyticsEvents.CountAsync(a => a.EventType == "UserAction");
        var systemEvents = await _db.AnalyticsEvents.CountAsync(a => a.EventType == "SystemEvent");
        var errors = await _db.AnalyticsEvents.CountAsync(a => a.EventType == "Error");

        ViewBag.Search = search;
        ViewBag.EventType = eventType;
        ViewBag.Page = page;
        ViewBag.PageSize = pageSize;
        ViewBag.TotalPages = (int)Math.Ceiling((double)totalLogs / pageSize);
        ViewBag.SortBy = sortBy;
        ViewBag.SortOrder = sortOrder;
        ViewBag.TotalLogs = totalLogs;
        ViewBag.TotalEvents = totalEvents;
        ViewBag.AdminActions = adminActions;
        ViewBag.UserActions = userActions;
        ViewBag.SystemEvents = systemEvents;
        ViewBag.Errors = errors;

        return View(logs);
    }

    /// <summary>
    /// Chi tiết log event
    /// </summary>
    public async Task<IActionResult> LogDetails(int id)
    {
        var logEvent = await _db.AnalyticsEvents
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.AnalyticsEventId == id);

        if (logEvent == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy log event";
            return RedirectToAction("SystemLogs");
        }

        return View(logEvent);
    }

    /// <summary>
    /// Xóa logs cũ (dọn dẹp)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CleanupOldLogs(int daysToKeep = 90)
    {
        try
        {
            var cutoffDate = DateTime.Now.AddDays(-daysToKeep);
            var oldLogs = await _db.AnalyticsEvents
                .Where(a => a.OccurredAt < cutoffDate)
                .ToListAsync();

            var deletedCount = oldLogs.Count;
            _db.AnalyticsEvents.RemoveRange(oldLogs);

            // Ghi log cleanup action
            _db.AnalyticsEvents.Add(new AnalyticsEvent
            {
                UserId = int.Parse(User.FindFirst("UserId")?.Value ?? "0"),
                EventType = "AdminAction",
                Metadata = $"Cleaned up {deletedCount} log entries older than {daysToKeep} days",
                OccurredAt = DateTime.Now
            });

            await _db.SaveChangesAsync();

            return Json(new { 
                success = true, 
                message = $"Đã xóa {deletedCount} log cũ (>{daysToKeep} ngày)",
                deletedCount 
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Lỗi khi dọn dẹp logs: " + ex.Message });
        }
    }

    /// <summary>
    /// Lấy thống kê logs theo thời gian (cho biểu đồ)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetLogStatsChart(string range = "week")
    {
        var now = DateTime.UtcNow;
        int days = range == "week" ? 7 : 30;
        var fromDate = now.Date.AddDays(-days + 1);

        var labels = Enumerable.Range(0, days)
            .Select(i => fromDate.AddDays(i).ToString("dd/MM"))
            .ToList();

        // Admin actions by day
        var adminData = await _db.AnalyticsEvents
            .Where(a => a.EventType == "AdminAction" && a.OccurredAt >= fromDate)
            .GroupBy(a => a.OccurredAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync();
        var adminSeries = labels.Select(l => {
            var d = DateTime.ParseExact(l, "dd/MM", null);
            return adminData.FirstOrDefault(x => x.Date == d)?.Count ?? 0;
        }).ToList();

        // User actions by day
        var userData = await _db.AnalyticsEvents
            .Where(a => a.EventType == "UserAction" && a.OccurredAt >= fromDate)
            .GroupBy(a => a.OccurredAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync();
        var userSeries = labels.Select(l => {
            var d = DateTime.ParseExact(l, "dd/MM", null);
            return userData.FirstOrDefault(x => x.Date == d)?.Count ?? 0;
        }).ToList();

        // Errors by day
        var errorData = await _db.AnalyticsEvents
            .Where(a => a.EventType == "Error" && a.OccurredAt >= fromDate)
            .GroupBy(a => a.OccurredAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync();
        var errorSeries = labels.Select(l => {
            var d = DateTime.ParseExact(l, "dd/MM", null);
            return errorData.FirstOrDefault(x => x.Date == d)?.Count ?? 0;
        }).ToList();

        return Json(new { labels, adminSeries, userSeries, errorSeries });
    }

    /// <summary>
    /// Lấy top users theo hoạt động
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetTopActiveUsers()
    {
        var topUsers = await _db.AnalyticsEvents
            .Include(a => a.User)
            .Where(a => a.OccurredAt >= DateTime.Now.AddDays(-30)) // Last 30 days
            .GroupBy(a => new { a.UserId, a.User.FullName, a.User.Email })
            .Select(g => new {
                UserId = g.Key.UserId,
                UserName = g.Key.FullName ?? g.Key.Email,
                ActivityCount = g.Count(),
                LastActivity = g.Max(x => x.OccurredAt)
            })
            .OrderByDescending(x => x.ActivityCount)
            .Take(10)
            .ToListAsync();

        return Json(topUsers);
    }

    /// <summary>
    /// Export logs to CSV
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ExportLogs(string eventType = "", DateTime? fromDate = null, DateTime? toDate = null)
    {
        try
        {
            IQueryable<AnalyticsEvent> query = _db.AnalyticsEvents.Include(a => a.User);

            // Apply filters
            if (!string.IsNullOrEmpty(eventType))
            {
                query = query.Where(a => a.EventType == eventType);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(a => a.OccurredAt >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(a => a.OccurredAt <= toDate.Value);
            }

            var logs = await query.OrderByDescending(a => a.OccurredAt).Take(10000).ToListAsync(); // Limit to 10k records

            // Generate CSV content
            var csv = new System.Text.StringBuilder();
            csv.AppendLine("EventId,EventType,UserId,UserName,UserEmail,Metadata,OccurredAt");

            foreach (var log in logs)
            {
                csv.AppendLine($"{log.AnalyticsEventId},{log.EventType},{log.UserId},{log.User?.FullName ?? ""},{log.User?.Email ?? ""},{log.Metadata?.Replace(",", ";") ?? ""},{log.OccurredAt:yyyy-MM-dd HH:mm:ss}");
            }

            var fileName = $"system_logs_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());

            // Log export action
            _db.AnalyticsEvents.Add(new AnalyticsEvent
            {
                UserId = int.Parse(User.FindFirst("UserId")?.Value ?? "0"),
                EventType = "AdminAction",
                Metadata = $"Exported {logs.Count} log entries to CSV (EventType: {eventType ?? "All"}, From: {fromDate?.ToString("yyyy-MM-dd") ?? "N/A"}, To: {toDate?.ToString("yyyy-MM-dd") ?? "N/A"})",
                OccurredAt = DateTime.Now
            });
            await _db.SaveChangesAsync();

            return File(bytes, "text/csv", fileName);
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Lỗi khi export logs: " + ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetDashboardChartData(string range = "month")
    {
        var now = DateTime.UtcNow;
        int days = range == "week" ? 7 : 30;
        var fromDate = now.Date.AddDays(-days + 1);

        // Chuẩn bị mảng ngày
        var labels = Enumerable.Range(0, days)
            .Select(i => fromDate.AddDays(i).ToString("dd/MM"))
            .ToList();

        // User đăng ký mới theo ngày
        var userData = await _db.Users
            .Where(u => u.CreatedAt >= fromDate)
            .GroupBy(u => u.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync();
        var userSeries = labels.Select(l => {
            var d = DateTime.ParseExact(l, "dd/MM", null);
            return userData.FirstOrDefault(x => x.Date == d)?.Count ?? 0;
        }).ToList();

        // Nhóm chat mới theo ngày
        var groupData = await _db.ChatGroups
            .Where(g => g.CreatedAt >= fromDate)
            .GroupBy(g => g.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync();
        var groupSeries = labels.Select(l => {
            var d = DateTime.ParseExact(l, "dd/MM", null);
            return groupData.FirstOrDefault(x => x.Date == d)?.Count ?? 0;
        }).ToList();

        // Doanh thu theo ngày
        var revenueData = await _db.PaymentTransactions
            .Where(t => t.Status == "Success" && t.CreatedAt >= fromDate)
            .GroupBy(t => t.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Total = g.Sum(x => x.Amount) })
            .ToListAsync();
        var revenueSeries = labels.Select(l => {
            var d = DateTime.ParseExact(l, "dd/MM", null);
            return revenueData.FirstOrDefault(x => x.Date == d)?.Total ?? 0;
        }).ToList();

        // Tin nhắn gửi theo ngày
        var messageData = await _db.Messages
            .Where(m => m.SentAt >= fromDate)
            .GroupBy(m => m.SentAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync();
        var messageSeries = labels.Select(l => {
            var d = DateTime.ParseExact(l, "dd/MM", null);
            return messageData.FirstOrDefault(x => x.Date == d)?.Count ?? 0;
        }).ToList();

        return Json(new {
            labels,
            userSeries,
            groupSeries,
            revenueSeries,
            messageSeries
        });
    }

    /// <summary>
    /// Trang Analytics - Thống kê chi tiết và biểu đồ
    /// </summary>
    public async Task<IActionResult> Analytics()
    {
        var now = DateTime.UtcNow;
        var today = now.Date;
        var weekAgo = now.AddDays(-7);
        var monthAgo = now.AddMonths(-1);
        var yearAgo = now.AddYears(-1);

        // User Analytics
        ViewBag.TotalUsers = await _db.Users.CountAsync();
        ViewBag.ActiveUsers = await _db.Users.CountAsync(u => u.LastLoginAt >= weekAgo);
        ViewBag.NewUsersToday = await _db.Users.CountAsync(u => u.CreatedAt >= today);
        ViewBag.NewUsersWeek = await _db.Users.CountAsync(u => u.CreatedAt >= weekAgo);
        ViewBag.NewUsersMonth = await _db.Users.CountAsync(u => u.CreatedAt >= monthAgo);
        ViewBag.PremiumUsers = await _db.Users.CountAsync(u => u.IsPremium);

        // Content Analytics
        ViewBag.TotalGroups = await _db.ChatGroups.CountAsync();
        ViewBag.TotalMessages = await _db.Messages.CountAsync();
        ViewBag.TotalMeetings = await _db.VideoRooms.CountAsync();
        ViewBag.TotalRecordings = await _db.Recordings.CountAsync();
        ViewBag.TotalFiles = await _db.Media.CountAsync();
        ViewBag.TotalQuizzes = await _db.Quizzes.CountAsync();

        // Financial Analytics
        ViewBag.TotalRevenue = await _db.PaymentTransactions.Where(p => p.Status == "PAID").SumAsync(p => p.Amount);
        ViewBag.RevenueToday = await _db.PaymentTransactions.Where(p => p.Status == "PAID" && p.CreatedAt >= today).SumAsync(p => p.Amount);
        ViewBag.RevenueWeek = await _db.PaymentTransactions.Where(p => p.Status == "PAID" && p.CreatedAt >= weekAgo).SumAsync(p => p.Amount);
        ViewBag.RevenueMonth = await _db.PaymentTransactions.Where(p => p.Status == "PAID" && p.CreatedAt >= monthAgo).SumAsync(p => p.Amount);
        ViewBag.TotalTransactions = await _db.PaymentTransactions.CountAsync();
        ViewBag.SuccessfulTransactions = await _db.PaymentTransactions.CountAsync(p => p.Status == "PAID");

        // System Analytics
        ViewBag.TotalEvents = await _db.AnalyticsEvents.CountAsync();
        ViewBag.AdminActions = await _db.AnalyticsEvents.CountAsync(a => a.EventType == "AdminAction");
        ViewBag.UserActions = await _db.AnalyticsEvents.CountAsync(a => a.EventType == "UserAction");
        ViewBag.SystemEvents = await _db.AnalyticsEvents.CountAsync(a => a.EventType == "SystemEvent");
        ViewBag.ErrorEvents = await _db.AnalyticsEvents.CountAsync(a => a.EventType == "Error");

        // Storage Analytics
        ViewBag.TotalStorage = await _db.Recordings.SumAsync(r => r.FileSize);
        ViewBag.RecordingStorage = await _db.Recordings.SumAsync(r => r.FileSize);
        ViewBag.FileStorage = 0; // Media model doesn't have FileSize property

        // Recent Activity
        var recentActivity = await _db.AnalyticsEvents
            .Include(a => a.User)
            .OrderByDescending(a => a.OccurredAt)
            .Take(10)
            .ToListAsync();
        ViewBag.RecentActivity = recentActivity;

        // Top Users by Activity
        var topUsers = await _db.AnalyticsEvents
            .Include(a => a.User)
            .GroupBy(a => a.UserId)
            .Select(g => new {
                User = g.First().User,
                ActivityCount = g.Count(),
                LastActivity = g.Max(x => x.OccurredAt)
            })
            .OrderByDescending(x => x.ActivityCount)
            .Take(10)
            .ToListAsync();
        ViewBag.TopUsers = topUsers;

        return View();
    }

} 