namespace Zela.Services.Interface
{
    public interface IFileSummaryService
    {
        Task<FileSummary> GetSummaryAsync(string fileUrl, string fileName);

        Task<FileSummary> SaveSummaryAsync(string fileUrl, string fileName, string summary);
    }
}

