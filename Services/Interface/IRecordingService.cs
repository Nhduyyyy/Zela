using Zela.Models;

namespace Zela.Services.Interface
{
    public interface IRecordingService
    {
        /// <summary>
        /// Upload recording or screenshot file to cloud storage
        /// </summary>
        /// <param name="file">The file to upload</param>
        /// <param name="type">Type of file: "recording" or "screenshot"</param>
        /// <param name="meetingCode">Meeting code for organization</param>
        /// <param name="userId">User ID for security validation</param>
        /// <param name="sessionId">Optional: Session ID to link recording to specific call session</param>
        /// <returns>Upload result with file URL and metadata</returns>
        Task<RecordingUploadResult> UploadRecordingAsync(IFormFile file, string type, string meetingCode, int userId, Guid? sessionId = null, int? duration = null, string? metadata = null, string? thumbnailUrl = null);
        
        /// <summary>
        /// Get recording history for a user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="meetingCode">Optional meeting code filter</param>
        /// <returns>List of user's recordings</returns>
        Task<List<RecordingHistoryItem>> GetRecordingHistoryAsync(int userId, string? meetingCode = null);
        
        /// <summary>
        /// Delete a recording file
        /// </summary>
        /// <param name="recordingId">Recording ID to delete</param>
        /// <param name="userId">User ID for authorization</param>
        /// <returns>Success status</returns>
        Task<bool> DeleteRecordingAsync(string recordingId, int userId);
        
        /// <summary>
        /// Validate recording file before upload
        /// </summary>
        /// <param name="file">File to validate</param>
        /// <param name="type">File type</param>
        /// <returns>Validation result</returns>
        RecordingValidationResult ValidateRecordingFile(IFormFile file, string type);
        
        /// <summary>
        /// Get recordings for a specific call session
        /// </summary>
        /// <param name="sessionId">Session ID</param>
        /// <param name="userId">User ID for authorization</param>
        /// <returns>List of recordings for the session</returns>
        Task<List<RecordingHistoryItem>> GetSessionRecordingsAsync(Guid sessionId, int userId);
        
        /// <summary>
        /// Save main recording URL to CallSession (automatic recording)
        /// </summary>
        /// <param name="sessionId">Session ID</param>
        /// <param name="recordingUrl">Recording URL</param>
        /// <returns>Success status</returns>
        Task<bool> SaveSessionRecordingUrlAsync(Guid sessionId, string recordingUrl);

        /// <summary>
        /// Gets all recordings for a specific user (for recordings management page)
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>List of user's recordings</returns>
        Task<List<Recording>> GetUserRecordingsAsync(int userId);

        /// <summary>
        /// Gets a specific recording by ID
        /// </summary>
        /// <param name="recordingId">Recording ID</param>
        /// <param name="userId">User ID to verify access</param>
        /// <returns>Recording details or null if not found</returns>
        Task<Recording?> GetRecordingByIdAsync(Guid recordingId, int userId);

        /// <summary>
        /// Updates recording description and tags
        /// </summary>
        /// <param name="recordingId">Recording ID</param>
        /// <param name="description">New description</param>
        /// <param name="tags">New tags</param>
        /// <param name="userId">User ID to verify ownership</param>
        /// <returns>True if update was successful</returns>
        Task<bool> UpdateRecordingAsync(Guid recordingId, string? description, string? tags, int userId);

        /// <summary>
        /// Updates last accessed timestamp for a recording
        /// </summary>
        /// <param name="recordingId">Recording ID</param>
        /// <param name="userId">User ID</param>
        /// <returns>Task</returns>
        Task UpdateLastAccessedAsync(Guid recordingId, int userId);

        /// <summary>
        /// Tracks download event for a recording
        /// </summary>
        /// <param name="recordingId">Recording ID</param>
        /// <param name="userId">User ID</param>
        /// <returns>Task</returns>
        Task TrackDownloadAsync(Guid recordingId, int userId);

        /// <summary>
        /// Deletes a recording by Guid (overload for new endpoints)
        /// </summary>
        /// <param name="recordingId">Recording Guid ID to delete</param>
        /// <param name="userId">User ID to verify ownership/access</param>
        /// <returns>True if deletion was successful</returns>
        Task<bool> DeleteRecordingAsync(Guid recordingId, int userId);

        /// <summary>
        /// Rolls back download count if download fails
        /// </summary>
        /// <param name="recordingId">Recording ID</param>
        /// <param name="userId">User ID</param>
        /// <returns>Task</returns>
        Task RollbackDownloadAsync(Guid recordingId, int userId);
    }

    public class RecordingUploadResult
    {
        public bool Success { get; set; }
        public string? Url { get; set; }
        public string? FileName { get; set; }
        public long FileSize { get; set; }
        public string? Type { get; set; }
        public string? MeetingCode { get; set; }
        public Guid? SessionId { get; set; }
        public DateTime UploadedAt { get; set; }
        public string? Error { get; set; }
    }

    public class RecordingHistoryItem
    {
        public string Id { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string Type { get; set; } = string.Empty;
        public string MeetingCode { get; set; } = string.Empty;
        public Guid? SessionId { get; set; }
        public DateTime CreatedAt { get; set; }
        public int UserId { get; set; }
    }

    public class RecordingValidationResult
    {
        public bool IsValid { get; set; }
        public string? ErrorMessage { get; set; }
        public long MaxFileSize { get; set; }
        public string[] AllowedTypes { get; set; } = Array.Empty<string>();
    }
} 