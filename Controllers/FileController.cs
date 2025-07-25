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
            // Khởi tạo ViewModel để truyền dữ liệu ra view
            var vm = new FileSummaryViewModel
            {
                FileUrl = url,
                FileName = filename
            };
            try
            {
                string filePath;
                // Kiểm tra nếu url là đường dẫn http/https thì dùng trực tiếp, ngược lại chuyển sang đường dẫn vật lý
                if (url.StartsWith("http://") || url.StartsWith("https://"))
                {
                    filePath = url;
                }
                else
                {
                    filePath = GetPhysicalPathFromUrl(url);
                }
                // Gọi hàm trích xuất nội dung xem trước file (tối đa 500 ký tự)
                vm.PreviewContent = await FileContentExtractor.ExtractPreviewAsync(filePath, filename);
            }
            catch (Exception ex)
            {
                // Nếu có lỗi, gán thông báo lỗi vào ViewModel
                vm.Error = "Không thể xem trước file: " + ex.Message;
            }
            // Trả về partial view hiển thị nội dung xem trước
            return PartialView("_FilePreview", vm);
        }

        [HttpPost]
        public async Task<IActionResult> Summarize([FromForm] string url, [FromForm] string filename)
        {
            // Khởi tạo ViewModel để truyền dữ liệu ra view
            var vm = new FileSummaryViewModel
            {
                FileUrl = url,
                FileName = filename
            };

            // 1. Kiểm tra đã có bản tóm tắt chưa trong DB
            var summaryEntity = await _fileSummaryService.GetSummaryAsync(url, filename);
            if (summaryEntity != null)
            {
                // Nếu đã có, lấy nội dung tóm tắt và trả về view
                vm.SummaryContent = summaryEntity.SummaryContent;
                return PartialView("_FileSummary", vm);
            }

            // 2. Nếu chưa có, thực hiện tóm tắt và lưu lại
            try
            {
                // Xác định đường dẫn vật lý hoặc url file
                string filePath = url.StartsWith("http") ? url : GetPhysicalPathFromUrl(url);
                // Trích xuất toàn bộ nội dung file (giới hạn 6000 ký tự nếu quá dài)
                var content = await FileContentExtractor.ExtractTextAsync(filePath, filename);
                if (content.Length > 6000) content = content.Substring(0, 6000);
                // Gọi ChatGPT để tóm tắt nội dung file
                var summary = await _chatGPTService.SummarizeAsync(content);

                // Lưu bản tóm tắt vào DB
                await _fileSummaryService.SaveSummaryAsync(url, filename, summary);

                // Gán nội dung tóm tắt vào ViewModel
                vm.SummaryContent = summary;
            }
            catch (Exception ex)
            {
                // Nếu có lỗi, gán thông báo lỗi vào ViewModel
                vm.Error = "Không thể tóm tắt file: " + ex.Message;
            }
            // Trả về partial view hiển thị nội dung tóm tắt
            return PartialView("_FileSummary", vm);
        }

        // Hàm chuyển đổi url file thành đường dẫn vật lý trên server
        private string GetPhysicalPathFromUrl(string url)
        {
            var wwwroot = Path.Combine(_env.ContentRootPath, "wwwroot");
            var rel = url.StartsWith("/") ? url.Substring(1) : url;
            return Path.Combine(wwwroot, rel.Replace("/", Path.DirectorySeparatorChar.ToString()));
        }
    }

    // Lớp hỗ trợ trích xuất nội dung file (txt, docx) để xem trước hoặc tóm tắt
    public static class FileContentExtractor
    {
        // Trích xuất toàn bộ nội dung file (txt, docx) để tóm tắt
        public static async Task<string> ExtractTextAsync(string filePathOrUrl, string filename)
        {
            var ext = Path.GetExtension(filename).ToLower();
            // Nếu là url, tải file về tạm thời rồi đọc nội dung
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
                    // Đọc nội dung file tạm
                    var result = await ExtractTextAsync(tempFile, filename);
                    Console.WriteLine("[ExtractTextAsync] Đã đọc nội dung file tạm.");
                    return result;
                }
                finally
                {
                    // Xóa file tạm sau khi đọc xong
                    System.IO.File.Delete(tempFile);
                    Console.WriteLine($"[ExtractTextAsync] Đã xóa file tạm: {tempFile}");
                }
            }
            Console.WriteLine($"[ExtractTextAsync] Đọc file local: {filePathOrUrl}");
            // Đọc file local theo định dạng
            switch (ext)
            {
                case ".txt":
                    // Đọc toàn bộ nội dung file txt
                    return await System.IO.File.ReadAllTextAsync(filePathOrUrl);
                case ".docx":
                    // Đọc toàn bộ nội dung file docx
                    return ExtractDocxText(filePathOrUrl);
                case ".doc":
                    // Không hỗ trợ file .doc cũ
                    throw new NotSupportedException("Không hỗ trợ file .doc cũ.");
                default:
                    // Không hỗ trợ các định dạng khác
                    throw new NotSupportedException("Định dạng file không hỗ trợ.");
            }
        }
        // Trích xuất nội dung xem trước (tối đa 500 ký tự đầu) cho file
        public static async Task<string> ExtractPreviewAsync(string filePathOrUrl, string filename)
        {
            var ext = Path.GetExtension(filename).ToLower();
            // Nếu là url, tải file về tạm thời rồi đọc nội dung xem trước
            if (filePathOrUrl.StartsWith("http://") || filePathOrUrl.StartsWith("https://"))
            {
                using var httpClient = new HttpClient();
                var bytes = await httpClient.GetByteArrayAsync(filePathOrUrl);
                var tempFile = Path.GetTempFileName() + ext;
                await System.IO.File.WriteAllBytesAsync(tempFile, bytes);
                try
                {
                    // Đọc nội dung xem trước từ file tạm
                    return ExtractPreview(tempFile, filename);
                }
                finally
                {
                    // Xóa file tạm sau khi đọc xong
                    System.IO.File.Delete(tempFile);
                }
            }
            // Đọc nội dung xem trước từ file local
            return ExtractPreview(filePathOrUrl, filename);
        }
        // Đọc nội dung xem trước (tối đa 500 ký tự đầu) từ file local
        public static string ExtractPreview(string filePath, string filename)
        {
            var ext = Path.GetExtension(filename).ToLower();
            string content = ext switch
            {
                ".txt" => System.IO.File.ReadAllText(filePath),
                ".docx" => ExtractDocxText(filePath),
                _ => throw new NotSupportedException("Định dạng file không hỗ trợ.")
            };
            // Lấy 500 ký tự đầu tiên, giữ nguyên định dạng
            if (content.Length > 500)
                content = content.Substring(0, 500) + "...";
            return content;
        }
        // Đọc toàn bộ nội dung file docx
        private static string ExtractDocxText(string filePath)
        {
            using (var doc = WordprocessingDocument.Open(filePath, false))
            {
                return doc.MainDocumentPart.Document.Body.InnerText;
            }
        }
    }

    // Service thao tác với DB để lấy/lưu bản tóm tắt file
    public class FileSummaryService : IFileSummaryService
    {
        private readonly ApplicationDbContext _db;
        public FileSummaryService(ApplicationDbContext db) { _db = db; }

        // Lấy bản tóm tắt file từ DB nếu đã có
        public async Task<FileSummary> GetSummaryAsync(string fileUrl, string fileName)
        {
            return await _db.FileSummaries
                .FirstOrDefaultAsync(x => x.FileUrl == fileUrl && x.FileName == fileName);
        }

        // Lưu bản tóm tắt file mới vào DB
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