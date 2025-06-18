using Zela.Models;
using Microsoft.EntityFrameworkCore;
using Zela.DbContext;
using Zela.ViewModels;

namespace Zela.Services;

public class ChatService : IChatService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IFileUploadService _fileUploadService;

    public ChatService(ApplicationDbContext dbContext, IFileUploadService fileUploadService)
    {
        _dbContext = dbContext;
        _fileUploadService = fileUploadService;
    }

    public async Task<MessageViewModel> SendMessageAsync(int senderId, int recipientId, string content, IFormFile? file = null)
    {
        var msg = new Message
        {
            SenderId = senderId,
            RecipientId = recipientId,
            Content = content ?? "",
            SentAt = DateTime.Now,
            IsEdited = false,
            Media = new List<Media>()
        };

        if (file != null && file.Length > 0)
        {
            Console.WriteLine($"[ChatService] Uploading file: {file.FileName}, type: {file.ContentType}, size: {file.Length}");
            var url = await _fileUploadService.UploadAsync(file, "chat");
            Console.WriteLine($"[ChatService] Uploaded file url: {url}");
            msg.Media.Add(new Media
            {
                MediaType = file.ContentType,
                Url = url
            });
        }

        _dbContext.Messages.Add(msg);
        await _dbContext.SaveChangesAsync();

        var sender = await _dbContext.Users.FindAsync(senderId);
        return new MessageViewModel
        {
            MessageId = msg.MessageId,
            SenderId = senderId,
            RecipientId = recipientId,
            SenderName = sender?.FullName ?? "Unknown",
            AvatarUrl = sender?.AvatarUrl ?? "",
            Content = msg.Content ?? "",
            SentAt = msg.SentAt,
            IsMine = true,
            IsEdited = false,
            Media = msg.Media.Select(md => new MediaViewModel
            {
                Url = md.Url,
                MediaType = md.MediaType
            }).ToList()
        };
    }

    // Lấy bạn bè đã kết bạn (status = accepted)
    public async Task<List<FriendViewModel>> GetFriendListAsync(int userId)
    {
        // Giả sử bạn có bảng Friendships (UserId1, UserId2, StatusId)
        var friendIds = await _dbContext.Friendships
            .Where(f => (f.UserId1 == userId || f.UserId2 == userId) && f.StatusId == 2) // 2 = Accepted
            .Select(f => f.UserId1 == userId ? f.UserId2 : f.UserId1)
            .ToListAsync();

        var friends = await _dbContext.Users
            .Where(u => friendIds.Contains(u.UserId))
            .Select(u => new FriendViewModel
            {
                UserId = u.UserId,
                FullName = u.FullName,
                AvatarUrl = u.AvatarUrl,
                IsOnline = u.LastLoginAt > DateTime.Now.AddMinutes(-3),
                LastMessage = _dbContext.Messages
                    .Where(m =>
                        (m.SenderId == userId && m.RecipientId == u.UserId) ||
                        (m.SenderId == u.UserId && m.RecipientId == userId))
                    .OrderByDescending(m => m.SentAt)
                    .Select(m => m.Content)
                    .FirstOrDefault(),
                LastTime = _dbContext.Messages
                    .Where(m =>
                        (m.SenderId == userId && m.RecipientId == u.UserId) ||
                        (m.SenderId == u.UserId && m.RecipientId == userId))
                    .OrderByDescending(m => m.SentAt)
                    .Select(m => m.SentAt.ToString("HH:mm"))
                    .FirstOrDefault()
            })
            .ToListAsync();

        return friends;
    }

    // Lấy lịch sử chat 1-1
    public async Task<List<MessageViewModel>> GetMessagesAsync(int userId, int friendId)
    {
        return await _dbContext.Messages
            .Include(m => m.Sender)
            .Include(m => m.Recipient)
            .Include(m => m.Media)
            .Where(m =>
                (m.SenderId == userId && m.RecipientId == friendId) ||
                (m.SenderId == friendId && m.RecipientId == userId))
            .OrderBy(m => m.SentAt)
            .Select(m => new MessageViewModel
            {
                MessageId = m.MessageId,
                SenderId = m.SenderId,
                RecipientId = m.RecipientId ?? 0,
                SenderName = m.Sender.FullName,
                AvatarUrl = m.Sender.AvatarUrl,
                Content = m.Content,
                SentAt = m.SentAt,
                IsMine = m.SenderId == userId,
                IsEdited = m.IsEdited,
                Media = m.Media.Select(md => new MediaViewModel
                {
                    Url = md.Url,
                    MediaType = md.MediaType
                }).ToList()
            })
            .ToListAsync();
    }

    // Lưu message mới
    public async Task<MessageViewModel> SaveMessageAsync(int senderId, int recipientId, string content)
    {
        var sender = await _dbContext.Users.FindAsync(senderId);

        var message = new Message
        {
            SenderId = senderId,
            RecipientId = recipientId,
            SentAt = DateTime.Now,
            Content = content,
            IsEdited = false
        };
        _dbContext.Messages.Add(message);
        await _dbContext.SaveChangesAsync();

        return new MessageViewModel
        {
            MessageId = message.MessageId,
            SenderId = senderId,
            RecipientId = recipientId,
            SenderName = sender.FullName,
            AvatarUrl = sender.AvatarUrl,
            Content = content,
            SentAt = message.SentAt,
            IsMine = true,
            IsEdited = false
        };
    }
    
    // Group chat methods
    public async Task<GroupMessageViewModel> SendGroupMessageAsync(int senderId, int groupId, string content)
    {
        var sender = await _dbContext.Users.FindAsync(senderId);
        var group = await _dbContext.ChatGroups.FindAsync(groupId);

        if (sender == null)
            throw new Exception("Sender not found");

        if (group == null)
            throw new Exception("Group not found");

        var message = new Message
        {
            SenderId = senderId,
            GroupId = groupId,
            Content = content,
            SentAt = DateTime.Now,
            IsEdited = false
        };

        _dbContext.Messages.Add(message);
        await _dbContext.SaveChangesAsync();

        return new GroupMessageViewModel
        {
            MessageId = (int)message.MessageId,
            SenderId = senderId,
            GroupId = groupId,
            SenderName = sender.FullName,
            AvatarUrl = sender.AvatarUrl,
            Content = content,
            SentAt = message.SentAt,
            IsMine = false, // Will be determined by client based on senderId
            IsEdited = false
        };
    }

    public async Task<ChatGroup> CreateGroupAsync(int creatorId, string name, string description)
    {
        // Validate input
        if (string.IsNullOrEmpty(name))
            throw new ArgumentException("Tên nhóm không được để trống");

        if (name.Length > 100)
            throw new ArgumentException("Tên nhóm không được vượt quá 100 ký tự");

        if (description?.Length > 50)
            throw new ArgumentException("Mô tả không được vượt quá 50 ký tự");

        // Check if creator exists
        var creator = await _dbContext.Users.FindAsync(creatorId);
        if (creator == null)
            throw new ArgumentException("Người tạo nhóm không tồn tại");

        var group = new ChatGroup
        {
            Name = name,
            Description = description,
            CreatorId = creatorId,
            CreatedAt = DateTime.Now,
            IsOpen = true
        };

        try
        {
            _dbContext.ChatGroups.Add(group);
            await _dbContext.SaveChangesAsync();

            // Add creator as first member and moderator
            var member = new GroupMember
            {
                GroupId = group.GroupId,
                UserId = creatorId,
                IsModerator = true,
                JoinedAt = DateTime.Now
            };

            _dbContext.GroupMembers.Add(member);
            await _dbContext.SaveChangesAsync();

            return group;
        }
        catch (Exception ex)
        {
            // Log the error
            throw new Exception("Không thể tạo nhóm: " + ex.Message);
        }
    }

    public async Task AddMemberToGroupAsync(int groupId, int userId)
    {
        var existingMember = await _dbContext.GroupMembers
            .FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == userId);

        if (existingMember != null)
            return;

        var member = new GroupMember
        {
            GroupId = groupId,
            UserId = userId,
            IsModerator = false,
            JoinedAt = DateTime.Now
        };

        _dbContext.GroupMembers.Add(member);
        await _dbContext.SaveChangesAsync();
    }

    public async Task RemoveMemberFromGroupAsync(int groupId, int userId)
    {
        var member = await _dbContext.GroupMembers
            .FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == userId);

        if (member != null)
        {
            _dbContext.GroupMembers.Remove(member);
            await _dbContext.SaveChangesAsync();
        }
    }

    public async Task<List<GroupMessageViewModel>> GetGroupMessagesAsync(int groupId)
    {
        var messages = await _dbContext.Messages
            .Include(m => m.Sender)
            .Where(m => m.GroupId == groupId)
            .OrderBy(m => m.SentAt)
            .ToListAsync();

        return messages.Select(m => new GroupMessageViewModel
        {
            MessageId = (int)m.MessageId,
            SenderId = (int)m.SenderId,
            GroupId = (int)(m.GroupId ?? 0),
            SenderName = m.Sender?.FullName ?? "Unknown",
            AvatarUrl = m.Sender?.AvatarUrl ?? "/images/default-avatar.jpeg",
            Content = m.Content,
            SentAt = m.SentAt,
            IsMine = false, // Will be set by client
            IsEdited = m.IsEdited
        }).ToList();
    }

    public async Task<List<GroupViewModel>> GetUserGroupsAsync(int userId)
    {
        var groupMembers = await _dbContext.GroupMembers
            .Include(gm => gm.ChatGroup)
                .ThenInclude(g => g.Messages.OrderByDescending(m => m.SentAt).Take(1))
            .Include(gm => gm.ChatGroup)
                .ThenInclude(g => g.Creator)
            .Include(gm => gm.ChatGroup)
                .ThenInclude(g => g.Members)
            .Where(gm => gm.UserId == userId)
            .ToListAsync();

        return groupMembers.Select(gm => new GroupViewModel
        {
            GroupId = (int)gm.ChatGroup.GroupId,
            Name = gm.ChatGroup.Name,
            Description = gm.ChatGroup.Description,
            CreatedAt = gm.ChatGroup.CreatedAt,
            CreatorName = gm.ChatGroup.Creator?.FullName ?? "Unknown",
            MemberCount = gm.ChatGroup.Members?.Count ?? 0,
            LastMessage = gm.ChatGroup.Messages?.FirstOrDefault()?.Content ?? "Chưa có tin nhắn nào",
            LastTime = gm.ChatGroup.Messages?.FirstOrDefault()?.SentAt.ToString("HH:mm") ?? ""
        }).ToList();
    }

    public async Task<ChatGroup> GetGroupDetailsAsync(int groupId)
    {
        return await _dbContext.ChatGroups
            .Include(g => g.Members)
                .ThenInclude(m => m.User)
            .Include(g => g.Creator)
            .Include(g => g.Messages)
                .ThenInclude(m => m.Sender)
            .FirstOrDefaultAsync(g => g.GroupId == groupId);
    }

    public async Task UpdateGroupAsync(int groupId, string name, string description)
    {
        var group = await _dbContext.ChatGroups.FindAsync(groupId);
        if (group == null)
            throw new Exception("Group not found");

        group.Name = name;
        group.Description = description;
        
        await _dbContext.SaveChangesAsync();
    }

    public async Task DeleteGroupAsync(int groupId)
    {
        var group = await _dbContext.ChatGroups
            .Include(g => g.Members)
            .Include(g => g.Messages)
            .FirstOrDefaultAsync(g => g.GroupId == groupId);

        if (group == null)
            throw new Exception("Group not found");

        // Xóa tất cả tin nhắn trong nhóm
        _dbContext.Messages.RemoveRange(group.Messages);
        
        // Xóa tất cả thành viên
        _dbContext.GroupMembers.RemoveRange(group.Members);
        
        // Xóa nhóm
        _dbContext.ChatGroups.Remove(group);
        
        await _dbContext.SaveChangesAsync();
    }

    public async Task<List<UserViewModel>> SearchUsersAsync(string searchTerm, int currentUserId)
    {
        return await _dbContext.Users
            .Where(u => u.UserId != currentUserId && // Không hiển thị người dùng hiện tại
                       (u.FullName.Contains(searchTerm) || u.Email.Contains(searchTerm)))
            .Select(u => new UserViewModel
            {
                UserId = (int)u.UserId,
                FullName = u.FullName,
                Email = u.Email,
                AvatarUrl = u.AvatarUrl
            })
            .Take(10) // Giới hạn kết quả tìm kiếm
            .ToListAsync();
    }

    public async Task ToggleModeratorAsync(int groupId, int userId)
    {
        var member = await _dbContext.GroupMembers
            .FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == userId);

        if (member == null)
            throw new Exception("Không tìm thấy thành viên trong nhóm");

        member.IsModerator = !member.IsModerator;
        await _dbContext.SaveChangesAsync();
    }
}
