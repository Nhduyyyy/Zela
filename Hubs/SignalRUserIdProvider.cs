using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

public class SignalRUserIdProvider : IUserIdProvider
{
    public string GetUserId(HubConnectionContext connection)
    {
        // Debug: Log tất cả claims để xem có gì
        if (connection.User?.Claims != null)
        {
            foreach (var claim in connection.User.Claims)
            {
                Console.WriteLine($"Claim: {claim.Type} = {claim.Value}");
            }
        }

        // Thử nhiều cách để lấy UserId
        var userId = connection.User?.FindFirst("UserId")?.Value;
        
        if (string.IsNullOrEmpty(userId))
        {
            // Thử lấy từ NameIdentifier
            userId = connection.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
        
        if (string.IsNullOrEmpty(userId))
        {
            // Thử lấy từ Name
            userId = connection.User?.FindFirst(ClaimTypes.Name)?.Value;
        }
        
        if (string.IsNullOrEmpty(userId))
        {
            // Thử lấy từ Email
            var email = connection.User?.FindFirst(ClaimTypes.Email)?.Value;
            if (!string.IsNullOrEmpty(email))
            {
                // Tìm UserId từ email (cần implement logic này)
                userId = GetUserIdFromEmail(email);
            }
        }

        Console.WriteLine($"SignalRUserIdProvider: Final userId = {userId}");
        return userId;
    }
    
    private string GetUserIdFromEmail(string email)
    {
        // TODO: Implement logic để lấy UserId từ email
        // Có thể inject DbContext hoặc UserService để tìm user
        return null;
    }
}