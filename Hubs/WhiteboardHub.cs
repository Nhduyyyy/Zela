using Microsoft.AspNetCore.SignalR;
using Zela.Services.Interface;
using System.Security.Claims;

namespace Zela.Hubs;

public class WhiteboardHub : Hub
{
    private readonly IWhiteboardService _whiteboardService;

    public WhiteboardHub(IWhiteboardService whiteboardService)
    {
        _whiteboardService = whiteboardService;
    }

    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Join whiteboard room
    /// </summary>
    public async Task JoinWhiteboard(int whiteboardId)
    {
        var userId = GetCurrentUserId();
        
        // Check if user can access whiteboard
        if (await _whiteboardService.CanUserAccessWhiteboardAsync(whiteboardId, userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"whiteboard_{whiteboardId}");
            await Clients.Group($"whiteboard_{whiteboardId}").SendAsync("UserJoined", userId);
        }
    }

    /// <summary>
    /// Leave whiteboard room
    /// </summary>
    public async Task LeaveWhiteboard(int whiteboardId)
    {
        var userId = GetCurrentUserId();
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"whiteboard_{whiteboardId}");
        await Clients.Group($"whiteboard_{whiteboardId}").SendAsync("UserLeft", userId);
    }

    /// <summary>
    /// Broadcast drawing data to other users
    /// </summary>
    public async Task BroadcastDrawing(int whiteboardId, string drawingData)
    {
        var userId = GetCurrentUserId();
        
        // Save to database
        var sessions = await _whiteboardService.GetSessionsByWhiteboardAsync(whiteboardId);
        var latestSession = sessions.FirstOrDefault();
        
        if (latestSession != null)
        {
            await _whiteboardService.UpdateSessionDataAsync(latestSession.SessionId, drawingData);
        }

        // Broadcast to other users in the room
        await Clients.OthersInGroup($"whiteboard_{whiteboardId}").SendAsync("DrawingUpdated", drawingData, userId);
    }

    /// <summary>
    /// Broadcast cursor position
    /// </summary>
    public async Task BroadcastCursor(int whiteboardId, double x, double y, string tool)
    {
        var userId = GetCurrentUserId();
        await Clients.OthersInGroup($"whiteboard_{whiteboardId}").SendAsync("CursorMoved", x, y, tool, userId);
    }

    /// <summary>
    /// Broadcast tool change
    /// </summary>
    public async Task BroadcastToolChange(int whiteboardId, string tool, string color, int size)
    {
        var userId = GetCurrentUserId();
        await Clients.OthersInGroup($"whiteboard_{whiteboardId}").SendAsync("ToolChanged", tool, color, size, userId);
    }

    /// <summary>
    /// Broadcast clear canvas
    /// </summary>
    public async Task BroadcastClearCanvas(int whiteboardId)
    {
        var userId = GetCurrentUserId();
        
        // Clear in database
        var sessions = await _whiteboardService.GetSessionsByWhiteboardAsync(whiteboardId);
        var latestSession = sessions.FirstOrDefault();
        
        if (latestSession != null)
        {
            await _whiteboardService.UpdateSessionDataAsync(latestSession.SessionId, "[]");
        }

        await Clients.OthersInGroup($"whiteboard_{whiteboardId}").SendAsync("CanvasCleared", userId);
    }

    /// <summary>
    /// Request current canvas data
    /// </summary>
    public async Task RequestCanvasData(int whiteboardId)
    {
        var sessions = await _whiteboardService.GetSessionsByWhiteboardAsync(whiteboardId);
        var latestSession = sessions.FirstOrDefault();
        
        if (latestSession != null)
        {
            await Clients.Caller.SendAsync("CanvasDataReceived", latestSession.CanvasData);
        }
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier);
        return int.Parse(userIdClaim?.Value ?? "0");
    }
} 