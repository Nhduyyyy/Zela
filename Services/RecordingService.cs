using Microsoft.EntityFrameworkCore;
using Zela.DbContext;
using Zela.Models;
using Zela.Services.Interface;

namespace Zela.Services
{
    public class RecordingService : IRecordingService
    {
        private readonly IFileUploadService _fileUploadService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<RecordingService> _logger;

        // Recording validation constants
        private readonly Dictionary<string, long> _maxFileSizes = new()
        {
            { "recording", 500 * 1024 * 1024 }, // 500MB for recordings
            { "screenshot", 50 * 1024 * 1024 }  // 50MB for screenshots
        };

        private readonly string[] _allowedVideoTypes = {
            "video/webm", 
            "video/webm;codecs=vp9", 
            "video/webm;codecs=vp8", 
            "video/webm;codecs=vp9,opus", 
            "video/webm;codecs=vp8,opus",
            "video/mp4", 
            "video/mp4;codecs=h264", 
            "video/mp4;codecs=h264,aac",
            "video/avi", 
            "video/mov", 
            "video/quicktime"
        };

        private readonly string[] _allowedImageTypes = {
            "image/png", "image/jpeg", "image/jpg", "image/webp"
        };

        public RecordingService(
            IFileUploadService fileUploadService,
            ApplicationDbContext context,
            ILogger<RecordingService> logger)
        {
            _fileUploadService = fileUploadService;
            _context = context;
            _logger = logger;
        }

