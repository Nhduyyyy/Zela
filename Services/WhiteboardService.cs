using Microsoft.EntityFrameworkCore;
using Zela.DbContext;
using Zela.Models;
using Zela.Services.Interface;
using Zela.ViewModels;
using System.Text.Json;

namespace Zela.Services;

public class WhiteboardService : IWhiteboardService
{
    private readonly ApplicationDbContext _context;

    public WhiteboardService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<WhiteboardIndexViewModel> GetWhiteboardIndexAsync(int userId, string searchTerm = "", string filterType = "all")
    {
        var query = _context.Whiteboards
            .Include(w => w.Creator)
            .Include(w => w.Sessions)
            .AsQueryable();

        // Filter theo loại
        switch (filterType.ToLower())
        {
            case "my":
                query = query.Where(w => w.CreatorId == userId);
                break;
            case "public":
                query = query.Where(w => w.IsPublic);
                break;
            case "templates":
                query = query.Where(w => w.IsTemplate);
                break;
        }

        // Search
        if (!string.IsNullOrEmpty(searchTerm))
        {
            query = query.Where(w => w.Title.Contains(searchTerm) || w.Description.Contains(searchTerm));
        }

        var whiteboards = await query
            .OrderByDescending(w => w.UpdatedAt ?? w.CreatedAt)
            .ToListAsync();

        var myWhiteboards = whiteboards
            .Where(w => w.CreatorId == userId)
            .Select(w => MapToCardViewModel(w, userId))
            .ToList();

        var publicTemplates = whiteboards
            .Where(w => w.IsPublic && w.IsTemplate)
            .Select(w => MapToCardViewModel(w, userId))
            .ToList();

        var recentSessions = await GetRecentSessionsAsync(userId, 10);

        return new WhiteboardIndexViewModel
        {
            MyWhiteboards = myWhiteboards,
            PublicTemplates = publicTemplates,
            RecentSessions = recentSessions,
            SearchTerm = searchTerm,
            FilterType = filterType
        };
    }

    public async Task<WhiteboardCardViewModel> GetWhiteboardByIdAsync(int whiteboardId, int userId)
    {
        var whiteboard = await _context.Whiteboards
            .Include(w => w.Creator)
            .Include(w => w.Sessions)
            .FirstOrDefaultAsync(w => w.WhiteboardId == whiteboardId);

        if (whiteboard == null) return null;

        return MapToCardViewModel(whiteboard, userId);
    }

    public async Task<int> CreateWhiteboardAsync(CreateWhiteboardViewModel model, int userId)
    {
        var whiteboard = new Whiteboard
        {
            Title = model.Title,
            Description = model.Description ?? "",
            CreatorId = userId,
            CreatedAt = DateTime.UtcNow,
            IsPublic = model.IsPublic,
            IsTemplate = model.IsTemplate
        };

        _context.Whiteboards.Add(whiteboard);
        await _context.SaveChangesAsync();

        // Tạo session đầu tiên
        await CreateSessionAsync(whiteboard.WhiteboardId, model.RoomId);

        return whiteboard.WhiteboardId;
    }

