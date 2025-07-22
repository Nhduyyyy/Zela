using System.Text;
using Zela.Models;
using Microsoft.EntityFrameworkCore;
using Zela.DbContext;
using Zela.Enum;
using Zela.ViewModels;
using Zela.Services.Interface;

namespace Zela.Services;

public class ChatService : IChatService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IFileUploadService _fileUploadService;
    private readonly INotificationService _notificationService;

    public ChatService(ApplicationDbContext dbContext, IFileUploadService fileUploadService, INotificationService notificationService)
    {
        _dbContext = dbContext;
        _fileUploadService = fileUploadService;
        _notificationService = notificationService;
    }

    public async Task<MessageViewModel> SendMessageAsync(int senderId, int recipientId, string content,
        List<IFormFile>? files = null)
    {
        try
        {
            var msg = new Message
            {
                SenderId = senderId,
                RecipientId = recipientId,
                Content = content ?? "",
                SentAt = DateTime.Now,
                IsEdited = false,
                Media = new List<Media>(),
                MessageStatus = MessageStatus.Sent // Default MessageStatus value = sent
            };

            MessageType notificationType = MessageType.Text;
            string notificationContent = "";

            if (files != null && files.Count > 0)
            {
                foreach (var file in files)
                {
                    if (file != null && file.Length > 0)
                    {
                        var url = await _fileUploadService.UploadAsync(file, "chat");
                        msg.Media.Add(new Media
                        {
                            MediaType = file.ContentType,
                            Url = url,
                            FileName = file.FileName,
                            UploadedAt = DateTime.Now
                        });
                        // Determine notification type and content
                        if (file.ContentType.StartsWith("image"))
                        {
                            notificationType = MessageType.Image;
                            notificationContent = "[Hình ảnh]";
                        }
                        else
                        {
                            notificationType = MessageType.File;
                            notificationContent = file.FileName.Length > 20 ? file.FileName.Substring(0, 20) + "..." : file.FileName;
                        }
                    }
                }
            }
            else
            {
                notificationType = MessageType.Text;
                notificationContent = (content ?? "").Length > 50 ? (content ?? "").Substring(0, 50) + "..." : (content ?? "");
            }

            _dbContext.Messages.Add(msg);
            await _dbContext.SaveChangesAsync();

            // Create notification for recipient
            if (recipientId != senderId)
            {
                await _notificationService.CreateNotificationAsync(senderId, recipientId, notificationContent, notificationType);
            }

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
                    MediaType = md.MediaType,
                    FileName = md.FileName
                }).ToList()
            };
        }
        catch (Exception ex)
        {
            // Log lỗi chi tiết
            Console.WriteLine($"Error in SendMessageAsync: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    // /// Lấy danh sách bạn bè đã chấp nhận của user, kèm thông tin cơ bản và tin nhắn mới nhất.
    // /// </summary>
    public async Task<List<FriendViewModel>> GetFriendListAsync(int userId)
    {
        // Bước 1: Lấy danh sách ID của bạn bè (StatusId == 2 nghĩa là đã Accepted)
        var friendIds = await _dbContext.Friendships
            // Lọc các mối quan hệ mà userId là một bên
            .Where(f => (f.UserId1 == userId || f.UserId2 == userId) && f.StatusId == 2) // 2 = Accepted
            // Chọn ID của bên còn lại trong mỗi quan hệ
            .Select(f => f.UserId1 == userId ? f.UserId2 : f.UserId1)
            // Thực thi truy vấn và lấy về List<int>
            .ToListAsync();

        // Bước 2: Lấy thông tin chi tiết của các user trong danh sách friendIds
        var friends = await _dbContext.Users
            // Chỉ lấy các User có UserId nằm trong friendIds
            .Where(u => friendIds.Contains(u.UserId))
            // Map mỗi User thành FriendViewModel
            .Select(u => new FriendViewModel
            {
                // Cột UserId của friend
                UserId = u.UserId,
                // Tên đầy đủ
                FullName = u.FullName,
                // URL avatar
                AvatarUrl = u.AvatarUrl,
                // Đánh dấu online nếu LastLoginAt trong vòng 3 phút gần nhất
                IsOnline = u.LastLoginAt > DateTime.Now.AddMinutes(-3),

                // Lấy nội dung tin nhắn mới nhất giữa userId và u.UserId
                LastMessage = _dbContext.Messages
                    // Chọn tin nhắn mà sender/recipient là cặp userId và friendId
                    .Where(m =>
                        /* Case A: bạn gửi cho friend
                       - SenderId phải đúng userId (bạn)
                       - RecipientId phải đúng u.UserId (friend)
                       Cả hai điều kiện trong dấu ngoặc đơn phải cùng đúng */
                        (m.SenderId == userId && m.RecipientId == u.UserId) ||
                        /* Case B: friend gửi cho bạn
                        - SenderId phải đúng u.UserId
                        - RecipientId phải đúng userId
                        Cả hai điều kiện trong dấu ngoặc đơn phải cùng đúng */
                        (m.SenderId == u.UserId && m.RecipientId == userId))
                    // Sau khi lọc, sắp xếp giảm dần theo thời gian gửi (SentAt)
                    .OrderByDescending(m => m.SentAt)
                    // Chỉ lấy phần nội dung tin nhắn
                    .Select(m => m.Content)
                    // Lấy tin nhắn đầu tiên trong kết quả (mới nhất) hoặc null nếu không có
                    .FirstOrDefault(),

                // Lấy thời gian gửi của tin nhắn mới nhất, format "HH:mm"
                LastTime = _dbContext.Messages
                    .Where(m =>
                        (m.SenderId == userId && m.RecipientId == u.UserId) ||
                        (m.SenderId == u.UserId && m.RecipientId == userId))
                    .OrderByDescending(m => m.SentAt)
                    .Select(m => m.SentAt.ToString("HH:mm")) // chuyển DateTime thành string giờ:phút
                    .FirstOrDefault() // lấy giá trị đầu tiên hoặc null
            })
            .ToListAsync();

        return friends;
    }

    /// <summary>
    /// Lấy lịch sử chat 1-1 giữa user và friend, bao gồm tin nhắn text, media và sticker.
    /// </summary>
    public async Task<List<MessageViewModel>> GetMessagesAsync(int userId, int friendId)
    {
        // Truy vấn bảng Messages với eager loading các navigation properties
        var message = await _dbContext.Messages
            // Eager-load các bảng ngoại liên quan (navigation properties) dựa trên khóa ngoại:
            // - SenderId => bảng Users (đối tượng Sender)
            // - RecipientId => bảng Users (đối tượng Recipient)
            // - MessageId => bảng Media (collection Media)
            // - MessageId => bảng Sticker (collection Sticker)
            .Include(m => m.Sender)    // Tải kèm thông tin người gửi qua FK SenderId
            .Include(m => m.Recipient) // Tải kèm thông tin người nhận qua FK RecipientId
            .Include(m => m.Media)     // Tải kèm danh sách media đính kèm (qua FK MessageId)
            .Include(m => m.Sticker)   // Tải kèm danh sách sticker gắn kèm (qua FK MessageId)
            // Lọc tin nhắn giữa user và friend theo hai chiều:
            // + Bạn gửi cho friend (SenderId == userId && RecipientId == friendId)
            // + Friend gửi cho bạn (SenderId == friendId && RecipientId == userId)
            .Where(m =>
                (m.SenderId == userId && m.RecipientId == friendId) ||
                (m.SenderId == friendId && m.RecipientId == userId))
            // Sắp xếp theo thời gian gửi tăng dần (cũ tới mới) để hiển thị hội thoại đúng thứ tự
            .OrderBy(m => m.SentAt)
            // Thực thi truy vấn bất đồng bộ, trả về danh sách MessageViewModel
            .ToListAsync();
        
        // Cập nhật các tin nhắn "đã gửi" thành "đã nhận"
        var deliveredMessages = message
            .Where(m => m.RecipientId == userId && m.MessageStatus == MessageStatus.Sent)
            .ToList();

        foreach (var msg in deliveredMessages)
        {
            msg.MessageStatus = MessageStatus.Delivered;
        }

        await _dbContext.SaveChangesAsync();
        
        // Ánh xạ Message entity thành MessageViewModel để trả về client
        return message.Select(m => new MessageViewModel
            {
                MessageId = m.MessageId,
                SenderId = m.SenderId,
                RecipientId = m.RecipientId ?? 0,
                SenderName = m.Sender.FullName, // Được load sẵn qua Include
                AvatarUrl = m.Sender.AvatarUrl, // Được load sẵn qua Include
                Content = m.Content,
                SentAt = m.SentAt,
                IsMine = m.SenderId == userId,
                IsEdited = m.IsEdited,

                // Chuyển từng Media entity thành MediaViewModel
                Media = m.Media.Select(md => new MediaViewModel
                {
                    Url = md.Url,
                    MediaType = md.MediaType,
                    FileName = md.FileName
                }).ToList(),
                // Lấy sticker đầu tiên nếu có, sử dụng navigation property đã include:
                StickerUrl = m.Sticker.FirstOrDefault() != null ? m.Sticker.FirstOrDefault().StickerUrl : null,
                StickerType = m.Sticker.FirstOrDefault() != null ? m.Sticker.FirstOrDefault().StickerType : null,
                Status = m.MessageStatus
            })
            .ToList();
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

    // Tìm user theo ID
    public async Task<User?> FindUserByIdAsync(int userId)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.UserId == userId);

        return user;
    }

    // Group chat methods
    public async Task<GroupMessageViewModel> SendGroupMessageAsync(int senderId, int groupId, string content, List<IFormFile>? files = null, long? replyToMessageId = null)
    {
        var sender = await _dbContext.Users.FindAsync(senderId);
        var group = await _dbContext.ChatGroups.FindAsync(groupId);

        if (sender == null)
            throw new Exception("Sender not found");

        if (group == null)
            throw new Exception("Group not found");

        string replyContent = null;
        string replySenderName = null;
        if (replyToMessageId.HasValue)
        {
            var replyMsg = await _dbContext.Messages
                .Include(m => m.Sender)
                .FirstOrDefaultAsync(m => m.MessageId == replyToMessageId.Value);

            if (replyMsg != null)
            {
                replyContent = replyMsg.Content;
                replySenderName = replyMsg.Sender?.FullName ?? "ai đó";
            }
        }

        var message = new Message
        {
            SenderId = senderId,
            GroupId = groupId,
            Content = content ?? "",
            SentAt = DateTime.Now,
            IsEdited = false,
            Media = new List<Media>(),
            ReplyToMessageId = replyToMessageId
        };

        // Handle file uploads if provided
        if (files != null && files.Count > 0)
        {
            Console.WriteLine($"[ChatService] Uploading {files.Count} file(s) for group chat");
            
            foreach (var file in files)
            {
                if (file != null && file.Length > 0)
                {
                    Console.WriteLine(
                        $"[ChatService] Uploading file: {file.FileName}, type: {file.ContentType}, size: {file.Length}");
                    var url = await _fileUploadService.UploadAsync(file, "group-chat");
                    Console.WriteLine($"[ChatService] Uploaded file url: {url}");
                    message.Media.Add(new Media
                    {
                        MediaType = file.ContentType,
                        Url = url,
                        FileName = file.FileName,
                        UploadedAt = DateTime.Now
                    });
                }
            }
        }

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
            IsEdited = false,
            Media = message.Media.Select(md => new MediaViewModel
            {
                Url = md.Url,
                MediaType = md.MediaType,
                FileName = md.FileName
            }).ToList(),
            ReplyToMessageId = replyToMessageId,
            ReplyToMessageContent = replyContent,
            ReplyToMessageSenderName = replySenderName
        };
    }

    public async Task<ChatGroup> CreateGroupAsync(int creatorId, string name, string description, string avatarUrl, string password, List<int> friendIds)
    {
        // Validate input
        if (string.IsNullOrEmpty(name))
            throw new ArgumentException("Tên nhóm không được để trống");
        if (name.Length > 100)
            throw new ArgumentException("Tên nhóm không được vượt quá 100 ký tự");
        if (description?.Length > 50)
            throw new ArgumentException("Mô tả không được vượt quá 50 ký tự");
        if (friendIds == null || friendIds.Count < 2)
            throw new ArgumentException("Bạn phải chọn ít nhất 2 người bạn để tạo nhóm.");

        // Không cho phép trùng creatorId
        friendIds = friendIds.Distinct().Where(fid => fid != creatorId).ToList();
        if (friendIds.Count < 2)
            throw new ArgumentException("Bạn phải chọn ít nhất 2 người bạn khác bạn để tạo nhóm.");

        // Kiểm tra tất cả friendIds có tồn tại
        var validFriends = await _dbContext.Users.Where(u => friendIds.Contains(u.UserId)).Select(u => u.UserId).ToListAsync();
        if (validFriends.Count != friendIds.Count)
            throw new ArgumentException("Có bạn bè không tồn tại hoặc đã bị xóa.");

        using var transaction = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            var group = new ChatGroup
            {
                Name = name,
                Description = description ?? "",
                CreatorId = creatorId,
                CreatedAt = DateTime.Now,
                IsOpen = true,
                AvatarUrl = string.IsNullOrEmpty(avatarUrl) ? "/images/default-group-avatar.png" : avatarUrl,
                Password = string.IsNullOrEmpty(password) ? null : password,
                Members = new List<GroupMember>(),
                Messages = new List<Message>()
            };
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

            // Add selected friends as members
            foreach (var fid in friendIds)
            {
                var friendMember = new GroupMember
                {
                    GroupId = group.GroupId,
                    UserId = fid,
                    IsModerator = false,
                    JoinedAt = DateTime.Now
                };
                _dbContext.GroupMembers.Add(friendMember);
            }
            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
            return group;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            // Log the error
            Console.WriteLine($"Error in CreateGroupAsync: {ex.Message}");
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
            .Include(m => m.Media)
            .Include(m => m.Sticker)
            .Where(m => m.GroupId == groupId)
            .OrderBy(m => m.SentAt)
            .ToListAsync();

        // Bổ sung lấy nội dung tin nhắn được reply nếu có
        var messageDict = messages.ToDictionary(m => m.MessageId, m => m);

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
            IsEdited = m.IsEdited,
            Media = m.Media.Select(md => new MediaViewModel
            {
                Url = md.Url,
                MediaType = md.MediaType,
                FileName = md.FileName
            }).ToList(),
            ReplyToMessageId = m.ReplyToMessageId,
            ReplyToMessageContent = m.ReplyToMessageId.HasValue && messageDict.ContainsKey(m.ReplyToMessageId.Value)
                ? messageDict[m.ReplyToMessageId.Value].Content
                : null,
            ReplyToMessageSenderName = m.ReplyToMessageId.HasValue && messageDict.ContainsKey(m.ReplyToMessageId.Value)
                ? messageDict[m.ReplyToMessageId.Value].Sender.FullName
                : null,
            StickerUrl = m.Sticker.FirstOrDefault() != null ? m.Sticker.FirstOrDefault().StickerUrl : null,
            StickerType = m.Sticker.FirstOrDefault() != null ? m.Sticker.FirstOrDefault().StickerType : null
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
            AvatarUrl = gm.ChatGroup.AvatarUrl ?? "/images/default-group-avatar.png",
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
    
    public async Task<MessageReactionViewModel> AddReactionAsync(long messageId, int userId, string reactionType)
    {
        // Kiểm tra xem user đã reaction chưa
        var existingReaction = await _dbContext.MessageReactions
            .FirstOrDefaultAsync(r => r.MessageId == messageId && r.UserId == userId);

        if (existingReaction != null)
        {
            // Nếu đã reaction cùng loại, xóa reaction
            if (existingReaction.ReactionType == reactionType)
            {
                _dbContext.MessageReactions.Remove(existingReaction);
                await _dbContext.SaveChangesAsync();
                return null; // Trả về null để client biết đã xóa
            }
            else
            {
                // Nếu reaction khác loại, cập nhật
                existingReaction.ReactionType = reactionType;
                existingReaction.CreatedAt = DateTime.Now;
                await _dbContext.SaveChangesAsync();
                
                var user = await _dbContext.Users.FindAsync(userId);
                return new MessageReactionViewModel
                {
                    ReactionId = existingReaction.ReactionId,
                    MessageId = existingReaction.MessageId,
                    UserId = existingReaction.UserId,
                    UserName = user?.FullName ?? "Unknown",
                    ReactionType = existingReaction.ReactionType,
                    CreatedAt = existingReaction.CreatedAt
                };
            }
        }
        else
        {
            // Tạo reaction mới
            var reaction = new MessageReaction
            {
                MessageId = messageId,
                UserId = userId,
                ReactionType = reactionType,
                CreatedAt = DateTime.Now
            };

            _dbContext.MessageReactions.Add(reaction);
            await _dbContext.SaveChangesAsync();

            var user = await _dbContext.Users.FindAsync(userId);
            return new MessageReactionViewModel
            {
                ReactionId = reaction.ReactionId,
                MessageId = reaction.MessageId,
                UserId = reaction.UserId,
                UserName = user?.FullName ?? "Unknown",
                ReactionType = reaction.ReactionType,
                CreatedAt = reaction.CreatedAt
            };
        }
    }

    public async Task<List<MessageReactionSummaryViewModel>> GetMessageReactionsAsync(long messageId, int currentUserId)
    {
        var reactions = await _dbContext.MessageReactions
            .Include(r => r.User)
            .Where(r => r.MessageId == messageId)
            .ToListAsync();

        var groupedReactions = reactions
            .GroupBy(r => r.ReactionType)
            .Select(g => new MessageReactionSummaryViewModel
            {
                ReactionType = g.Key,
                Count = g.Count(),
                UserNames = g.Select(r => r.User?.FullName ?? "Unknown").ToList(),
                HasUserReaction = g.Any(r => r.UserId == currentUserId)
            })
            .ToList();

        return groupedReactions;
    }

    public async Task RemoveReactionAsync(long messageId, int userId)
    {
        var reaction = await _dbContext.MessageReactions
            .FirstOrDefaultAsync(r => r.MessageId == messageId && r.UserId == userId);

        if (reaction != null)
        {
            _dbContext.MessageReactions.Remove(reaction);
            await _dbContext.SaveChangesAsync();
        }
    }

    public async Task<bool> HasUserReactionAsync(long messageId, int userId, string reactionType)
    {
        return await _dbContext.MessageReactions
            .AnyAsync(r => r.MessageId == messageId && r.UserId == userId && r.ReactionType == reactionType);
    }

    public async Task<GroupMessageViewModel> SendGroupStickerAsync(int senderId, int groupId, string stickerUrl)
    {
        var sender = await _dbContext.Users.FindAsync(senderId);
        var group = await _dbContext.ChatGroups.FindAsync(groupId);
        if (sender == null) throw new Exception("Sender not found");
        if (group == null) throw new Exception("Group not found");
        if (string.IsNullOrEmpty(stickerUrl)) throw new ArgumentException("Sticker URL cannot be empty");

        // Lấy loại sticker từ url
        var parts = stickerUrl.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var stickerIndex = Array.IndexOf(parts, "sticker");
        string stickerType = "Unknown";
        if (stickerIndex != -1 && parts.Length > stickerIndex + 1)
            stickerType = parts[stickerIndex + 1];

        var message = new Message
        {
            SenderId = senderId,
            GroupId = groupId,
            Content = "Sticker",
            SentAt = DateTime.Now,
            IsEdited = false,
            Media = new List<Media>(),
            Sticker = new List<Sticker>()
        };
        var sticker = new Sticker
        {
            StickerUrl = stickerUrl,
            StickerType = stickerType,
            SentAt = DateTime.Now
        };
        message.Sticker.Add(sticker);
        _dbContext.Messages.Add(message);
        await _dbContext.SaveChangesAsync();

        return new GroupMessageViewModel
        {
            MessageId = (int)message.MessageId,
            SenderId = senderId,
            GroupId = groupId,
            SenderName = sender.FullName,
            AvatarUrl = sender.AvatarUrl,
            Content = message.Content,
            SentAt = message.SentAt,
            IsMine = false,
            IsEdited = false,
            Media = new List<MediaViewModel>(),
            StickerUrl = stickerUrl,
            StickerType = stickerType
        };
    }

    // Lấy media của nhóm (ảnh, video, file)
    public async Task<(List<MediaViewModel> Images, List<MediaViewModel> Videos, List<MediaViewModel> Files)> GetGroupMediaAsync(int groupId, int limit = 20)
    {
        var media = await _dbContext.Media
            .Include(m => m.Message)
            .Where(m => m.Message.GroupId == groupId)
            .OrderByDescending(m => m.UploadedAt)
            .Take(limit)
            .ToListAsync();

        var images = new List<MediaViewModel>();
        var videos = new List<MediaViewModel>();
        var files = new List<MediaViewModel>();

        foreach (var item in media)
        {
            var mediaViewModel = new MediaViewModel
            {
                Url = item.Url,
                MediaType = item.MediaType,
                FileName = item.FileName
            };

            if (item.MediaType != null && item.MediaType.StartsWith("image/"))
            {
                images.Add(mediaViewModel);
            }
            else if (item.MediaType != null && item.MediaType.StartsWith("video/"))
            {
                videos.Add(mediaViewModel);
            }
            else
            {
                files.Add(mediaViewModel);
            }
        }

        return (images, videos, files);
    }

    // New method: Create group with avatar and friendIds parsing/validation
    public async Task<(bool Success, string Message, GroupViewModel Group)> CreateGroupWithAvatarAndFriendsAsync(
        int creatorId, string name, string description, IFormFile avatar, string password, string friendIdsJson)
    {
        try
        {
            if (string.IsNullOrEmpty(name?.Trim()))
                return (false, "Tên nhóm không được để trống", null);
            if (creatorId == 0)
                return (false, "Không tìm thấy thông tin người dùng", null);

            // Parse friendIds
            var friendIdList = new List<int>();
            if (!string.IsNullOrEmpty(friendIdsJson))
            {
                friendIdList = System.Text.Json.JsonSerializer.Deserialize<List<int>>(friendIdsJson);
            }
            if (friendIdList == null || friendIdList.Count < 2)
                return (false, "Bạn phải chọn ít nhất 2 người bạn để tạo nhóm.", null);

            // Handle avatar upload
            string avatarUrl = null;
            if (avatar != null && avatar.Length > 0)
            {
                // Sử dụng dịch vụ upload bên thứ ba (Cloudinary)
                avatarUrl = await _fileUploadService.UploadAsync(avatar, "group-avatar");
            }

            // Call existing group creation logic
            var group = await CreateGroupAsync(creatorId, name.Trim(), description?.Trim(), avatarUrl, password, friendIdList);
            var groupVm = new GroupViewModel
            {
                GroupId = group.GroupId,
                Name = group.Name,
                AvatarUrl = group.AvatarUrl,
                Description = group.Description,
                CreatedAt = group.CreatedAt
            };
            return (true, null, groupVm);
        }
        catch (ArgumentException ex)
        {
            return (false, ex.Message, null);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating group: {ex.Message}");
            return (false, "Có lỗi xảy ra khi tạo nhóm. Vui lòng thử lại.", null);
        }
    }

    // New method: Add member with all validation
    public async Task<(bool Success, string Message)> AddMemberWithValidationAsync(int groupId, int userIdToAdd, int currentUserId)
    {
        try
        {
            if (currentUserId == 0)
                return (false, "Không tìm thấy thông tin người dùng");
            var group = await GetGroupDetailsAsync(groupId);
            if (group == null)
                return (false, "Không tìm thấy nhóm");
            var isMember = group.Members.Any(m => m.UserId == currentUserId);
            if (!isMember)
                return (false, "Bạn không phải thành viên của nhóm này");
            var existingMember = group.Members.FirstOrDefault(m => m.UserId == userIdToAdd);
            if (existingMember != null)
                return (false, "Người dùng đã là thành viên của nhóm");
            await AddMemberToGroupAsync(groupId, userIdToAdd);
            return (true, "Thêm thành viên thành công");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error adding member to group: {ex.Message}");
            return (false, "Có lỗi xảy ra khi thêm thành viên. Vui lòng thử lại.");
        }
    }

    // New method: Search users and filter out group members
    public async Task<List<UserViewModel>> SearchUsersWithGroupFilterAsync(string searchTerm, int currentUserId, int? groupId)
    {
        var users = await SearchUsersAsync(searchTerm, currentUserId);
        if (groupId.HasValue)
        {
            var group = await GetGroupDetailsAsync(groupId.Value);
            if (group != null)
            {
                var existingMemberIds = group.Members.Select(m => m.UserId).ToHashSet();
                users = users.Where(u => !existingMemberIds.Contains(u.UserId)).ToList();
            }
        }
        return users;
    }

    // New method: Build group sidebar view model (right)
    public async Task<GroupViewModel> BuildGroupSidebarViewModelAsync(int groupId, int mediaLimit)
    {
        var group = await GetGroupDetailsAsync(groupId);
        if (group == null) return null;
        var members = group.Members?.Select(m => new UserViewModel
        {
            UserId = m.UserId,
            FullName = m.User?.FullName ?? "Unknown",
            Email = m.User?.Email ?? "",
            AvatarUrl = m.User?.AvatarUrl ?? "/images/default-avatar.jpeg",
            IsOnline = m.User != null && m.User.LastLoginAt > DateTime.Now.AddMinutes(-3),
            LastLoginAt = m.User?.LastLoginAt
        }).ToList() ?? new List<UserViewModel>();
        var (images, videos, files) = await GetGroupMediaAsync(groupId, mediaLimit);
        return new GroupViewModel
        {
            GroupId = (int)group.GroupId,
            Name = group.Name,
            Description = group.Description,
            AvatarUrl = group.AvatarUrl ?? "/images/default-group-avatar.png",
            MemberCount = group.Members?.Count ?? 0,
            CreatedAt = group.CreatedAt,
            CreatorId = group.CreatorId,
            CreatorName = group.Creator?.FullName ?? "Unknown",
            Members = members,
            Images = images,
            Videos = videos,
            Files = files
        };
    }

    // New method: Build group sidebar media view model
    public async Task<GroupViewModel> BuildGroupSidebarMediaViewModelAsync(int groupId, int mediaLimit)
    {
        // For sidebar media, just call the above with a higher limit
        return await BuildGroupSidebarViewModelAsync(groupId, mediaLimit);
    }
    
    public async Task<List<MessageViewModel>> SearchMessagesAsync(int userId, int friendId, string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            return new List<MessageViewModel>();
        
        var rawMessages = await _dbContext.Messages
            .Include(m => m.Sender)
            .Include(m => m.Recipient)
            .Where(m =>
                ((m.SenderId == userId && m.RecipientId == friendId) ||
                 (m.SenderId == friendId && m.RecipientId == userId)) &&
                m.Content != null
            )
            .OrderByDescending(m => m.SentAt)
            .Take(200) // Lấy nhiều hơn để lọc sau
            .ToListAsync();

        // Chuẩn hóa keyword
        var plainKeyword = RemoveVietnameseDiacritics(keyword.ToLowerInvariant());

        var messages = rawMessages
            .Where(m => RemoveVietnameseDiacritics(m.Content.ToLowerInvariant()).Contains(plainKeyword))
            .Take(50) // Giới hạn kết quả sau lọc
            .Select(m => new MessageViewModel
            {
                MessageId = m.MessageId,
                SenderId = m.SenderId,
                RecipientId = m.RecipientId ?? 0,
                SenderName = m.Sender.FullName,
                Content = m.Content,
                SentAt = m.SentAt,
                IsEdited = m.IsEdited
            })
            .Take(50)
            .ToList();
        return messages;
    }

    // MessageStatus change
    // chuyển trạng thái của các tin nhắn đã đánh dấu là đã nhận, đã gửi nếu tin nhắn mới hơn có trạng thái là đã xem
    public async Task<List<long>> MarkAsSeenAsync(long messageId, int userId)
    {
        var message = await _dbContext.Messages.FindAsync(messageId);
        if (message == null || message.RecipientId != userId) return [];

        var messagesToUpdate = await _dbContext.Messages
            .Where(m =>
                m.RecipientId == userId &&
                m.SenderId == message.SenderId &&
                m.MessageStatus != MessageStatus.Seen &&
                m.SentAt <= message.SentAt)
            .ToListAsync();

        foreach (var msg in messagesToUpdate)
        {
            msg.MessageStatus = MessageStatus.Seen;
        }

        await _dbContext.SaveChangesAsync();
        return messagesToUpdate.Select(m => m.MessageId).ToList();
    }

    // chuyển trạng thái của các tin nhắn đã đánh dấu là đã gửi nếu tin nhắn mới hơn có trạng thái là đã nhận
    public async Task<List<long>> MarkAsDeliveredAsync(long messageId, int userId)
    {
        var message = await _dbContext.Messages.FindAsync(messageId);
        if (message == null || message.RecipientId != userId || message.MessageStatus != MessageStatus.Sent) return [];

        var messagesToUpdate = await _dbContext.Messages
            .Where(m =>
                m.RecipientId == userId &&
                m.SenderId == message.SenderId &&
                m.MessageStatus == MessageStatus.Sent &&
                m.SentAt <= message.SentAt)
            .ToListAsync();

        foreach (var msg in messagesToUpdate)
        {
            msg.MessageStatus = MessageStatus.Delivered;
        }

        await _dbContext.SaveChangesAsync();
        return messagesToUpdate.Select(m => m.MessageId).ToList();
    }

    private static string RemoveVietnameseDiacritics(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        var normalized = text.Normalize(System.Text.NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var c in normalized)
        {
            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }
        return sb.ToString().Normalize(System.Text.NormalizationForm.FormC);
    }

    public async Task<string> UploadGroupAvatarAsync(IFormFile avatarFile)
    {
        // Sử dụng dịch vụ upload cloud đã có
        return await _fileUploadService.UploadAsync(avatarFile, "group-avatar");
    }

    public async Task UpdateGroupFullAsync(int groupId, string name, string? description, string? avatarUrl, string? password)
    {
        var group = await _dbContext.ChatGroups.FindAsync(groupId);
        if (group == null)
            throw new Exception("Group not found");
        group.Name = name;
        group.Description = string.IsNullOrEmpty(description) ? "" : description;
        if (!string.IsNullOrEmpty(avatarUrl))
            group.AvatarUrl = avatarUrl;
        group.Password = string.IsNullOrEmpty(password) ? null : password;
        await _dbContext.SaveChangesAsync();
    }

    // Group edit methods
    public async Task<EditGroupViewModel> GetGroupInfoForEditAsync(int groupId)
    {
        var group = await GetGroupDetailsAsync(groupId);
        if (group == null) return null;
        
        return new EditGroupViewModel
        {
            GroupId = group.GroupId,
            Name = group.Name,
            Description = group.Description,
            AvatarUrl = group.AvatarUrl,
            Password = group.Password
        };
    }

    public async Task<(bool Success, string Message)> EditGroupAsync(EditGroupViewModel model, IFormFile? avatarFile)
    {
        try
        {
            // Validate model
            if (model == null || model.GroupId <= 0)
                return (false, "Dữ liệu không hợp lệ. Vui lòng kiểm tra lại.");

            if (string.IsNullOrWhiteSpace(model.Name))
                return (false, "Tên nhóm không được để trống.");

            // Upload avatar nếu có file mới
            if (avatarFile != null && avatarFile.Length > 0)
            {
                var avatarUrl = await UploadGroupAvatarAsync(avatarFile);
                model.AvatarUrl = avatarUrl;
            }

            await UpdateGroupFullAsync(model.GroupId, model.Name, model.Description, model.AvatarUrl, model.Password);
            
            return (true, "Cập nhật nhóm thành công!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating group: {ex.Message}");
            return (false, "Có lỗi xảy ra khi cập nhật nhóm");
        }
    }

    // Group join methods
    public async Task<(bool Success, string Message)> JoinGroupAsync(int groupId, int userId)
    {
        try
        {
            if (userId == 0)
                return (false, "Bạn cần đăng nhập để tham gia nhóm.");

            var group = await GetGroupDetailsAsync(groupId);
            if (group == null)
                return (false, "Nhóm không tồn tại.");

            var isMember = group.Members.Any(m => m.UserId == userId);
            if (isMember)
                return (false, "Bạn đã là thành viên của nhóm này.");

            await AddMemberToGroupAsync(groupId, userId);
            return (true, "Bạn đã tham gia nhóm thành công!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error joining group: {ex.Message}");
            return (false, "Có lỗi xảy ra khi tham gia nhóm");
        }
    }

    // Message reaction methods with validation
    public async Task<(bool Success, string Message, object Result)> AddReactionWithValidationAsync(long messageId, int userId, string reactionType)
    {
        try
        {
            if (userId == 0)
                return (false, "Không tìm thấy thông tin người dùng", null);

            var reaction = await AddReactionAsync(messageId, userId, reactionType);
            
            if (reaction == null)
            {
                // Reaction đã bị xóa
                return (true, "Reaction removed", new { action = "removed" });
            }
            else
            {
                // Reaction đã được thêm hoặc cập nhật
                return (true, "Reaction added", new { action = "added", reaction = reaction });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error adding reaction: {ex.Message}");
            return (false, "Có lỗi xảy ra khi thêm biểu tượng cảm xúc", null);
        }
    }

    public async Task<(bool Success, string Message, object Result)> GetMessageReactionsWithValidationAsync(long messageId, int userId)
    {
        try
        {
            if (userId == 0)
                return (false, "Không tìm thấy thông tin người dùng", null);

            var reactions = await GetMessageReactionsAsync(messageId, userId);
            return (true, "Success", new { reactions = reactions });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting reactions: {ex.Message}");
            return (false, "Có lỗi xảy ra khi lấy biểu tượng cảm xúc", null);
        }
    }

    public async Task<(bool Success, string Message)> RemoveReactionWithValidationAsync(long messageId, int userId)
    {
        try
        {
            if (userId == 0)
                return (false, "Không tìm thấy thông tin người dùng");

            await RemoveReactionAsync(messageId, userId);
            return (true, "Reaction removed successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error removing reaction: {ex.Message}");
            return (false, "Có lỗi xảy ra khi xóa biểu tượng cảm xúc");
        }
    }

    // Group message methods with validation
    public async Task<(bool Success, string Message, GroupMessageViewModel Result)> SendGroupMessageWithValidationAsync(int senderId, int groupId, string content, List<IFormFile> files, long? replyToMessageId)
    {
        try
        {
            if (senderId == 0)
                return (false, "Không tìm thấy thông tin người dùng", null);

            // Validate input
            if (string.IsNullOrWhiteSpace(content) && (files == null || files.Count == 0))
                return (false, "Vui lòng nhập nội dung tin nhắn hoặc chọn file", null);

            var message = await SendGroupMessageAsync(senderId, groupId, content, files, replyToMessageId);
            return (true, "Message sent successfully", message);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending group message: {ex.Message}");
            return (false, "Có lỗi xảy ra khi gửi tin nhắn. Vui lòng thử lại.", null);
        }
    }

    // Group messages with user context
    public async Task<List<GroupMessageViewModel>> GetGroupMessagesWithUserContextAsync(int groupId, int userId)
    {
        var messages = await GetGroupMessagesAsync(groupId);
        
        // Set IsMine for each message
        foreach (var msg in messages)
        {
            msg.IsMine = msg.SenderId == userId;
            
            // Update reactions to show if current user has reacted
            foreach (var reaction in msg.Reactions)
            {
                reaction.HasUserReaction = await HasUserReactionAsync(msg.MessageId, userId, reaction.ReactionType);
            }
        }
        
        return messages;
    }
}