        public async Task<RecordingUploadResult> UploadRecordingAsync(IFormFile file, string type, string meetingCode, int userId, Guid? sessionId = null, int? duration = null, string? metadata = null, string? thumbnailUrl = null)
        {
            try
            {
                _logger.LogInformation("Starting recording upload for user {UserId}, meeting {MeetingCode}, type {Type}", 
                    userId, meetingCode, type);

                // Validate input parameters
                if (file == null || file.Length == 0)
                {
                    return new RecordingUploadResult
                    {
                        Success = false,
                        Error = "No file provided"
                    };
                }

                // Validate file
                var validationResult = ValidateRecordingFile(file, type);
                if (!validationResult.IsValid)
                {
                    return new RecordingUploadResult
                    {
                        Success = false,
                        Error = validationResult.ErrorMessage
                    };
                }

                // Check if user has permission for this meeting
                var hasPermission = await ValidateUserMeetingPermission(userId, meetingCode);
                if (!hasPermission)
                {
                    return new RecordingUploadResult
                    {
                        Success = false,
                        Error = "User does not have permission to upload recordings for this meeting"
                    };
                }

                // Upload file to cloud storage
                var folderPath = $"recordings/{meetingCode}";
                _logger.LogInformation("Uploading to Cloudinary - File: {FileName}, Size: {FileSize}, ContentType: {ContentType}", 
                    file.FileName, file.Length, file.ContentType);
                
                string uploadUrl = null;
                try
                {
                    uploadUrl = await _fileUploadService.UploadAsync(file, folderPath);
                    _logger.LogInformation("Cloudinary upload successful - URL: {UploadUrl}", uploadUrl);
                }
                catch (Exception cloudEx)
                {
                    _logger.LogError(cloudEx, "Cloudinary upload failed for file: {FileName}. Will save to database without cloud URL", file.FileName);
                    // Continue to save to database even if cloud upload fails
                }

                if (string.IsNullOrEmpty(uploadUrl))
                {
                    _logger.LogWarning("No cloud URL available for file: {FileName}. Saving to database with local reference only", file.FileName);
                    uploadUrl = $"local://{file.FileName}"; // Placeholder URL for local files
                }

                // Fix filename extension if needed
                var fixedFileName = FixFileExtension(file.FileName, file.ContentType, type);
                
                // Auto-detect active CallSession for recordings and link them
                Guid? activeSessionId = null;
                if (type.ToLower() == "recording")
                {
                    activeSessionId = await FindActiveSessionForMeetingAsync(meetingCode, userId);
                    if (activeSessionId.HasValue)
                    {
                        _logger.LogInformation("Linking recording to active session {SessionId} for meeting {MeetingCode}", 
                            activeSessionId.Value, meetingCode);
                    }
                    else
                    {
                        _logger.LogInformation("No active CallSession found for meeting {MeetingCode}, recording saved without session link", meetingCode);
                    }
                }
                
                // Create recording record in database
                var recording = new Recording
                {
                    Id = Guid.NewGuid(),
                    FileName = fixedFileName,
                    OriginalFileName = file.FileName,
                    FileUrl = uploadUrl,
                    FileSize = file.Length,
                    ContentType = file.ContentType,
                    RecordingType = type,
                    MeetingCode = meetingCode,
                    SessionId = activeSessionId, // Use detected active session
                    UserId = userId,
                    Duration = duration, // Set duration from frontend
                    Metadata = metadata, // Set metadata from frontend
                    ThumbnailUrl = thumbnailUrl, // Set thumbnail URL from frontend
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.Recordings.Add(recording);
                
                try
                {
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Recording saved to database successfully: {RecordingId}, FileName: {FileName}, Type: {Type}", 
                        recording.Id, recording.FileName, recording.RecordingType);
                }
                catch (Exception dbEx)
                {
                    _logger.LogError(dbEx, "Failed to save recording to database: {RecordingId}", recording.Id);
                    throw new Exception("Failed to save recording to database: " + dbEx.Message);
                }

                // Update CallSession.RecordingUrl after successful database save
                if (activeSessionId.HasValue && type.ToLower() == "recording")
                {
                    var updateSuccess = await SaveSessionRecordingUrlAsync(activeSessionId.Value, uploadUrl);
                    if (updateSuccess)
                    {
                        _logger.LogInformation("CallSession.RecordingUrl updated for session {SessionId}: {RecordingUrl}", 
                            activeSessionId.Value, uploadUrl);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to update CallSession.RecordingUrl for session {SessionId}", activeSessionId.Value);
                    }
                }

                // SessionId already set in recording creation above

                _logger.LogInformation("Recording upload process completed successfully: {RecordingId}", recording.Id);

                return new RecordingUploadResult
                {
                    Success = true,
                    Url = uploadUrl,
                    FileName = file.FileName,
                    FileSize = file.Length,
                    Type = type,
                    MeetingCode = meetingCode,
                    SessionId = sessionId,
                    UploadedAt = recording.CreatedAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading recording for user {UserId}, meeting {MeetingCode}", userId, meetingCode);
                return new RecordingUploadResult
                {
                    Success = false,
                    Error = "An unexpected error occurred while uploading the recording"
                };
            }
        }

        public async Task<List<RecordingHistoryItem>> GetRecordingHistoryAsync(int userId, string? meetingCode = null)
        {
            try
            {
                var query = _context.Recordings
                    .Where(r => r.UserId == userId && r.IsActive);

                if (!string.IsNullOrEmpty(meetingCode))
                {
                    query = query.Where(r => r.MeetingCode == meetingCode);
                }

                var recordings = await query
                    .OrderByDescending(r => r.CreatedAt)
                    .Take(50) // Limit to last 50 recordings
                    .Select(r => new RecordingHistoryItem
                    {
                        Id = r.Id.ToString(),
                        FileName = r.FileName,
                        Url = r.FileUrl,
                        FileSize = r.FileSize,
                        Type = r.RecordingType,
                        MeetingCode = r.MeetingCode,
                        SessionId = r.SessionId,
                        CreatedAt = r.CreatedAt,
                        UserId = r.UserId
                    })
                    .ToListAsync();

                return recordings;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recording history for user {UserId}", userId);
                return new List<RecordingHistoryItem>();
            }
        }

        public async Task<bool> DeleteRecordingAsync(string recordingId, int userId)
        {
            try
            {
                if (!Guid.TryParse(recordingId, out var guid))
                {
                    return false;
                }

                var recording = await _context.Recordings
                    .FirstOrDefaultAsync(r => r.Id == guid && r.UserId == userId && r.IsActive);

                if (recording == null)
                {
                    return false;
                }

                // Delete from cloud storage
                try
                {
                    await _fileUploadService.DeleteAsync(recording.FileUrl);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete file from cloud storage: {FileUrl}", recording.FileUrl);
                    // Continue with database deletion even if cloud deletion fails
                }

                // Soft delete from database
                recording.IsActive = false;
                recording.DeletedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Recording deleted successfully: {RecordingId}", recordingId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting recording {RecordingId} for user {UserId}", recordingId, userId);
                return false;
            }
        }