    public async Task<bool> UpdateWhiteboardAsync(EditWhiteboardViewModel model, int userId)
    {
        var whiteboard = await _context.Whiteboards
            .FirstOrDefaultAsync(w => w.WhiteboardId == model.WhiteboardId && w.CreatorId == userId);

        if (whiteboard == null) return false;

        whiteboard.Title = model.Title;
        whiteboard.Description = model.Description ?? "";
        whiteboard.IsPublic = model.IsPublic;
        whiteboard.IsTemplate = model.IsTemplate;
        whiteboard.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteWhiteboardAsync(int whiteboardId, int userId)
    {
        var whiteboard = await _context.Whiteboards
            .FirstOrDefaultAsync(w => w.WhiteboardId == whiteboardId && w.CreatorId == userId);

        if (whiteboard == null) return false;

        _context.Whiteboards.Remove(whiteboard);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> TogglePublicAsync(int whiteboardId, int userId)
    {
        var whiteboard = await _context.Whiteboards
            .FirstOrDefaultAsync(w => w.WhiteboardId == whiteboardId && w.CreatorId == userId);

        if (whiteboard == null) return false;

        whiteboard.IsPublic = !whiteboard.IsPublic;
        whiteboard.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ToggleTemplateAsync(int whiteboardId, int userId)
    {
        var whiteboard = await _context.Whiteboards
            .FirstOrDefaultAsync(w => w.WhiteboardId == whiteboardId && w.CreatorId == userId);

        if (whiteboard == null) return false;

        whiteboard.IsTemplate = !whiteboard.IsTemplate;
        whiteboard.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<WhiteboardSessionViewModel> GetSessionByIdAsync(int sessionId)
    {
        var session = await _context.WhiteboardSessions
            .Include(s => s.Whiteboard)
            .FirstOrDefaultAsync(s => s.SessionId == sessionId);

        if (session == null) return null;

        return new WhiteboardSessionViewModel
        {
            SessionId = session.SessionId,
            WhiteboardId = session.WhiteboardId,
            CanvasData = session.CanvasData,
            CreatedAt = session.CreatedAt,
            LastModifiedAt = session.LastModifiedAt,
            ThumbnailUrl = session.ThumbnailUrl
        };
    }

    public async Task<int> CreateSessionAsync(int whiteboardId, int? roomId = null)
    {
        var session = new WhiteboardSession
        {
            WhiteboardId = whiteboardId,
            RoomId = roomId,
            CreatedAt = DateTime.UtcNow,
            CanvasData = "[]", // Empty canvas
            ThumbnailUrl = string.Empty, // Set default empty string
            IsActive = true
        };

        _context.WhiteboardSessions.Add(session);
        await _context.SaveChangesAsync();

        return session.SessionId;
    }

    public async Task<bool> UpdateSessionDataAsync(int sessionId, string canvasData)
    {
        var session = await _context.WhiteboardSessions
            .FirstOrDefaultAsync(s => s.SessionId == sessionId);

        if (session == null) return false;

        session.CanvasData = canvasData;
        session.LastModifiedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> SaveSessionThumbnailAsync(int sessionId, string thumbnailData)
    {
        var session = await _context.WhiteboardSessions
            .FirstOrDefaultAsync(s => s.SessionId == sessionId);

        if (session == null) return false;

        // TODO: Upload thumbnail to Cloudinary and get URL
        session.ThumbnailUrl = thumbnailData; // Temporary
        session.LastModifiedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<WhiteboardSessionViewModel>> GetSessionsByWhiteboardAsync(int whiteboardId)
    {
        var sessions = await _context.WhiteboardSessions
            .Where(s => s.WhiteboardId == whiteboardId)
            .OrderByDescending(s => s.LastModifiedAt ?? s.CreatedAt)
            .ToListAsync();

        return sessions.Select(s => new WhiteboardSessionViewModel
        {
            SessionId = s.SessionId,
            WhiteboardId = s.WhiteboardId,
            CanvasData = s.CanvasData,
            CreatedAt = s.CreatedAt,
            LastModifiedAt = s.LastModifiedAt,
            ThumbnailUrl = s.ThumbnailUrl
        }).ToList();
    }

    public async Task<bool> DeleteSessionAsync(int sessionId, int userId)
    {
        var session = await _context.WhiteboardSessions
            .Include(s => s.Whiteboard)
            .FirstOrDefaultAsync(s => s.SessionId == sessionId && s.Whiteboard.CreatorId == userId);

        if (session == null) return false;

        _context.WhiteboardSessions.Remove(session);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<WhiteboardCardViewModel>> GetPublicTemplatesAsync(int userId = 0)
    {
        var templates = await _context.Whiteboards
            .Include(w => w.Creator)
            .Include(w => w.Sessions)
            .Where(w => w.IsPublic && w.IsTemplate)
            .OrderByDescending(w => w.CreatedAt)
            .ToListAsync();

        return templates.Select(w => MapToCardViewModel(w, userId)).ToList();
    }

    public async Task<List<WhiteboardCardViewModel>> GetRecentSessionsAsync(int userId, int limit = 10)
    {
        var recentSessions = await _context.WhiteboardSessions
            .Include(s => s.Whiteboard)
            .ThenInclude(w => w.Creator)
            .Where(s => s.Whiteboard.CreatorId == userId)
            .OrderByDescending(s => s.LastModifiedAt ?? s.CreatedAt)
            .Take(limit)
            .ToListAsync();

        return recentSessions.Select(s => new WhiteboardCardViewModel
        {
            WhiteboardId = s.Whiteboard.WhiteboardId,
            Title = s.Whiteboard.Title,
            Description = s.Whiteboard.Description,
            CreatorName = s.Whiteboard.Creator.FullName,
            CreatorAvatar = s.Whiteboard.Creator.AvatarUrl,
            CreatedAt = s.CreatedAt,
            LastModifiedAt = s.LastModifiedAt,
            IsPublic = s.Whiteboard.IsPublic,
            IsTemplate = s.Whiteboard.IsTemplate,
            ThumbnailUrl = s.ThumbnailUrl,
            SessionCount = 1,
            IsOwner = true
        }).ToList();
    }

    public async Task<bool> CloneWhiteboardAsync(int sourceWhiteboardId, int userId, string newTitle)
    {
        var sourceWhiteboard = await _context.Whiteboards
            .Include(w => w.Sessions)
            .FirstOrDefaultAsync(w => w.WhiteboardId == sourceWhiteboardId && w.IsPublic);

        if (sourceWhiteboard == null) return false;

        var newWhiteboard = new Whiteboard
        {
            Title = newTitle,
            Description = sourceWhiteboard.Description,
            CreatorId = userId,
            CreatedAt = DateTime.UtcNow,
            IsPublic = false,
            IsTemplate = false
        };

        _context.Whiteboards.Add(newWhiteboard);
        await _context.SaveChangesAsync();

        // Clone session cuối cùng
        var latestSession = sourceWhiteboard.Sessions
            .OrderByDescending(s => s.LastModifiedAt ?? s.CreatedAt)
            .FirstOrDefault();

        if (latestSession != null)
        {
            var newSession = new WhiteboardSession
            {
                WhiteboardId = newWhiteboard.WhiteboardId,
                CreatedAt = DateTime.UtcNow,
                CanvasData = latestSession.CanvasData,
                ThumbnailUrl = latestSession.ThumbnailUrl,
                IsActive = true
            };

            _context.WhiteboardSessions.Add(newSession);
            await _context.SaveChangesAsync();
        }

        return true;
    }

    public async Task<bool> CanUserAccessWhiteboardAsync(int whiteboardId, int userId)
    {
        var whiteboard = await _context.Whiteboards
            .FirstOrDefaultAsync(w => w.WhiteboardId == whiteboardId);

        if (whiteboard == null) return false;

        return whiteboard.CreatorId == userId || whiteboard.IsPublic;
    }

    public async Task<bool> CanUserEditWhiteboardAsync(int whiteboardId, int userId)
    {
        var whiteboard = await _context.Whiteboards
            .FirstOrDefaultAsync(w => w.WhiteboardId == whiteboardId);

        if (whiteboard == null) return false;

        return whiteboard.CreatorId == userId;
    }

    private WhiteboardCardViewModel MapToCardViewModel(Whiteboard whiteboard, int userId = 0)
    {
        return new WhiteboardCardViewModel
        {
            WhiteboardId = whiteboard.WhiteboardId,
            Title = whiteboard.Title,
            Description = whiteboard.Description,
            CreatorName = whiteboard.Creator?.FullName ?? "Unknown",
            CreatorAvatar = whiteboard.Creator?.AvatarUrl,
            CreatedAt = whiteboard.CreatedAt,
            LastModifiedAt = whiteboard.UpdatedAt,
            IsPublic = whiteboard.IsPublic,
            IsTemplate = whiteboard.IsTemplate,
            ThumbnailUrl = whiteboard.Sessions?.OrderByDescending(s => s.LastModifiedAt ?? s.CreatedAt).FirstOrDefault()?.ThumbnailUrl,
            SessionCount = whiteboard.Sessions?.Count ?? 0,
            IsOwner = userId > 0 && whiteboard.CreatorId == userId
        };
    }
} 