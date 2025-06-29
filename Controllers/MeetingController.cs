using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Zela.Models.ViewModels;
using Zela.Services;
using Zela.Services.Interface;
using Zela.ViewModels;
using Microsoft.Extensions.Logging;

namespace Zela.Controllers
{
    public class MeetingController : Controller
    {
        private readonly IMeetingService _meetingService;
        private readonly IRecordingService _recordingService;

        public MeetingController(IMeetingService meetingService, IRecordingService recordingService)
        {
            _meetingService = meetingService;
            _recordingService = recordingService;
        }

        [HttpGet]
        public IActionResult Index() => View();

        [HttpGet]
        public IActionResult Create()
        {
            var vm = new CreateMeetingViewModel {
                CreatorId = int.Parse(User.FindFirst("UserId")?.Value ?? "0")
            };
            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateMeetingViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var code = await _meetingService.CreateMeetingAsync(vm.CreatorId);
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
        public async Task<IActionResult> Room(string code)
        {
            // Lấy userId từ claim "UserId" (vì lúc tạo bạn đã gán claim này)
            int userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");

            // 2. Xác định host
            ViewBag.IsHost = await _meetingService.IsHostAsync(code, userId);

            // 3. Luôn truyền meeting code
            ViewBag.MeetingCode = code;
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
                var currentUserId = HttpContext.Session.GetInt32("UserId");
                if (currentUserId == null)
                    return Unauthorized();

                // Use current user's ID if not specified, or validate access
                var targetUserId = userId ?? currentUserId.Value;
                if (targetUserId != currentUserId.Value)
                {
                    // Only allow admins to view other users' recordings
                    return Forbid("Access denied");
                }

                var recordings = await _recordingService.GetUserRecordingsAsync(targetUserId);
                return Json(new { success = true, recordings = recordings });
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
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Account");

            return View();
        }

        [HttpPost]
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


    }
}