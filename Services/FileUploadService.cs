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
        var acc = new Account(
            config["Cloudinary:CloudName"]!,
            config["Cloudinary:ApiKey"]!,
            config["Cloudinary:ApiSecret"]!
        );
        _cloudinary = new Cloudinary(acc);
    }

    public async Task<string> UploadAsync(IFormFile file, string folder = null)
    {
        var publicId = string.IsNullOrWhiteSpace(folder)
            ? Guid.NewGuid().ToString("N")
            : $"{folder.TrimEnd('/')}/{Guid.NewGuid():N}";

        UploadResult result;
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

        if (result.Error is not null)
            throw new Exception($"Upload lỗi: {result.Error.Message}");

        return result.SecureUrl.ToString();
    }

    public async Task DeleteAsync(string fileUrl)
    {
        // Lấy public_id từ URL
        var uri = new Uri(fileUrl);
        var path = uri.AbsolutePath.TrimStart('/');
        var parts = path.Split('/');
        var idx = Array.IndexOf(parts, "upload");
        var publicIdWithVersion = string.Join('/', parts[(idx + 2)..]);
        var publicId = Path.ChangeExtension(publicIdWithVersion, null);

        ResourceType rType =
            fileUrl.Contains("/image/") ? ResourceType.Image :
            fileUrl.Contains("/video/") ? ResourceType.Video :
            ResourceType.Raw;

        var delParams = new DeletionParams(publicId)
        {
            ResourceType = rType
        };
        var delResult = await _cloudinary.DestroyAsync(delParams);
        if (delResult.Result != "ok")
            throw new Exception($"Xóa lỗi: {delResult.Result}");
    }
}