using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Features;
using Zela.Models.ViewModels;
using Zela.Services;
using Zela.Services.Interface;
using Zela.ViewModels;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Zela.Controllers
{
    public class MeetingController : Controller
    {
        // Khai báo biến thành viên (field) để lưu các service xử lý logic họp và ghi âm/ghi hình
        private readonly IMeetingService _meetingService; // Service xử lý các nghiệp vụ liên quan đến Meeting (phòng họp)
        private readonly IRecordingService _recordingService; // Service xử lý các nghiệp vụ liên quan đến Recording (ghi âm/ghi hình)
        private readonly IMeetingRoomMessageService _meetingRoomMessageService; // Service xử lý tin nhắn trong phòng
        private readonly IAudioTranscriptionService _audioTranscriptionService; // Service xử lý audio transcription
        private readonly IChatGPTService _chatGPTService; // Service xử lý translation

        // Constructor của MeetingController, được gọi khi controller này được khởi tạo
        public MeetingController(IMeetingService meetingService, IRecordingService recordingService, IMeetingRoomMessageService meetingRoomMessageService, IAudioTranscriptionService audioTranscriptionService, IChatGPTService chatGPTService)
        {
            // Gán các service được inject từ bên ngoài vào biến thành viên để sử dụng trong các action của controller
            _meetingService = meetingService;
            _recordingService = recordingService;
            _meetingRoomMessageService = meetingRoomMessageService;
            _audioTranscriptionService = audioTranscriptionService;
            _chatGPTService = chatGPTService;
        }

        // Đánh dấu action này sẽ xử lý các HTTP GET request đến /Meeting hoặc /Meeting/Index
        [HttpGet]
        // Action method hiển thị trang chính của module Meeting
        // Khi được gọi, nó sẽ render file Views/Meeting/Index.cshtml cho người dùng
        public IActionResult Index() => View();

        // Đánh dấu action này sẽ xử lý HTTP GET request đến /Meeting/Create
        [HttpGet]
        // Action method hiển thị form tạo cuộc họp mới
        public IActionResult Create()
        {
            // Tạo một ViewModel cho form tạo meeting, gán CreatorId là user hiện tại (lấy từ claim "UserId")
            var vm = new CreateMeetingViewModel
            {
                // Lấy UserId từ claim của user đang đăng nhập, nếu không có thì dùng 0
                CreatorId = int.Parse(User.FindFirst("UserId")?.Value ?? "0")
            };
            // Trả về view Create (Views/Meeting/Create.cshtml), đồng thời truyền ViewModel này xuống view
            return View(vm);
        }

        // Đánh dấu action này sẽ xử lý HTTP POST request khi submit form tạo cuộc họp mới
        [HttpPost]
        public async Task<IActionResult> Create(CreateMeetingViewModel vm)
        {
            // Kiểm tra dữ liệu form gửi lên có hợp lệ không (dựa vào các validation attribute trong ViewModel)
            // Nếu không hợp lệ, trả lại view cùng dữ liệu cũ để người dùng sửa lỗi
            if (!ModelState.IsValid) return View(vm);

            // Gọi service để tạo cuộc họp mới, truyền vào CreatorId lấy từ ViewModel (dữ liệu form)
            // Hàm này trả về mã code của phòng họp vừa tạo
            var code = await _meetingService.CreateMeetingAsync(vm.CreatorId);

            // đồng thời truyền mã code của phòng họp vừa tạo để hiển thị giao diện phòng họp
            // Chuyển hướng người dùng đến trang Room (phòng họp) với mã code của cuộc họp vừa tạo
            return RedirectToAction(nameof(Room), new { code });
        }

        [HttpGet]
        public IActionResult Join()
            => View(new JoinMeetingViewModel());

        [HttpPost]
        public async Task<IActionResult> Join(JoinMeetingViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var result = await _meetingService.JoinMeetingAsync(vm.Password);
            if (!result.Success)
            {
                ModelState.AddModelError("", result.Error);
                return View(vm);
            }

            return RedirectToAction(nameof(Room), new { code = vm.Password });
        }

        [HttpGet]
        // Action này xử lý HTTP GET khi người dùng truy cập vào phòng họp (Meeting Room)
        // Đường dẫn sẽ có dạng: /Meeting/Room?code=xxxxxx
        public async Task<IActionResult> Room(string code)
        {
            // Lấy userId của người dùng hiện tại từ claim "UserId" (được gán khi đăng nhập)
            // Nếu không có claim thì gán mặc định là 0 (không hợp lệ)
            int userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");

            // Kiểm tra xem user hiện tại có phải là host (người tạo/phụ trách) của phòng họp này không
            // Kết quả (true/false) được lưu vào ViewBag.IsHost để view có thể hiển thị các chức năng đặc biệt cho host
            ViewBag.IsHost = await _meetingService.IsHostAsync(code, userId);

            // Truyền mã phòng họp (code) cho view để sử dụng (ví dụ: hiển thị, truyền cho JS)
            ViewBag.MeetingCode = code;

            // Truyền userId của người dùng hiện tại cho view (có thể dùng cho các thao tác realtime, phân quyền, ...)
            ViewBag.UserId = userId;

            // Truyền lại mã phòng họp với tên biến "code" (giữ lại cho tương thích với code cũ hoặc JS phía client)
            ViewBag.code = code;

            // Truyền tên phòng họp cho view (ví dụ: "Meeting ABC123"), có thể dùng để hiển thị tiêu đề phòng
            ViewBag.meetingName = $"Meeting {code}";

            // Trả về view mặc định (Views/Meeting/Room.cshtml), các biến ViewBag ở trên sẽ được sử dụng trong view này
            return View();
        }

        // ======== IN-ROOM STATISTICS ========



        [HttpGet]
        public async Task<IActionResult> GetRoomStatsData(string code)
        {
            try
            {
                var statsData = await _meetingService.GetRoomStatsDataAsync(code);
                return Json(statsData);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetActiveSession(string code)
        {
            try
            {
                var session = await _meetingService.GetActiveSessionAsync(code);
                if (session != null)
                {
                    return Json(new { sessionId = session.SessionId });
                }
                else
                {
                    return Json(new { sessionId = (string?)null });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetRoomId(string code)
        {
            try
            {
                var room = await _meetingService.GetRoomByCodeAsync(code);
                if (room != null)
                {
                    return Json(new { roomId = room.RoomId });
                }
                else
                {
                    return Json(new { roomId = (int?)null });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // ======== NEW API ENDPOINTS FOR STATISTICS ========

        [HttpGet]
        public async Task<IActionResult> GetCallHistory(string password)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                    return Unauthorized();

                // Check if user has access to this room
                var isHost = await _meetingService.IsHostAsync(password, userId.Value);
                if (!isHost)
                    return Forbid("Only room host can view call history");

                var history = await _meetingService.GetCallHistoryAsync(password);
                return Json(history);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCallStatistics(string password)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                    return Unauthorized();

                // Check if user has access to this room
                var isHost = await _meetingService.IsHostAsync(password, userId.Value);
                if (!isHost)
                    return Forbid("Only room host can view statistics");

                var stats = await _meetingService.GetCallStatisticsAsync(password);
                return Json(stats);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAttendanceReport(Guid sessionId)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                    return Unauthorized();

                // For now, allow any authenticated user to view attendance
                // You can add more specific authorization logic here
                var attendance = await _meetingService.GetAttendanceReportAsync(sessionId);
                return Json(attendance);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveRecording(Guid sessionId, string recordingUrl)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                    return Unauthorized();

                // Use RecordingService to save main session recording URL
                var success = await _recordingService.SaveSessionRecordingUrlAsync(sessionId, recordingUrl);

                if (success)
                {
                    return Ok(new { message = "Recording URL saved successfully" });
                }
                else
                {
                    return BadRequest(new { error = "Failed to save recording URL" });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost]
        [RequestSizeLimit(50 * 1024 * 1024)] // 50MB limit
        public async Task<IActionResult> UploadRecording(IFormFile file, string type, string meetingCode, int? duration = null, string? metadata = null, string? thumbnailUrl = null, Guid? sessionId = null)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                    return Unauthorized();

                Console.WriteLine($"[Recording Upload] Starting upload - File: {file?.FileName}, Size: {file?.Length}, Type: {file?.ContentType}, MeetingCode: {meetingCode}, SessionId: {sessionId}");

                // Use RecordingService to handle upload with all business logic
                var result = await _recordingService.UploadRecordingAsync(file, type, meetingCode, userId.Value, sessionId, duration, metadata, thumbnailUrl);

                if (result.Success)
                {
                    return Ok(new
                    {
                        success = true,
                        url = result.Url,
                        filename = result.FileName,
                        size = result.FileSize,
                        type = result.Type,
                        meetingCode = result.MeetingCode,
                        sessionId = result.SessionId,
                        uploadedAt = result.UploadedAt
                    });
                }
                else
                {
                    return BadRequest(new { error = result.Error });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Recording Upload ERROR] File: {file?.FileName}, Error: {ex.Message}");
                return BadRequest(new { error = "An unexpected error occurred while uploading the recording" });
            }
        }

        // ======== NEW RECORDING MANAGEMENT ENDPOINTS ========

        [HttpGet]
        public async Task<IActionResult> GetRecordingHistory(int? userId = null, string? meetingCode = null)
        {
            try
            {
                // Try both claim and session approaches
                var currentUserId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
                if (currentUserId == 0)
                {
                    currentUserId = HttpContext.Session.GetInt32("UserId") ?? 0;
                }

                if (currentUserId == 0)
                    return Unauthorized();

                // Use current user's ID if not specified, or validate access
                var targetUserId = userId ?? currentUserId;
                if (targetUserId != currentUserId)
                {
                    // Only allow admins to view other users' recordings
                    return Forbid("Access denied");
                }

                // Use GetRecordingHistoryAsync instead of GetUserRecordingsAsync
                // This returns the proper RecordingHistoryItem format that JavaScript expects
                var recordings = await _recordingService.GetRecordingHistoryAsync(targetUserId, meetingCode);

                // Map to format expected by JavaScript
                var mappedRecordings = recordings.Select(r => new
                {
                    id = r.Id,
                    fileName = r.FileName,
                    originalFileName = r.OriginalFileName,
                    fileUrl = r.Url,
                    fileSize = r.FileSize,
                    recordingType = r.Type,
                    meetingCode = r.MeetingCode,
                    sessionId = r.SessionId,
                    createdAt = r.CreatedAt,
                    userId = r.UserId,
                    duration = r.Duration,
                    metadata = r.Metadata,
                    thumbnailUrl = r.ThumbnailUrl,
                    tags = r.Tags,
                    description = r.Description,
                    downloadCount = r.DownloadCount,
                    lastAccessedAt = r.LastAccessedAt
                }).ToList();

                return Json(new { success = true, recordings = mappedRecordings });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting recording history for user {userId}: {ex.Message}");
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteRecording(Guid recordingId)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                    return Unauthorized();

                var result = await _recordingService.DeleteRecordingAsync(recordingId, userId.Value);

                if (result)
                {
                    return Json(new { success = true });
                }
                else
                {
                    return Json(new { success = false, error = "Recording not found or access denied" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateRecording([FromBody] UpdateRecordingRequest request)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                    return Unauthorized();

                var result = await _recordingService.UpdateRecordingAsync(request.RecordingId, request.Description, request.Tags, userId.Value);

                if (result)
                {
                    return Json(new { success = true });
                }
                else
                {
                    return Json(new { success = false, error = "Recording not found or access denied" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateLastAccessed([FromBody] UpdateLastAccessedRequest request)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                    return Unauthorized();

                await _recordingService.UpdateLastAccessedAsync(request.RecordingId, userId.Value);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> TrackDownload([FromBody] TrackDownloadRequest request)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                    return Unauthorized();

                await _recordingService.TrackDownloadAsync(request.RecordingId, userId.Value);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> RollbackDownload([FromBody] TrackDownloadRequest request)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                    return Unauthorized();

                await _recordingService.RollbackDownloadAsync(request.RecordingId, userId.Value);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> DownloadRecording(Guid recordingId)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                    return Unauthorized();

                var recording = await _recordingService.GetRecordingByIdAsync(recordingId, userId.Value);

                if (recording == null)
                {
                    return NotFound("Recording not found or access denied");
                }

                // Track download
                await _recordingService.TrackDownloadAsync(recordingId, userId.Value);

                // If it's a direct file URL, redirect with proper headers
                if (!string.IsNullOrEmpty(recording.FileUrl))
                {
                    // For cloud storage URLs, we need to proxy the download
                    using var httpClient = new HttpClient();

                    try
                    {
                        var response = await httpClient.GetAsync(recording.FileUrl);

                        if (response.IsSuccessStatusCode)
                        {
                            var fileStream = await response.Content.ReadAsStreamAsync();
                            var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";

                            // Force download with proper headers
                            Response.Headers.Add("Content-Disposition", $"attachment; filename=\"{recording.FileName}\"");

                            return File(fileStream, contentType, recording.FileName);
                        }
                        else
                        {
                            // Fallback to redirect
                            return Redirect(recording.FileUrl);
                        }
                    }
                    catch
                    {
                        // If proxy fails, fallback to redirect
                        return Redirect(recording.FileUrl);
                    }
                }

                return NotFound("File URL not found");
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ViewRecording(Guid id)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                    return RedirectToAction("Login", "Account");

                var recording = await _recordingService.GetRecordingByIdAsync(id, userId.Value);

                if (recording == null)
                {
                    return NotFound("Recording not found or access denied");
                }

                // Track view access
                await _recordingService.UpdateLastAccessedAsync(id, userId.Value);

                ViewBag.Recording = recording;
                return View();
            }
            catch (Exception ex)
            {
                return NotFound("Error loading recording");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Recordings()
        {
            // Try both claim and session approaches to get userId
            var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
            if (userId == 0)
            {
                userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            }

            if (userId == 0)
                return RedirectToAction("Login", "Account");

            // Pass userId to view for JavaScript initialization
            ViewBag.UserId = userId;
            return View();
        }

        [HttpPost]
        [RequestSizeLimit(50 * 1024 * 1024)] // 50MB limit
        public async Task<IActionResult> ValidateRecordingFile(IFormFile file, string type)
        {
            try
            {
                var validationResult = _recordingService.ValidateRecordingFile(file, type);

                return Ok(new
                {
                    isValid = validationResult.IsValid,
                    errorMessage = validationResult.ErrorMessage,
                    maxFileSize = validationResult.MaxFileSize,
                    allowedTypes = validationResult.AllowedTypes
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetSessionRecordings(Guid sessionId)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                    return Unauthorized();

                var recordings = await _recordingService.GetSessionRecordingsAsync(sessionId, userId.Value);
                return Json(recordings);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // ======== ROOM CHAT ENDPOINTS ========

        [HttpGet]
        public async Task<IActionResult> GetRoomMessages(int roomId, Guid sessionId)
        {
            try
            {
                var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
                if (userId == 0)
                {
                    userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                }

                if (userId == 0)
                    return Unauthorized();

                var messages = await _meetingRoomMessageService.GetRoomMessagesAsync(roomId, sessionId, userId);
                return Json(messages);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] MeetingSendMessageViewModel model)
        {
            try
            {
                var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
                if (userId == 0)
                {
                    userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                }

                if (userId == 0)
                    return Unauthorized();

                var message = await _meetingRoomMessageService.SendMessageAsync(model, userId);
                return Json(new { success = true, message = message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, error = ex.Message });
            }
        }

        [HttpPut]
        public async Task<IActionResult> EditMessage([FromBody] MeetingEditMessageViewModel model)
        {
            try
            {
                var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
                if (userId == 0)
                {
                    userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                }

                if (userId == 0)
                    return Unauthorized();

                var message = await _meetingRoomMessageService.EditMessageAsync(model, userId);
                if (message == null)
                    return BadRequest(new { success = false, error = "Không thể chỉnh sửa tin nhắn này" });

                return Json(new { success = true, message = message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, error = ex.Message });
            }
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteMessage(long messageId)
        {
            try
            {
                var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
                if (userId == 0)
                {
                    userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                }

                if (userId == 0)
                    return Unauthorized();

                var success = await _meetingRoomMessageService.DeleteMessageAsync(messageId, userId);
                if (!success)
                    return BadRequest(new { success = false, error = "Không thể xóa tin nhắn này" });

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetRoomParticipants(int roomId)
        {
            try
            {
                var participants = await _meetingRoomMessageService.GetRoomParticipantsAsync(roomId);
                return Json(participants);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ChatPanel(int roomId, Guid sessionId)
        {
            try
            {
                var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
                if (userId == 0)
                {
                    userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                }

                if (userId == 0)
                    return Unauthorized();

                var messages = await _meetingRoomMessageService.GetRoomMessagesAsync(roomId, sessionId, userId);
                var participants = await _meetingRoomMessageService.GetRoomParticipantsAsync(roomId);

                var chatPanel = new MeetingChatPanelViewModel
                {
                    RoomId = roomId,
                    SessionId = sessionId,
                    Messages = messages,
                    Participants = participants,
                    CurrentUserId = userId,
                    AllowChat = true // Có thể lấy từ cài đặt phòng
                };

                return PartialView("_ChatPanel", chatPanel);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // ======== REAL-TIME SUBTITLE ENDPOINTS ========

        [HttpPost]
        public async Task<IActionResult> TranscribeAudio()
        {
            try
            {
                // Debug raw request body BEFORE model binding
                Request.EnableBuffering();
                Request.Body.Position = 0;
                using var reader = new StreamReader(Request.Body, leaveOpen: true);
                var rawBody = await reader.ReadToEndAsync();
                Request.Body.Position = 0;
                
                Console.WriteLine($"🎵 [DEBUG] Raw request body (first 200 chars): {rawBody.Substring(0, Math.Min(200, rawBody.Length))}");
                Console.WriteLine($"🎵 [DEBUG] Request.ContentType: {Request.ContentType}");
                Console.WriteLine($"🎵 [DEBUG] Request.ContentLength: {Request.ContentLength}");
                
                // Debug the raw JSON structure first
                Console.WriteLine($"🎵 [DEBUG] Full raw JSON length: {rawBody.Length}");
                
                // Try to parse as JsonDocument to see the structure
                try
                {
                    using var jsonDoc = System.Text.Json.JsonDocument.Parse(rawBody);
                    var root = jsonDoc.RootElement;
                    
                    Console.WriteLine($"🎵 [DEBUG] JSON root element type: {root.ValueKind}");
                    Console.WriteLine($"🎵 [DEBUG] Available properties: {string.Join(", ", root.EnumerateObject().Select(p => p.Name))}");
                    
                    if (root.TryGetProperty("sessionId", out var sessionIdElement))
                    {
                        Console.WriteLine($"🎵 [DEBUG] sessionId value: '{sessionIdElement.GetString()}'");
                        Console.WriteLine($"🎵 [DEBUG] sessionId value type: {sessionIdElement.ValueKind}");
                    }
                    else
                    {
                        Console.WriteLine($"❌ [DEBUG] sessionId property not found in JSON");
                    }
                    
                    if (root.TryGetProperty("speakerId", out var speakerIdElement))
                    {
                        Console.WriteLine($"🎵 [DEBUG] speakerId value: '{speakerIdElement}'");
                        Console.WriteLine($"🎵 [DEBUG] speakerId value type: {speakerIdElement.ValueKind}");
                        
                        if (speakerIdElement.ValueKind == JsonValueKind.Number)
                        {
                            Console.WriteLine($"🎵 [DEBUG] speakerId as number: {speakerIdElement.GetInt32()}");
                        }
                        else if (speakerIdElement.ValueKind == JsonValueKind.String)
                        {
                            Console.WriteLine($"🎵 [DEBUG] speakerId as string: {speakerIdElement.GetString()}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"❌ [DEBUG] speakerId property not found in JSON");
                    }
                    
                    if (root.TryGetProperty("audioData", out var audioDataElement))
                    {
                        Console.WriteLine($"🎵 [DEBUG] audioData length: {audioDataElement.GetString()?.Length ?? 0}");
                    }
                    else
                    {
                        Console.WriteLine($"❌ [DEBUG] audioData property not found in JSON");
                    }
                }
                catch (System.Text.Json.JsonException ex)
                {
                    Console.WriteLine($"❌ [DEBUG] JSON parsing error: {ex.Message}");
                }
                
                // Now try to deserialize manually
                try
                {
                    var request = System.Text.Json.JsonSerializer.Deserialize<AudioTranscriptionRequest>(rawBody);
                    Console.WriteLine($"🎵 [DEBUG] Manual deserialization - AudioData length: {request?.AudioData?.Length ?? 0}");
                    Console.WriteLine($"🎵 [DEBUG] Manual deserialization - SessionId: {request?.SessionId}");
                    Console.WriteLine($"🎵 [DEBUG] Manual deserialization - SpeakerId: {request?.SpeakerId}");
                    Console.WriteLine($"🎵 [DEBUG] Manual deserialization - Language: {request?.Language}");
                    
                    if (request == null)
                    {
                        return BadRequest(new { success = false, error = "Failed to deserialize request" });
                    }
                    
                    // Continue with the rest of the logic using the manually deserialized request
                    return await ProcessTranscriptionRequest(request);
                }
                catch (System.Text.Json.JsonException ex)
                {
                    Console.WriteLine($"❌ [DEBUG] JSON deserialization error: {ex.Message}");
                    return BadRequest(new { success = false, error = "Invalid JSON format" });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error reading request body: {ex.Message}");
                return BadRequest(new { 
                    success = false, 
                    error = ex.Message 
                });
            }
        }

        private async Task<IActionResult> ProcessTranscriptionRequest(AudioTranscriptionRequest request)
        {
            try
            {
                // Validate request
                if (string.IsNullOrEmpty(request?.AudioData))
                {
                    Console.WriteLine("❌ [DEBUG] AudioData is null or empty");
                    return BadRequest(new { success = false, error = "AudioData is required" });
                }
                
                if (request.SessionId == Guid.Empty)
                {
                    Console.WriteLine("❌ [DEBUG] SessionId is empty");
                    return BadRequest(new { success = false, error = "Valid SessionId is required" });
                }
                
                if (request.SpeakerId <= 0)
                {
                    Console.WriteLine("❌ [DEBUG] SpeakerId is invalid");
                    return BadRequest(new { success = false, error = "Valid SpeakerId is required" });
                }

                var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
                if (userId == 0)
                {
                    userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                }

                if (userId == 0)
                    return Unauthorized();

                Console.WriteLine($"🎵 Received audio chunk from user {userId} for session {request.SessionId}");
                Console.WriteLine($"🔤 Audio data length: {request.AudioData.Length} characters");
                Console.WriteLine($"📦 Audio data preview: {request.AudioData.Substring(0, Math.Min(50, request.AudioData.Length))}...");

                // Convert base64 to stream
                var audioBytes = Convert.FromBase64String(request.AudioData);
                using var audioStream = new MemoryStream(audioBytes);

                // Process audio with AI
                var subtitle = await _audioTranscriptionService.ProcessRealTimeAudioAsync(
                    audioStream, 
                    request.SessionId, 
                    request.SpeakerId, 
                    request.Language ?? "vi"
                );

                if (subtitle == null)
                {
                    return Json(new { success = false, error = "No text transcribed from audio" });
                }

                Console.WriteLine($"📝 Transcribed text: {subtitle.OriginalText}");

                // Broadcast via SignalR (sẽ implement sau)
                // await _hub.Clients.Group(request.SessionId.ToString()).SendAsync("ReceiveSubtitle", subtitle);

                return Json(new { 
                    success = true, 
                    subtitle = subtitle,
                    message = "Audio processed successfully" 
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error processing audio: {ex.Message}");
                return BadRequest(new { 
                    success = false, 
                    error = ex.Message 
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> TranslateText([FromBody] TranslationRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request?.Text))
                {
                    return BadRequest(new { success = false, error = "Text is required" });
                }

                if (string.IsNullOrEmpty(request?.TargetLanguage))
                {
                    return BadRequest(new { success = false, error = "Target language is required" });
                }

                Console.WriteLine($"🌍 Translating: '{request.Text}' to {request.TargetLanguage}");

                // Sử dụng ChatGPT để translate
                var translatedText = await _chatGPTService.TranslateTextAsync(
                    request.Text, 
                    request.SourceLanguage ?? "vi", 
                    request.TargetLanguage
                );

                if (string.IsNullOrEmpty(translatedText))
                {
                    return Json(new { success = false, error = "Translation failed" });
                }

                Console.WriteLine($"🌍 Translated: '{request.Text}' → '{translatedText}'");

                return Json(new { 
                    success = true, 
                    translatedText = translatedText,
                    sourceLanguage = request.SourceLanguage ?? "vi",
                    targetLanguage = request.TargetLanguage
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error translating text: {ex.Message}");
                return BadRequest(new { 
                    success = false, 
                    error = ex.Message 
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveLanguagePreference([FromBody] LanguagePreferenceRequest request)
        {
            try
            {
                if (request?.SessionId == Guid.Empty)
                {
                    return BadRequest(new { success = false, error = "Valid SessionId is required" });
                }

                if (string.IsNullOrEmpty(request?.Language))
                {
                    return BadRequest(new { success = false, error = "Language is required" });
                }

                Console.WriteLine($"🌍 Saving language preference: {request.Language} for session {request.SessionId}");

                // Lưu preference vào database hoặc cache
                // TODO: Implement database storage

                return Json(new { 
                    success = true, 
                    message = "Language preference saved" 
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error saving language preference: {ex.Message}");
                return BadRequest(new { 
                    success = false, 
                    error = ex.Message 
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ToggleSubtitles([FromBody] ToggleSubtitlesRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
                if (userId == 0)
                {
                    userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                }

                if (userId == 0)
                    return Unauthorized();

                var enabled = await _audioTranscriptionService.EnableSubtitlesForUserAsync(
                    request.SessionId, 
                    userId, 
                    request.Enabled
                );

                // Notify other participants via SignalR (sẽ implement sau)
                // await _hub.Clients.Group(request.SessionId.ToString()).SendAsync("SubtitleToggled", userId, request.Enabled);

                return Json(new { success = true, enabled = enabled });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetSessionSubtitles(Guid sessionId)
        {
            try
            {
                var subtitles = await _audioTranscriptionService.GetSessionSubtitlesAsync(sessionId);
                return Json(subtitles);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetUserSubtitlePreference(Guid sessionId)
        {
            try
            {
                var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
                if (userId == 0)
                {
                    userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                }

                if (userId == 0)
                    return Unauthorized();

                var enabled = await _audioTranscriptionService.GetUserSubtitlePreferenceAsync(sessionId, userId);
                return Json(new { enabled = enabled });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> TestOpenAI()
        {
            try
            {
                // Test OpenAI API với một đoạn audio nhỏ
                var testAudio = new byte[] { 0x52, 0x49, 0x46, 0x46 }; // WAV header
                
                var result = await _audioTranscriptionService.TranscribeAudioChunkAsync(testAudio, "vi-VN");
                
                return Json(new { 
                    success = true, 
                    message = "OpenAI API test completed",
                    result = result 
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { 
                    success = false, 
                    error = ex.Message,
                    stackTrace = ex.StackTrace 
                });
            }
        }
    }

    // Request models cho subtitle system
    public class AudioTranscriptionRequest
    {
        [JsonPropertyName("audioData")]
        public string AudioData { get; set; }
        
        [JsonPropertyName("sessionId")]
        public Guid SessionId { get; set; }
        
        [JsonPropertyName("language")]
        public string Language { get; set; } = "vi";
        
        [JsonPropertyName("speakerId")]
        public int SpeakerId { get; set; }
    }

    public class ToggleSubtitlesRequest
    {
        public Guid SessionId { get; set; }
        public bool Enabled { get; set; }
    }
}