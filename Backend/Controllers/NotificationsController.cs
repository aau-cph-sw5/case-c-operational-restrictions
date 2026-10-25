using Backend.Models.DTOs;
using Backend.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

// CONTROLLER (MVCS):
// Handles incoming HTTP requests, route binding, and HTTP status codes.
// The inbox is scoped to the authenticated user; the recipient id is never read from the request.
[ApiController]
[Authorize]
[Route("api/[controller]")]
public class NotificationsController : ApiControllerBase
{
  private readonly INotificationService _notificationService;

  public NotificationsController(INotificationService notificationService)
  {
    _notificationService = notificationService;
  }

  /// <summary>
  /// Returns all notifications addressed to the logged-in user, newest first.
  /// </summary>
  /// <remarks>
  /// Sample request:
  ///
  ///     GET /api/notifications
  /// </remarks>
  /// <response code="200">Returns the user's notifications, or an empty list if there are none</response>
  /// <response code="401">Returns when the request is not authenticated</response>
  [HttpGet]
  [ProducesResponseType(typeof(List<NotificationDto>), StatusCodes.Status200OK)]
  public async Task<ActionResult<List<NotificationDto>>> GetNotifications()
  {
    var notifications = await _notificationService.GetNotificationsForUserAsync(CurrentUserId);

    return Ok(notifications.Select(NotificationDto.FromDalEntity).ToList());
  }
}
