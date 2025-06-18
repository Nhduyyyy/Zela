using Zela.Services.Dto;

namespace Zela.Services
{
    public interface IMeetingService
    {
        Task<string> CreateMeetingAsync(int creatorId);
        Task<JoinResult> JoinMeetingAsync(string password);
        
        Task<bool> IsHostAsync(string password, int userId);
        
        Task CloseMeetingAsync(string password);
    }
}