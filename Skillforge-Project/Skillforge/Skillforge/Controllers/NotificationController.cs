using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Skillforge.Constants;
using Skillforge.Dto;
using Skillforge.Service;
using System.Security.Claims;

namespace Skillforge.Controller;

/// <summary>
/// Exposes notification endpoints for authenticated users.
/// </summary>
[Route("api/v1/[controller]")]
[ApiController]
public class NotificationController(INotificationService notificationService) : ControllerBase
{
    /// <summary>
    /// Marks a notification as Read.
    /// Returns 404 if the notification does not exist or belongs to another user.
    /// </summary>
    [HttpPatch("{id}")]
    [Authorize]
    [ProducesResponseType(typeof(NotificationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        try
        {
            var userIdClaim = User.FindFirstValue("id");
            if (!int.TryParse(userIdClaim, out int userId))
                return Unauthorized(new { message = "Invalid token." });

            var (notification, alreadyRead) = await notificationService.MarkAsReadAsync(id, userId);

            if (notification is null)
                return NotFound(new { message = "Notification not found." });

            var message = alreadyRead
                ? "Notification is already marked as read."
                : "Notification marked as read.";

            return Ok(new { message, data = notification });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Returns all unread notifications for the authenticated user.
    /// Optionally filter by category (e.g. "Certification", "Report").
    /// </summary>
    /// <param name="category">Optional category filter.</param>
    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(List<NotificationResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetUnread([FromQuery] string? category)
    {
        try
        {
            var userIdClaim = User.FindFirstValue("id");
            if (!int.TryParse(userIdClaim, out int userId))
                return Unauthorized(new { message = "Invalid token." });

            var notifications = await notificationService.GetUnreadAsync(userId, category);

            if (!notifications.Any())
                return Ok(new { message = NotificationMessages.NoUnreadNotifications, data = notifications });

            return Ok(new { data = notifications });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }
}
