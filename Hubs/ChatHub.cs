using Microsoft.AspNetCore.SignalR;
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
}