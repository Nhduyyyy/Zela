using Microsoft.EntityFrameworkCore;
using Zela.DbContext;
using Zela.Models;
using Zela.Services.Interface;

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