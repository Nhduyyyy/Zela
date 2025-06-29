namespace Zela.ViewModels
{
    public class UpdateRecordingRequest
    {
        public Guid RecordingId { get; set; }
        public string? Description { get; set; }
        public string? Tags { get; set; }
    }

    public class UpdateLastAccessedRequest
    {
        public Guid RecordingId { get; set; }
    }

    public class TrackDownloadRequest
    {
        public Guid RecordingId { get; set; }
    }
} 