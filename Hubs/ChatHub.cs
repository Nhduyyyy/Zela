using Microsoft.AspNetCore.SignalR;
using Zela.Enum;
using Zela.Models;
using Zela.Services;
using Zela.ViewModels;

public class ChatHub : Hub
{
    private readonly IChatService _chatService;
    private readonly IStickerService _stickerService;
    public ChatHub(IChatService chatService, IStickerService stickerService)
    {
        _chatService = chatService;
        _stickerService = stickerService;
    }

    // Gửi tin nhắn 1-1
    public async Task SendMessage(int recipientId, string content)
    {
        // Lấy userId từ Claims hoặc Context.UserIdentifier
        int senderId = int.Parse(Context.UserIdentifier);

        // Lưu tin nhắn vào DB và trả về message mới (bao gồm thông tin avatar, tên, IsMine, SentAt...)
        var msgVm = await _chatService.SendMessageAsync(senderId, recipientId, content);

        // Gửi message cho người gửi
        await Clients.User(senderId.ToString()).SendAsync("ReceiveMessage", msgVm);

        // Gửi message cho người nhận (nếu khác người gửi)
        if (senderId != recipientId)
            await Clients.User(recipientId.ToString()).SendAsync("ReceiveMessage", msgVm);
    }
    
    // Send sticker 1-1
    public async Task SendSticker(int recipientId, string url)
    {
        // Lấy userId từ Claims hoặc Context.UserIdentifier
        int senderId = int.Parse(Context.UserIdentifier);
        
        // Lưu sticker vào DB và trả về sticker mới
        var stickerVm = await _stickerService.SendStickerAsync(senderId, recipientId, url);
        
        // Gửi sticker cho cả người gửi và người nhận
        await Clients.Users(senderId.ToString(), recipientId.ToString())
            .SendAsync("ReceiveSticker", stickerVm);
    }
    
    // Gửi tin nhắn nhóm
    public async Task SendGroupMessage(int groupId, string content, long? replyToMessageId = null)
    {
        // Lấy userId từ context
        int senderId = int.Parse(Context.UserIdentifier);
        // Lưu tin nhắn nhóm vào DB và trả về message mới
        var msgVm = await _chatService.SendGroupMessageAsync(senderId, groupId, content, null, replyToMessageId);
        // Gửi message cho tất cả thành viên trong group qua SignalR
        await Clients.Group(groupId.ToString()).SendAsync("ReceiveGroupMessage", msgVm);
    }

    // Tham gia vào nhóm chat
    public async Task JoinGroup(int groupId)
    {
        // Thêm kết nối hiện tại vào group SignalR
        await Groups.AddToGroupAsync(Context.ConnectionId, groupId.ToString());
    }

    // Rời khỏi nhóm chat
    public async Task LeaveGroup(int groupId)
    {
        // Xóa kết nối hiện tại khỏi group SignalR
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupId.ToString());
    }

    // Tạo nhóm chat mới
    public async Task<ChatGroup> CreateGroup(string name, string description)
    {
        // Lấy userId người tạo nhóm
        int creatorId = int.Parse(Context.UserIdentifier);
        // Tạo group mới (avatarUrl, password, friendIds để null hoặc rỗng)
        var group = await _chatService.CreateGroupAsync(creatorId, name, description, null, null, new List<int>());
        // Tự động tham gia vào nhóm sau khi tạo
        await JoinGroup(group.GroupId);
        return group;
    }

    // Thêm thành viên vào nhóm
    public async Task AddMemberToGroup(int groupId, int userId)
    {
        // Gọi service thêm thành viên vào DB
        await _chatService.AddMemberToGroupAsync(groupId, userId);
        // Thông báo cho tất cả thành viên trong nhóm về thành viên mới
        await Clients.Group(groupId.ToString()).SendAsync("MemberAdded", userId);
    }

    // Xóa thành viên khỏi nhóm
    public async Task RemoveMemberFromGroup(int groupId, int userId)
    {
        // Gọi service xóa thành viên khỏi DB
        await _chatService.RemoveMemberFromGroupAsync(groupId, userId);
        // Thông báo cho tất cả thành viên trong nhóm về thành viên bị xóa
        await Clients.Group(groupId.ToString()).SendAsync("MemberRemoved", userId);
    }

    // Gửi sticker nhóm
    public async Task SendGroupSticker(int groupId, string url)
    {
        // Lấy userId từ context
        int senderId = int.Parse(Context.UserIdentifier);
        // Lưu sticker nhóm vào DB và trả về sticker mới
        var stickerVm = await _chatService.SendGroupStickerAsync(senderId, groupId, url);
        // Gửi sticker cho tất cả thành viên trong group
        await Clients.Group(groupId.ToString()).SendAsync("ReceiveGroupSticker", stickerVm);
    }
    
    // Search Message history
    public async Task<List<MessageViewModel>> SearchMessages(int friendId, string keyword)
    {
        // Lấy userId từ context
        var userId = int.Parse(Context.UserIdentifier);
        // Gọi service tìm kiếm tin nhắn giữa user và friendId theo từ khóa
        var messages = await _chatService.SearchMessagesAsync(userId, friendId, keyword);
        return messages;
    }
    
    // Mark message as seen
    public async Task MarkAsSeen(long messageId)
    {
        // Lấy userId từ context
        int userId = int.Parse(Context.UserIdentifier);
        // Gọi service đánh dấu đã xem, trả về danh sách id tin nhắn đã cập nhật
        var messageIds = await _chatService.MarkAsSeenAsync(messageId, userId);

        if (messageIds.Any())
        {
            // Gửi sự kiện cập nhật trạng thái về client
            await Clients.User(Context.UserIdentifier).SendAsync("MessageStatusUpdated", new
            {
                messageIds,
                newStatus = MessageStatus.Seen,
                statusText = "Đã xem"
            });
        }
    }

    public async Task MarkAsDelivered(long messageId)
    {
        // Lấy userId từ context
        int userId = int.Parse(Context.UserIdentifier);
        // Gọi service đánh dấu đã nhận, trả về danh sách id tin nhắn đã cập nhật
        var messageIds = await _chatService.MarkAsDeliveredAsync(messageId, userId);

        if (messageIds.Any())
        {
            // Gửi sự kiện cập nhật trạng thái về client
            await Clients.User(Context.UserIdentifier).SendAsync("MessageStatusUpdated", new
            {
                messageIds,
                newStatus = MessageStatus.Delivered,
                statusText = "Đã nhận"
            });
        }
    }
}