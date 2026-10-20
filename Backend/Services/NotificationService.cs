using Backend.Data;
using Backend.Exceptions;
using Backend.Models.Entities;
using Backend.Models.Enum;
using Backend.Models.Requests;

using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

// Outbox (ADR 0008): these methods only add changes to the shared AppDbContext. The calling
// service saves them in the same SaveChanges as the restriction's state change, so a state
// change and its notifications are stored together or not at all.
public class NotificationService(
  AppDbContext appDbContext
) : INotificationService
{
  private readonly AppDbContext _appDbContext = appDbContext;

  /// <summary>
  /// Adds a notification with a pending push delivery. The caller must call SaveChangesAsync.
  /// </summary>
  public Guid AddNotification(CreateNotificationRequest request)
  {
    if (request.Type == NotificationType.SystemInfo)
    {
      if (string.IsNullOrWhiteSpace(request.Title))
        throw new InvalidNotificationException("A SystemInfo notification must have a title.");
      if (request.OperationalRestrictionId != null)
        throw new InvalidNotificationException("A SystemInfo notification cannot be about a restriction.");
    }
    else if (request.OperationalRestrictionId == null)
    {
      throw new InvalidNotificationException($"A {request.Type} notification must be about a restriction.");
    }

    var now = DateTime.UtcNow;
    var notification = new Notification
    {
      Id = Guid.NewGuid(),
      RecipientUserId = request.RecipientUserId,
      OperationalRestrictionId = request.OperationalRestrictionId,
      Type = request.Type,
      Title = request.Title,
      Message = request.Message,
      RequiresAction = request.RequiresAction,
      CreatedAt = now,
      Deliveries =
      [
        new NotificationDelivery
        {
          Id = Guid.NewGuid(),
          Status = DeliveryStatus.Pending,
          AttemptCount = 0,
          NextAttemptAt = now
        }
      ]
    };

    _appDbContext.Add(notification);

    return notification.Id;
  }

  /// <summary>
  /// Marks the recipient's open action-required notifications for a restriction as resolved, so they
  /// leave the "Påkrævet" filter. The caller must call SaveChangesAsync together with the action itself.
  /// </summary>
  public async Task ResolveActionRequiredAsync(Guid operationalRestrictionId, Guid recipientUserId)
  {
    var openNotifications = await _appDbContext.Notifications
      .Where(x => x.OperationalRestrictionId == operationalRestrictionId
        && x.RecipientUserId == recipientUserId
        && x.RequiresAction
        && x.ResolvedAt == null)
      .ToListAsync();

    var now = DateTime.UtcNow;
    foreach (var notification in openNotifications)
    {
      notification.ResolvedAt = now;
    }
  }
}
