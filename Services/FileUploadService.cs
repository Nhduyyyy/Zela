using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;

public class FileUploadService : IFileUploadService
{
    private readonly Cloudinary _cloudinary;

    public FileUploadService(IConfiguration config)
    {
        // Khởi tạo đối tượng Cloudinary với thông tin cấu hình từ appsettings
        var acc = new Account(
            config["Cloudinary:CloudName"]!,
            config["Cloudinary:ApiKey"]!,
            config["Cloudinary:ApiSecret"]!
        );
        _cloudinary = new Cloudinary(acc);
    }

    // Upload file lên Cloudinary, trả về URL file đã upload
    public async Task<string> UploadAsync(IFormFile file, string folder = null)
    {
        // Tạo publicId duy nhất cho file, có thể kèm folder nếu truyền vào
        var publicId = string.IsNullOrWhiteSpace(folder)
            ? Guid.NewGuid().ToString("N")
            : $"{folder.TrimEnd('/')}/{Guid.NewGuid():N}";

        UploadResult result;
        try
        {
            // Xác định loại file để upload đúng kiểu (image, video, raw)
            if (file.ContentType.StartsWith("image/"))
            {
                var parms = new ImageUploadParams
                {
                    File     = new FileDescription(file.FileName, file.OpenReadStream()),
                    PublicId = publicId
                };
                result = await _cloudinary.UploadAsync(parms);
            }
            else if (file.ContentType.StartsWith("video/"))
            {
                var parms = new VideoUploadParams
                {
                    File     = new FileDescription(file.FileName, file.OpenReadStream()),
                    PublicId = publicId
                };
                result = await _cloudinary.UploadAsync(parms);
            }
            else
            {
                var parms = new RawUploadParams
                {
                    File     = new FileDescription(file.FileName, file.OpenReadStream()),
                    PublicId = publicId
                };
                result = await _cloudinary.UploadAsync(parms);
            }
        }
        catch (Exception ex)
        {
            // Log lỗi upload ra console và ném lại exception
            Console.WriteLine($"[Cloudinary Upload ERROR] File: {file.FileName}, Type: {file.ContentType}, Error: {ex.Message}");
            throw;
        }

        // Kiểm tra kết quả trả về từ Cloudinary, nếu có lỗi thì ném exception
        if (result.Error is not null)
        {
            Console.WriteLine($"[Cloudinary Upload RESULT ERROR] File: {file.FileName}, Type: {file.ContentType}, Error: {result.Error.Message}");
            throw new Exception($"Upload lỗi: {result.Error.Message}");
        }

        // Log thành công và trả về URL file đã upload
        Console.WriteLine($"[Cloudinary Upload SUCCESS] File: {file.FileName}, Type: {file.ContentType}, Url: {result.SecureUrl}");
        return result.SecureUrl.ToString();
    }

    // Xóa file khỏi Cloudinary dựa vào URL
    public async Task DeleteAsync(string fileUrl)
    {
        // Lấy public_id từ URL file Cloudinary
        var uri = new Uri(fileUrl);
        var path = uri.AbsolutePath.TrimStart('/');
        var parts = path.Split('/');
        var idx = Array.IndexOf(parts, "upload");
        var publicIdWithVersion = string.Join('/', parts[(idx + 2)..]);
        var publicId = Path.ChangeExtension(publicIdWithVersion, null);

        // Xác định loại resource (image, video, raw) dựa vào URL
        ResourceType rType =
            fileUrl.Contains("/image/") ? ResourceType.Image :
            fileUrl.Contains("/video/") ? ResourceType.Video :
            ResourceType.Raw;

        // Tạo tham số xóa và gọi API Cloudinary
        var delParams = new DeletionParams(publicId)
        {
            ResourceType = rType
        };
        var delResult = await _cloudinary.DestroyAsync(delParams);
        // Nếu xóa không thành công thì ném exception
        if (delResult.Result != "ok")
            throw new Exception($"Xóa lỗi: {delResult.Result}");
    }
}