using Microsoft.AspNetCore.Mvc;
using Zela.ViewModels;
using Zela.Services.Interface;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Packaging;
using System;
using System.Net.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Zela.DbContext;

namespace Zela.Controllers
{
    public class FileController : Controller
    {
        private readonly IWebHostEnvironment _env;
        private readonly IChatGPTService _chatGPTService;
        private readonly IFileSummaryService _fileSummaryService;
        public FileController(IWebHostEnvironment env, IChatGPTService chatGPTService, IFileSummaryService fileSummaryService)
        {
            _env = env;
            _chatGPTService = chatGPTService;
            _fileSummaryService = fileSummaryService;
        }

        [HttpGet]
        public async Task<IActionResult> Preview(string url, string filename)
        {
            var vm = new FileSummaryViewModel
            {
                FileUrl = url,
                FileName = filename
            };
            try
            {
                string filePath;
                if (url.StartsWith("http://") || url.StartsWith("https://"))
                {
                    filePath = url;
                }
                else
                {
                    filePath = GetPhysicalPathFromUrl(url);
                }
                vm.PreviewContent = await FileContentExtractor.ExtractPreviewAsync(filePath, filename);
            }
            catch (Exception ex)
            {
                vm.Error = "Không thể xem trước file: " + ex.Message;
            }
            return PartialView("_FilePreview", vm);
        }

        [HttpPost]
        public async Task<IActionResult> Summarize([FromForm] string url, [FromForm] string filename)
        {
            var vm = new FileSummaryViewModel
            {
                FileUrl = url,
                FileName = filename
            };

            // 1. Kiểm tra đã có bản tóm tắt chưa
            var summaryEntity = await _fileSummaryService.GetSummaryAsync(url, filename);
            if (summaryEntity != null)
            {
                vm.SummaryContent = summaryEntity.SummaryContent;
                return PartialView("_FileSummary", vm);
            }

            // 2. Nếu chưa có, thực hiện tóm tắt và lưu lại
            try
            {
                string filePath = url.StartsWith("http") ? url : GetPhysicalPathFromUrl(url);
                var content = await FileContentExtractor.ExtractTextAsync(filePath, filename);
                if (content.Length > 6000) content = content.Substring(0, 6000);
                var summary = await _chatGPTService.SummarizeAsync(content);

                // Lưu vào DB
                await _fileSummaryService.SaveSummaryAsync(url, filename, summary);

                vm.SummaryContent = summary;
            }
            catch (Exception ex)
            {
                vm.Error = "Không thể tóm tắt file: " + ex.Message;
            }
            return PartialView("_FileSummary", vm);
        }

        private string GetPhysicalPathFromUrl(string url)
        {
            var wwwroot = Path.Combine(_env.ContentRootPath, "wwwroot");
            var rel = url.StartsWith("/") ? url.Substring(1) : url;
            return Path.Combine(wwwroot, rel.Replace("/", Path.DirectorySeparatorChar.ToString()));
        }
    }

    public static class FileContentExtractor
    {
        public static async Task<string> ExtractTextAsync(string filePathOrUrl, string filename)
        {
            var ext = Path.GetExtension(filename).ToLower();
            if (filePathOrUrl.StartsWith("http://") || filePathOrUrl.StartsWith("https://"))
            {
                Console.WriteLine($"[ExtractTextAsync] Tải file từ URL: {filePathOrUrl}");
                // Tải file về tạm thời
                using var httpClient = new HttpClient();
                var bytes = await httpClient.GetByteArrayAsync(filePathOrUrl);
                var tempFile = Path.GetTempFileName() + ext;
                await System.IO.File.WriteAllBytesAsync(tempFile, bytes);
                Console.WriteLine($"[ExtractTextAsync] Đã lưu file tạm: {tempFile}");
                try
                {
                    var result = await ExtractTextAsync(tempFile, filename);
                    Console.WriteLine("[ExtractTextAsync] Đã đọc nội dung file tạm.");
                    return result;
                }
                finally
                {
                    System.IO.File.Delete(tempFile);
                    Console.WriteLine($"[ExtractTextAsync] Đã xóa file tạm: {tempFile}");
                }
            }
            Console.WriteLine($"[ExtractTextAsync] Đọc file local: {filePathOrUrl}");
            // Đọc file local như cũ
            switch (ext)
            {
                case ".txt":
                    return await System.IO.File.ReadAllTextAsync(filePathOrUrl);
                case ".docx":
                    return ExtractDocxText(filePathOrUrl);
                case ".doc":
                    throw new NotSupportedException("Không hỗ trợ file .doc cũ.");
                default:
                    throw new NotSupportedException("Định dạng file không hỗ trợ.");
            }
        }
        public static async Task<string> ExtractPreviewAsync(string filePathOrUrl, string filename)
        {
            var ext = Path.GetExtension(filename).ToLower();
            if (filePathOrUrl.StartsWith("http://") || filePathOrUrl.StartsWith("https://"))
            {
                using var httpClient = new HttpClient();
                var bytes = await httpClient.GetByteArrayAsync(filePathOrUrl);
                var tempFile = Path.GetTempFileName() + ext;
                await System.IO.File.WriteAllBytesAsync(tempFile, bytes);
                try
                {
                    return ExtractPreview(tempFile, filename);
                }
                finally
                {
                    System.IO.File.Delete(tempFile);
                }
            }
            return ExtractPreview(filePathOrUrl, filename);
        }
        public static string ExtractPreview(string filePath, string filename)
        {
            var ext = Path.GetExtension(filename).ToLower();
            string content = ext switch
            {
                ".txt" => System.IO.File.ReadAllText(filePath),
                ".docx" => ExtractDocxText(filePath),
                _ => throw new NotSupportedException("Định dạng file không hỗ trợ.")
            };
            // Lấy 100 ký tự đầu tiên, giữ nguyên định dạng
            if (content.Length > 500)
                content = content.Substring(0, 500) + "...";
            return content;
        }
        private static string ExtractDocxText(string filePath)
        {
            using (var doc = WordprocessingDocument.Open(filePath, false))
            {
                return doc.MainDocumentPart.Document.Body.InnerText;
            }
        }
    }

    public class FileSummaryService : IFileSummaryService
    {
        private readonly ApplicationDbContext _db;
        public FileSummaryService(ApplicationDbContext db) { _db = db; }

        public async Task<FileSummary> GetSummaryAsync(string fileUrl, string fileName)
        {
            return await _db.FileSummaries
                .FirstOrDefaultAsync(x => x.FileUrl == fileUrl && x.FileName == fileName);
        }

        public async Task<FileSummary> SaveSummaryAsync(string fileUrl, string fileName, string summary)
        {
            var entity = new FileSummary
            {
                FileUrl = fileUrl,
                FileName = fileName,
                SummaryContent = summary,
                CreatedAt = DateTime.UtcNow
            };
            _db.FileSummaries.Add(entity);
            await _db.SaveChangesAsync();
            return entity;
        }
    }
} 