        public RecordingValidationResult ValidateRecordingFile(IFormFile file, string type)
        {
            // Check file size
            if (!_maxFileSizes.TryGetValue(type.ToLower(), out var maxSize))
            {
                return new RecordingValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Invalid recording type"
                };
            }

            if (file.Length > maxSize)
            {
                var maxSizeMB = maxSize / (1024 * 1024);
                return new RecordingValidationResult
                {
                    IsValid = false,
                    ErrorMessage = $"File size exceeds maximum allowed size of {maxSizeMB}MB",
                    MaxFileSize = maxSize
                };
            }

            // Check content type
            string[] allowedTypes;
            if (type.ToLower() == "recording")
            {
                allowedTypes = _allowedVideoTypes;
            }
            else if (type.ToLower() == "screenshot")
            {
                allowedTypes = _allowedImageTypes;
            }
            else
            {
                return new RecordingValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Invalid recording type. Must be 'recording' or 'screenshot'"
                };
            }

            // More flexible validation for video files
            bool isValidType = false;
            if (type.ToLower() == "recording")
            {
                // For recordings, accept any video/* type or specific allowed types
                isValidType = file.ContentType.ToLower().StartsWith("video/") || 
                             allowedTypes.Contains(file.ContentType.ToLower());
            }
            else
            {
                // For screenshots, use exact match
                isValidType = allowedTypes.Contains(file.ContentType.ToLower());
            }

            if (!isValidType)
            {
                _logger.LogWarning("Invalid file type: {ContentType} for type: {Type}", file.ContentType, type);
                return new RecordingValidationResult
                {
                    IsValid = false,
                    ErrorMessage = $"Invalid file type: {file.ContentType}. Expected video/* for recordings or {string.Join(", ", allowedTypes)} for screenshots",
                    AllowedTypes = allowedTypes
                };
            }

            // Validate file extension
            var extension = Path.GetExtension(file.FileName).ToLower();
            var validExtensions = GetValidExtensions(type);

            if (!validExtensions.Contains(extension))
            {
                return new RecordingValidationResult
                {
                    IsValid = false,
                    ErrorMessage = $"Invalid file extension. Allowed extensions: {string.Join(", ", validExtensions)}"
                };
            }

