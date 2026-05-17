using Microsoft.EntityFrameworkCore;
using Skillforge.Data;
using Skillforge.Domain;
using Skillforge.Dto;

namespace Skillforge.Repository;

public class NotificationRepository : INotificationRepository
{
    private readonly SkillForgeDB _context;

    public NotificationRepository(SkillForgeDB context)
    {
        _context = context;
    }

    public async Task<int> AddAsync(Notification notification)
    {
        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();
        return notification.NotificationID;
    }

    public async Task<(NotificationResponseDto? Notification, bool AlreadyRead)> MarkAsReadAsync(int notificationId, int userId)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.NotificationID == notificationId && n.UserID == userId);

        if (notification is null)
            return (null, false);

        if (notification.Status == "Read")
            return (MapToDto(notification), true);

        notification.Status = "Read";
        await _context.SaveChangesAsync();
        return (MapToDto(notification), false);
    }

    private static NotificationResponseDto MapToDto(Notification n) => new()
    {
        NotificationId = n.NotificationID,
        UserId         = n.UserID,
        CourseId       = n.CourseID,
        Message        = n.Message,
        Category       = n.Category,
        Status         = n.Status,
        CreatedDate    = n.CreatedDate
    };

    public async Task<bool> HasNotificationTodayAsync(int userId, int certificationId, string category)
    {
        var todayUtc  = DateTime.UtcNow.Date;
        var tomorrowUtc = todayUtc.AddDays(1);

        return await _context.Notifications.AnyAsync(n =>
            n.UserID   == userId   &&
            n.Category == category &&
            n.Message.Contains($"(ID: {certificationId})") &&
            n.CreatedDate >= todayUtc &&
            n.CreatedDate <  tomorrowUtc);
    }

    public async Task<List<NotificationResponseDto>> GetUnreadAsync(int userId, string? category)
    {
        var query = _context.Notifications
            .Where(n => n.UserID == userId && n.Status == "Unread");

        if (!string.IsNullOrEmpty(category))
            query = query.Where(n => n.Category == category);

        return await query
            .OrderByDescending(n => n.CreatedDate)
            .Select(n => new NotificationResponseDto
            {
                NotificationId = n.NotificationID,
                UserId         = n.UserID,
                CourseId       = n.CourseID,
                Message        = n.Message,
                Category       = n.Category,
                Status         = n.Status,
                CreatedDate    = n.CreatedDate
            })
            .ToListAsync();
    }
}
