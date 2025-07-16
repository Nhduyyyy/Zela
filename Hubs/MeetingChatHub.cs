using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Zela.DbContext;
using Zela.Enum;
using Zela.Models;
using Zela.Services;
using Zela.Services.Interface;
using Zela.ViewModels;

namespace Zela.Hubs;

public class MeetingChatHub : Hub
{
    private readonly IMeetingRoomMessageService _meetingRoomMessageService;
    private readonly IMeetingService _meetingService;
    private readonly ApplicationDbContext _context;

    public MeetingChatHub(IMeetingRoomMessageService meetingRoomMessageService, IMeetingService meetingService, ApplicationDbContext context)
    {
        _meetingRoomMessageService = meetingRoomMessageService;
        _meetingService = meetingService;
        _context = context;
    }

    /// <summary>
    /// Khi user kết nối vào hub
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
        Console.WriteLine($"MeetingChatHub: User {Context.UserIdentifier} connected");
    }

    /// <summary>
    /// Khi user ngắt kết nối khỏi hub
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        Console.WriteLine($"MeetingChatHub: User {Context.UserIdentifier} disconnecting");
        Console.WriteLine($"Connection ID: {Context.ConnectionId}");
        if (exception != null)
        {
            Console.WriteLine($"Disconnection exception: {exception.Message}");
            Console.WriteLine($"Exception stack trace: {exception.StackTrace}");
        }
        
        await base.OnDisconnectedAsync(exception);
        Console.WriteLine($"MeetingChatHub: User {Context.UserIdentifier} disconnected");
    }

    /// <summary>
    /// Join vào room chat
    /// </summary>
    public async Task JoinRoom(string meetingCode, Guid sessionId)
    {
        try
        {
            Console.WriteLine($"JoinRoom called - meetingCode: {meetingCode}, sessionId: {sessionId}");
            
            var userId = int.Parse(Context.UserIdentifier ?? "0");
            Console.WriteLine($"User ID from context: {userId}");
            
            if (userId == 0) 
            {
                Console.WriteLine("User ID is 0, returning");
                return;
            }

            // Tìm roomId từ meeting code
            Console.WriteLine($"Looking for room with password: {meetingCode}");
            var room = await _context.VideoRooms.FirstOrDefaultAsync(v => v.Password == meetingCode);
            if (room == null)
            {
                Console.WriteLine($"Room not found for meeting code: {meetingCode}");
                await Clients.Caller.SendAsync("Error", "Không tìm thấy phòng");
                return;
            }

            var roomId = room.RoomId;
            Console.WriteLine($"Found room ID: {roomId}");

            // Kiểm tra và thêm user vào RoomParticipants nếu chưa có
            Console.WriteLine($"Checking if user {userId} is in room {roomId}");
            var isInRoom = await _meetingRoomMessageService.IsUserInRoomAsync(userId, roomId);
            Console.WriteLine($"User {userId} is in room: {isInRoom}");
            
            if (!isInRoom)
            {
                Console.WriteLine($"Adding user {userId} to room {roomId} via TrackUserJoinAsync");
                // Tự động thêm user vào phòng khi join chat
                await _meetingService.TrackUserJoinAsync(sessionId, userId);
                Console.WriteLine($"Auto-added user {userId} to room {roomId} when joining chat");
            }

            // Join vào SignalR group
            Console.WriteLine($"Adding connection {Context.ConnectionId} to group room_{roomId}");
            await Groups.AddToGroupAsync(Context.ConnectionId, $"room_{roomId}");

            // Gửi thông báo cho các user khác (chỉ gửi nếu thực sự là user mới)
            if (!isInRoom)
            {
                Console.WriteLine($"Notifying others in room {roomId} about user {userId} joining");
                await Clients.OthersInGroup($"room_{roomId}").SendAsync("UserJoined", new
                {
                    userId = userId,
                    timestamp = DateTime.UtcNow
                });
            }

            Console.WriteLine($"User {userId} successfully joined room {roomId} (meeting code: {meetingCode})");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error joining room: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            await Clients.Caller.SendAsync("Error", "Lỗi khi tham gia phòng");
        }
    }

    /// <summary>
    /// Leave khỏi room chat
    /// </summary>
    public async Task LeaveRoom(string meetingCode, Guid sessionId)
    {
        try
        {
            var userId = int.Parse(Context.UserIdentifier ?? "0");
            if (userId == 0) return;

            // Tìm roomId từ meeting code
            var room = await _context.VideoRooms.FirstOrDefaultAsync(v => v.Password == meetingCode);
            if (room == null) return;

            var roomId = room.RoomId;

            // Leave khỏi SignalR group
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"room_{roomId}");
            
            // Track user leave
            await _meetingService.TrackUserLeaveAsync(sessionId, userId);

            // Gửi thông báo cho các user khác
            await Clients.OthersInGroup($"room_{roomId}").SendAsync("UserLeft", new
            {
                userId = userId,
                timestamp = DateTime.UtcNow
            });

            Console.WriteLine($"User {userId} left room {roomId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error leaving room: {ex.Message}");
        }
    }

    /// <summary>
    /// Gửi tin nhắn trong phòng
    /// </summary>
    public async Task SendRoomMessage(string meetingCode, Guid sessionId, string content, MessageType messageType = MessageType.Text, bool isPrivate = false, int? recipientId = null)
    {
        try
        {
            // Debug: Log user information
            Console.WriteLine($"Debug - Context.UserIdentifier: {Context.UserIdentifier}");
            Console.WriteLine($"Debug - Context.User.Identity.Name: {Context.User?.Identity?.Name}");
            Console.WriteLine($"Debug - Context.User.IsAuthenticated: {Context.User?.Identity?.IsAuthenticated}");
            
            if (Context.User?.Identity?.IsAuthenticated != true)
            {
                await Clients.Caller.SendAsync("Error", "Bạn chưa đăng nhập");
                return;
            }
            
            var senderId = int.Parse(Context.UserIdentifier ?? "0");
            if (senderId == 0)
            {
                await Clients.Caller.SendAsync("Error", "Không xác định được người dùng");
                return;
            }

            // Tìm roomId từ meeting code
            var room = await _context.VideoRooms.FirstOrDefaultAsync(v => v.Password == meetingCode);
            if (room == null)
            {
                await Clients.Caller.SendAsync("Error", "Không tìm thấy phòng");
                return;
            }

            var roomId = room.RoomId;

            // Đảm bảo user có trong RoomParticipants
            var isInRoom = await _meetingRoomMessageService.IsUserInRoomAsync(senderId, roomId);
            if (!isInRoom)
            {
                // Tự động thêm user vào phòng
                await _meetingService.TrackUserJoinAsync(sessionId, senderId);
                Console.WriteLine($"Auto-added user {senderId} to room {roomId} when sending message");
            }

            // Kiểm tra quyền gửi tin nhắn
            if (!await _meetingRoomMessageService.CanUserSendMessageAsync(senderId, roomId))
            {
                await Clients.Caller.SendAsync("Error", "Bạn không có quyền gửi tin nhắn trong phòng này");
                return;
            }

            // Tạo model để gửi tin nhắn
            var messageModel = new MeetingSendMessageViewModel
            {
                Content = content,
                RoomId = roomId,
                SessionId = sessionId,
                MessageType = messageType,
                IsPrivate = isPrivate,
                RecipientId = recipientId
            };

            // Lưu tin nhắn vào database
            var message = await _meetingRoomMessageService.SendMessageAsync(messageModel, senderId);

            // Gửi tin nhắn real-time
            if (isPrivate && recipientId.HasValue)
            {
                // Tin nhắn riêng tư - chỉ gửi cho sender và recipient
                await Clients.Users(senderId.ToString(), recipientId.Value.ToString())
                    .SendAsync("ReceivePrivateMessage", message);
            }
            else
            {
                // Tin nhắn công khai - gửi cho tất cả trong phòng
                await Clients.Group($"room_{roomId}").SendAsync("ReceiveRoomMessage", message);
            }

            // Gửi confirmation cho sender
            await Clients.Caller.SendAsync("MessageSent", new { success = true, messageId = message.MessageId });

            Console.WriteLine($"Message sent in room {roomId} by user {senderId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending message: {ex.Message}");
            await Clients.Caller.SendAsync("Error", "Lỗi khi gửi tin nhắn");
        }
    }

    /// <summary>
    /// Chỉnh sửa tin nhắn
    /// </summary>
    public async Task EditMessage(long messageId, string content, string? editReason = null)
    {
        try
        {
            var userId = int.Parse(Context.UserIdentifier ?? "0");
            if (userId == 0) return;

            var editModel = new MeetingEditMessageViewModel
            {
                MessageId = messageId,
                Content = content,
                EditReason = editReason
            };

            var editedMessage = await _meetingRoomMessageService.EditMessageAsync(editModel, userId);
            
            if (editedMessage != null)
            {
                // Gửi thông báo chỉnh sửa cho tất cả trong phòng
                await Clients.Group($"room_{editedMessage.RoomId}").SendAsync("MessageEdited", editedMessage);
            }
            else
            {
                await Clients.Caller.SendAsync("Error", "Không thể chỉnh sửa tin nhắn");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error editing message: {ex.Message}");
            await Clients.Caller.SendAsync("Error", "Lỗi khi chỉnh sửa tin nhắn");
        }
    }

    /// <summary>
    /// Xóa tin nhắn
    /// </summary>
    public async Task DeleteMessage(long messageId)
    {
        try
        {
            var userId = int.Parse(Context.UserIdentifier ?? "0");
            if (userId == 0) return;

            var success = await _meetingRoomMessageService.DeleteMessageAsync(messageId, userId);
            
            if (success)
            {
                // Lấy thông tin tin nhắn để biết roomId
                var message = await _meetingRoomMessageService.GetMessageByIdAsync(messageId);
                if (message != null)
                {
                    // Gửi thông báo xóa cho tất cả trong phòng
                    await Clients.Group($"room_{message.RoomId}").SendAsync("MessageDeleted", new
                    {
                        messageId = messageId,
                        deletedBy = userId,
                        timestamp = DateTime.UtcNow
                    });
                }
            }
            else
            {
                await Clients.Caller.SendAsync("Error", "Không thể xóa tin nhắn");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting message: {ex.Message}");
            await Clients.Caller.SendAsync("Error", "Lỗi khi xóa tin nhắn");
        }
    }

    /// <summary>
    /// Typing indicator
    /// </summary>
    public async Task StartTyping(string meetingCode)
    {
        try
        {
            var userId = int.Parse(Context.UserIdentifier ?? "0");
            if (userId == 0) return;

            // Tìm roomId từ meeting code
            var room = await _context.VideoRooms.FirstOrDefaultAsync(v => v.Password == meetingCode);
            if (room == null) return;

            await Clients.OthersInGroup($"room_{room.RoomId}").SendAsync("UserTyping", new
            {
                userId = userId,
                isTyping = true
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in typing indicator: {ex.Message}");
        }
    }

    /// <summary>
    /// Stop typing indicator
    /// </summary>
    public async Task StopTyping(string meetingCode)
    {
        try
        {
            var userId = int.Parse(Context.UserIdentifier ?? "0");
            if (userId == 0) return;

            // Tìm roomId từ meeting code
            var room = await _context.VideoRooms.FirstOrDefaultAsync(v => v.Password == meetingCode);
            if (room == null) return;

            await Clients.OthersInGroup($"room_{room.RoomId}").SendAsync("UserTyping", new
            {
                userId = userId,
                isTyping = false
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in typing indicator: {ex.Message}");
        }
    }

    /// <summary>
    /// Lấy danh sách participants trong phòng
    /// </summary>
    public async Task GetRoomParticipants(string meetingCode)
    {
        try
        {
            // Tìm roomId từ meeting code
            var room = await _context.VideoRooms.FirstOrDefaultAsync(v => v.Password == meetingCode);
            if (room == null)
            {
                await Clients.Caller.SendAsync("Error", "Không tìm thấy phòng");
                return;
            }

            var participants = await _meetingRoomMessageService.GetRoomParticipantsAsync(room.RoomId);
            await Clients.Caller.SendAsync("RoomParticipants", participants);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting participants: {ex.Message}");
            await Clients.Caller.SendAsync("Error", "Lỗi khi lấy danh sách người tham gia");
        }
    }
} 