using Microsoft.AspNetCore.SignalR;
using Zela.Models;
using Zela.Services;
using ZelaTestDemo.ViewModels;

public class ChatHub : Hub
{
    private readonly IChatService _chatService;
    public ChatHub(IChatService chatService)
    {
        _chatService = chatService;
    }

    // Gửi tin nhắn 1-1
    public async Task SendMessage(int recipientId, string content)
    {
        // Lấy userId từ Claims hoặc Context.UserIdentifier
        int senderId = int.Parse(Context.UserIdentifier);

        // Lưu vào DB và trả về message mới (có thông tin avatar, tên, IsMine, SentAt...)
        var msgVm = await _chatService.SendMessageAsync(senderId, recipientId, content);

        // Gửi message cho người gửi
        await Clients.User(senderId.ToString()).SendAsync("ReceiveMessage", msgVm);

        // Gửi message cho người nhận (nếu khác người gửi)
        if (senderId != recipientId)
            await Clients.User(recipientId.ToString()).SendAsync("ReceiveMessage", msgVm);
    }
    
    // Gửi tin nhắn nhóm
    public async Task SendGroupMessage(int groupId, string content)
    {
        int senderId = int.Parse(Context.UserIdentifier);
        var msgVm = await _chatService.SendGroupMessageAsync(senderId, groupId, content);
        
        // Gửi message cho tất cả thành viên trong nhóm
        await Clients.Group(groupId.ToString()).SendAsync("ReceiveGroupMessage", msgVm);
    }

    // Tham gia vào nhóm chat
    public async Task JoinGroup(int groupId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupId.ToString());
    }

    // Rời khỏi nhóm chat
    public async Task LeaveGroup(int groupId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupId.ToString());
    }

    // Tạo nhóm chat mới
    public async Task<ChatGroup> CreateGroup(string name, string description)
    {
        int creatorId = int.Parse(Context.UserIdentifier);
        var group = await _chatService.CreateGroupAsync(creatorId, name, description);
        
        // Tự động tham gia vào nhóm sau khi tạo
        await JoinGroup(group.GroupId);
        
        return group;
    }

    // Thêm thành viên vào nhóm
    public async Task AddMemberToGroup(int groupId, int userId)
    {
        await _chatService.AddMemberToGroupAsync(groupId, userId);
        // Thông báo cho tất cả thành viên trong nhóm
        await Clients.Group(groupId.ToString()).SendAsync("MemberAdded", userId);
    }

    // Xóa thành viên khỏi nhóm
    public async Task RemoveMemberFromGroup(int groupId, int userId)
    {
        await _chatService.RemoveMemberFromGroupAsync(groupId, userId);
        // Thông báo cho tất cả thành viên trong nhóm
        await Clients.Group(groupId.ToString()).SendAsync("MemberRemoved", userId);
    }
}