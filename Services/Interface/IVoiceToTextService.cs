using System.IO;
using System.Threading.Tasks;

namespace Zela.Services.Interface
{
    public interface IVoiceToTextService
    {
        Task<string> ConvertVoiceToTextAsync(Stream audioStream, string language);
    }
} 