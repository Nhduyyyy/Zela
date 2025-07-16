public class FileSummary
{
    public int Id { get; set; }
    public string FileUrl { get; set; }      // hoặc FileId nếu có
    public string FileName { get; set; }
    public string SummaryContent { get; set; }
    public DateTime CreatedAt { get; set; }
}