            return new RecordingValidationResult
            {
                IsValid = true,
                MaxFileSize = maxSize,
                AllowedTypes = allowedTypes
            };
        }

        private async Task<bool> ValidateUserMeetingPermission(int userId, string meetingCode)
        {
            try
            {
                // Check if user is the room creator
                var isCreator = await _context.VideoRooms
                    .AnyAsync(r => r.Password == meetingCode && r.CreatorId == userId);

                if (isCreator)
                {
                    return true;
                }

                // Check if user has participated in the meeting
                var hasParticipated = await _context.RoomParticipants
                    .AnyAsync(p => p.VideoRoom.Password == meetingCode && p.UserId == userId);

                return hasParticipated;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating user meeting permission for user {UserId}, meeting {MeetingCode}", userId, meetingCode);
                return false;
            }
        }

        private string[] GetValidExtensions(string type)
        {
            return type.ToLower() switch
            {
                "recording" => new[] { ".webm", ".mp4", ".avi", ".mov" },
                "screenshot" => new[] { ".png", ".jpg", ".jpeg", ".webp" },
                _ => Array.Empty<string>()
            };
        }

        public async Task<List<RecordingHistoryItem>> GetSessionRecordingsAsync(Guid sessionId, int userId)
        {
            try
            {
                // Get all recordings for the specific session
                var recordings = await _context.Recordings
                    .Where(r => r.SessionId == sessionId && r.IsActive)
                    .OrderByDescending(r => r.CreatedAt)
                    .Select(r => new RecordingHistoryItem
                    {
                        Id = r.Id.ToString(),
                        FileName = r.FileName,
                        Url = r.FileUrl,
                        FileSize = r.FileSize,
                        Type = r.RecordingType,
                        MeetingCode = r.MeetingCode,
                        SessionId = r.SessionId,
                        CreatedAt = r.CreatedAt,
                        UserId = r.UserId
                    })
                    .ToListAsync();

                // Optionally validate user access to this session
                if (recordings.Any())
                {
                    var hasAccess = await ValidateUserSessionAccess(sessionId, userId);
                    if (!hasAccess)
                    {
                        _logger.LogWarning("User {UserId} attempted to access recordings for session {SessionId} without permission", userId, sessionId);
                        return new List<RecordingHistoryItem>();
                    }
                }

                return recordings;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting session recordings for session {SessionId}, user {UserId}", sessionId, userId);
                return new List<RecordingHistoryItem>();
            }
        }

        public async Task<bool> SaveSessionRecordingUrlAsync(Guid sessionId, string recordingUrl)
        {
            try
            {
                var session = await _context.CallSessions
                    .FirstOrDefaultAsync(cs => cs.SessionId == sessionId);

                if (session == null)
                {
                    _logger.LogWarning("CallSession not found: {SessionId}", sessionId);
                    return false;
                }

                session.RecordingUrl = recordingUrl;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Recording URL saved for session {SessionId}: {RecordingUrl}", sessionId, recordingUrl);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving recording URL for session {SessionId}", sessionId);
                return false;
            }
        }

        private async Task<bool> ValidateUserSessionAccess(Guid sessionId, int userId)
        {
            try
            {
                // Check if user participated in the session or is the room creator
                var hasAccess = await _context.CallSessions
                    .Where(cs => cs.SessionId == sessionId)
                    .AnyAsync(cs => cs.VideoRoom.CreatorId == userId ||
                                  cs.Attendances.Any(a => a.UserId == userId));

                return hasAccess;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating user session access for session {SessionId}, user {UserId}", sessionId, userId);
                return false;
            }
        }

        // ======== NEW RECORDING MANAGEMENT METHODS ========

        public async Task<List<Recording>> GetUserRecordingsAsync(int userId)
        {
            try
            {
                var recordings = await _context.Recordings
                    .Where(r => r.UserId == userId && r.IsActive)
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync();

                _logger.LogInformation("Retrieved {Count} recordings for user {UserId}", recordings.Count, userId);
                return recordings;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recordings for user {UserId}", userId);
                throw;
            }
        }

        public async Task<Recording?> GetRecordingByIdAsync(Guid recordingId, int userId)
        {
            try
            {
                var recording = await _context.Recordings
                    .FirstOrDefaultAsync(r => r.Id == recordingId && r.UserId == userId && r.IsActive);

                return recording;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recording {RecordingId} for user {UserId}", recordingId, userId);
                throw;
            }
        }

        public async Task<bool> UpdateRecordingAsync(Guid recordingId, string? description, string? tags, int userId)
        {
            try
            {
                var recording = await _context.Recordings
                    .FirstOrDefaultAsync(r => r.Id == recordingId && r.UserId == userId && r.IsActive);

                if (recording == null)
                {
                    _logger.LogWarning("Recording {RecordingId} not found for user {UserId}", recordingId, userId);
                    return false;
                }

                recording.Description = description;
                recording.Tags = tags;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Updated recording {RecordingId} for user {UserId}", recordingId, userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating recording {RecordingId} for user {UserId}", recordingId, userId);
                throw;
            }
        }

        public async Task UpdateLastAccessedAsync(Guid recordingId, int userId)
        {
            try
            {
                var recording = await _context.Recordings
                    .FirstOrDefaultAsync(r => r.Id == recordingId && r.UserId == userId && r.IsActive);

                if (recording != null)
                {
                    recording.LastAccessedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating last accessed for recording {RecordingId}", recordingId);
                // Don't throw - this is not critical
            }
        }

        public async Task TrackDownloadAsync(Guid recordingId, int userId)
        {
            try
            {
                var recording = await _context.Recordings
                    .FirstOrDefaultAsync(r => r.Id == recordingId && r.UserId == userId && r.IsActive);

                if (recording != null)
                {
                    recording.DownloadCount = recording.DownloadCount + 1;
                    recording.LastAccessedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Tracked download for recording {RecordingId}, new count: {Count}", 
                        recordingId, recording.DownloadCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error tracking download for recording {RecordingId}", recordingId);
                // Don't throw - this is not critical
            }
        }

        public async Task RollbackDownloadAsync(Guid recordingId, int userId)
        {
            try
            {
                var recording = await _context.Recordings
                    .FirstOrDefaultAsync(r => r.Id == recordingId && r.UserId == userId && r.IsActive);

                if (recording != null && recording.DownloadCount > 0)
                {
                    recording.DownloadCount = recording.DownloadCount - 1;
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Rolled back download for recording {RecordingId}, new count: {Count}", 
                        recordingId, recording.DownloadCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rolling back download for recording {RecordingId}", recordingId);
                // Don't throw - this is not critical
            }
        }

        public async Task<bool> DeleteRecordingAsync(Guid recordingId, int userId)
        {
            try
            {
                var recording = await _context.Recordings
                    .FirstOrDefaultAsync(r => r.Id == recordingId && r.UserId == userId && r.IsActive);

                if (recording == null)
                {
                    _logger.LogWarning("Recording {RecordingId} not found for user {UserId}", recordingId, userId);
                    return false;
                }

                // Soft delete
                recording.IsActive = false;
                recording.DeletedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Soft deleted recording {RecordingId} for user {UserId}", recordingId, userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting recording {RecordingId} for user {UserId}", recordingId, userId);
                throw;
            }
        }

        private async Task<Guid?> FindActiveSessionForMeetingAsync(string meetingCode, int userId)
        {
            try
            {
                _logger.LogInformation("🔍 Searching for active session - MeetingCode: {MeetingCode}, UserId: {UserId}", meetingCode, userId);
                
                // Find the room first
                var room = await _context.VideoRooms
                    .FirstOrDefaultAsync(vr => vr.Password == meetingCode);

                if (room == null)
                {
                    _logger.LogWarning("❌ Room not found for meeting code: {MeetingCode}", meetingCode);
                    return null;
                }

                _logger.LogInformation("✅ Room found: RoomId {RoomId} for meeting {MeetingCode}", room.RoomId, meetingCode);

                // Find active CallSession for this room
                var activeSession = await _context.CallSessions
                    .Where(cs => cs.RoomId == room.RoomId && cs.EndedAt == null)
                    .OrderByDescending(cs => cs.StartedAt)
                    .FirstOrDefaultAsync();

                if (activeSession != null)
                {
                    _logger.LogInformation("✅ Found active session {SessionId} for meeting {MeetingCode}, RoomId: {RoomId}", 
                        activeSession.SessionId, meetingCode, room.RoomId);
                    return activeSession.SessionId;
                }

                // Debug: Check all sessions for this room
                var allSessions = await _context.CallSessions
                    .Where(cs => cs.RoomId == room.RoomId)
                    .OrderByDescending(cs => cs.StartedAt)
                    .Take(5)
                    .ToListAsync();

                _logger.LogWarning("❌ No active session found for meeting {MeetingCode}, RoomId: {RoomId}", meetingCode, room.RoomId);
                _logger.LogInformation("📊 Recent sessions for this room: {SessionCount}", allSessions.Count);
                
                foreach (var session in allSessions)
                {
                    _logger.LogInformation("📋 Session {SessionId}: Started={StartedAt}, Ended={EndedAt}", 
                        session.SessionId, session.StartedAt, session.EndedAt?.ToString() ?? "NULL");
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Error finding active session for meeting {MeetingCode}, user {UserId}", meetingCode, userId);
                return null;
            }
        }

        private string FixFileExtension(string fileName, string contentType, string type)
        {
            try
            {
                if (type.ToLower() != "recording" || string.IsNullOrEmpty(contentType))
                    return fileName;

                var currentExtension = Path.GetExtension(fileName).ToLower();
                var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
                
                // Determine correct extension based on content type
                string correctExtension = contentType.ToLower() switch
                {
                    var ct when ct.Contains("mp4") => ".mp4",
                    var ct when ct.Contains("webm") => ".webm",
                    var ct when ct.Contains("avi") => ".avi",
                    var ct when ct.Contains("mov") || ct.Contains("quicktime") => ".mov",
                    _ => currentExtension // Keep original if unknown
                };

                // If extension is already correct, return original filename
                if (currentExtension == correctExtension)
                    return fileName;

                // Return filename with corrected extension
                var fixedFileName = nameWithoutExtension + correctExtension;
                _logger.LogInformation("Fixed filename extension: {OriginalFileName} -> {FixedFileName} (ContentType: {ContentType})", 
                    fileName, fixedFileName, contentType);
                
                return fixedFileName;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error fixing file extension for {FileName}, using original", fileName);
                return fileName;
            }
        }
    }
} 