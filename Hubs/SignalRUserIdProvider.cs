using Microsoft.AspNetCore.SignalR;

public class SignalRUserIdProvider : IUserIdProvider
{
    public string GetUserId(HubConnectionContext connection)
    {
        return connection.User?.FindFirst("UserId")?.Value;
    }
}