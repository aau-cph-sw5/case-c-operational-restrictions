using Backend.Data;
using Backend.Exceptions;
using Backend.Models.Entities;
using Backend.Models.Enums;
using Backend.Models.Requests;

using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

// Outbox (ADR 0008): these methods only add changes to the shared AppDbContext. The calling
// service saves them in the same SaveChanges as the restriction's state change, so a state
// change and its notifications are stored together or not at all.
public class NotificationService(
  AppDbContext appDbContext,
  SignatureService signatureService
) : INotificationService
{
    private readonly AppDbContext _appDbContext = appDbContext;
    private readonly SignatureService _signatureService = signatureService;

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

    /// <summary>
    /// Returns all notifications for a recipient, newest first, for the in-app inbox.
    /// First resolves any open action-required notifications whose underlying action is now
    /// complete, so the inbox always reflects the current state on load.
    /// </summary>
    public async Task<List<Notification>> GetNotificationsForUserAsync(Guid recipientUserId)
    {
        await UpdateAllNotificationsAsync();
        await _appDbContext.SaveChangesAsync();

        return await _appDbContext.Notifications
          .AsNoTracking()
          .Where(x => x.RecipientUserId == recipientUserId)
          .OrderByDescending(x => x.CreatedAt)
          .ToListAsync();
    }

    /// <summary>
    /// Marks a notification as read for the specified recipient.
    /// </summary>
    public async Task<bool> MarkAsReadAsync(Guid notificationId, Guid recipientUserId)
    {
        var notification = await _appDbContext.Notifications
          .FirstOrDefaultAsync(x =>
              x.Id == notificationId &&
              x.RecipientUserId == recipientUserId);

        if (notification == null)
        {
            return false;
        }

        if (notification.ReadAt == null)
        {
            notification.ReadAt = DateTime.UtcNow;
            await _appDbContext.SaveChangesAsync();
        }

        return true;
    }

    /// <summary>
    /// Resolves all open action-required notifications whose underlying action is now complete.
    /// For SignatureRequired notifications, checks whether a RequiresSignature for the same
    /// restriction has been completed. The caller must call SaveChangesAsync.
    /// </summary>
    public async Task UpdateAllNotificationsAsync()
    {
        var openNotifications = await _appDbContext.Notifications
          .Where(x => x.RequiresAction && x.ResolvedAt == null)
          .ToListAsync();

        var now = DateTime.UtcNow;
        foreach (var notification in openNotifications)
        {
            if (notification.Type == NotificationType.SignatureRequired
                && notification.OperationalRestrictionId != null)
            {
                var requiresSignatures = _signatureService
                  .GetRequiresSignaturesByOperationalRestrictionId(notification.OperationalRestrictionId.Value);

                if (requiresSignatures.Any(rs => rs.SigneeId == notification.RecipientUserId
                    && rs.RequiresSignatureStatus == RequiresSignatureStatus.CompletedSignature))
                {
                    notification.ResolvedAt = now;
                }
            }
        }
    }
}
