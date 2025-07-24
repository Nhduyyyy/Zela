using Microsoft.EntityFrameworkCore;
using Zela.DbContext;
using Zela.Models;
using Zela.Services.Interface;

public class FileSummaryService : IFileSummaryService
{
    private readonly ApplicationDbContext _db;
    public FileSummaryService(ApplicationDbContext db) { _db = db; }

    // Lấy bản tóm tắt file từ DB dựa vào url và tên file
    public async Task<FileSummary> GetSummaryAsync(string fileUrl, string fileName)
    {
        // Truy vấn DB để lấy bản tóm tắt đầu tiên khớp với url và tên file
        return await _db.FileSummaries
            .FirstOrDefaultAsync(x => x.FileUrl == fileUrl && x.FileName == fileName);
    }

    // Lưu bản tóm tắt file mới vào DB
    public async Task<FileSummary> SaveSummaryAsync(string fileUrl, string fileName, string summary)
    {
        // Tạo entity FileSummary mới với thông tin truyền vào
        var entity = new FileSummary
        {
            FileUrl = fileUrl,
            FileName = fileName,
            SummaryContent = summary,
            CreatedAt = DateTime.UtcNow
        };
        // Thêm entity vào DB và lưu thay đổi
        _db.FileSummaries.Add(entity);
        await _db.SaveChangesAsync();
        // Trả về entity vừa lưu
        return entity;
    }
}