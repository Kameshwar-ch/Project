using Skillforge.Domain;
using Skillforge.Dto;

namespace Skillforge.Repository;

public interface INotificationRepository
{
    /// <summary>Persists a new notification and returns the generated NotificationID.</summary>
    Task<int> AddAsync(Notification notification);

    /// <summary>
    /// Returns all unread notifications for the given user,
    /// optionally filtered by category, ordered newest first.
    /// </summary>
    Task<List<NotificationResponseDto>> GetUnreadAsync(int userId, string? category);

    /// <summary>
    /// Marks a notification as Read.
    /// Returns (null, false)  → not found or wrong owner.
    /// Returns (dto,  true)   → was already Read, no change made.
    /// Returns (dto,  false)  → just marked as Read.
    /// </summary>
    Task<(NotificationResponseDto? Notification, bool AlreadyRead)> MarkAsReadAsync(int notificationId, int userId);

    /// <summary>
    /// Returns true if a notification for the given certification was already
    /// created today (UTC), preventing duplicate daily alerts from Option C.
    /// </summary>
    Task<bool> HasNotificationTodayAsync(int userId, int certificationId, string category);
}
