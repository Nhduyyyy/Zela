public interface IFileUploadService
{
    Task<string> UploadAsync(IFormFile file, string folder = null);
    Task DeleteAsync(string fileUrl);
}