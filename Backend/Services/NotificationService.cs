using Backend.Authorization;
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
  AppDbContext appDbContext
) : INotificationService
{
    private readonly AppDbContext _appDbContext = appDbContext;
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
        AddAuditEntry(notification, NotificationAuditEventType.Created, now);

        return notification.Id;
    }

    /// <summary>
    /// Marks the recipient's open action-required notifications for a restriction as resolved, so they
    /// leave the "Påkrævet" filter. The caller must call SaveChangesAsync together with the action itself.
    /// </summary>
    public async Task ResolveActionRequiredAsync(Guid operationalRestrictionId, Guid recipientUserId)
    {
        var openNotifications = await _appDbContext.Notifications
          .Include(x => x.Deliveries)
          .Where(x => x.OperationalRestrictionId == operationalRestrictionId
            && x.RecipientUserId == recipientUserId
            && x.RequiresAction
            && x.ResolvedAt == null)
          .ToListAsync();

        var now = DateTime.UtcNow;
        foreach (var notification in openNotifications)
        {
            notification.ResolvedAt = now;
            Resolve(notification, now);
        }
    }

    /// <summary>
    /// Reacts to a restriction moving to a new lifecycle step (MET-C-019):
    /// the previous step is done, so its open action-required notifications are resolved for every
    /// recipient, and the holders of the role the new step waits on are notified.
    /// Saves its changes on the shared AppDbContext, so when the caller has a transaction open the
    /// notifications are committed together with the state change, or not at all.
    /// </summary>
    public async Task NotifyRestrictionTransitionAsync(OperationalRestriction newVersion, Guid actorId)
    {
        var now = DateTime.UtcNow;

        await ResolveOpenForRestrictionAsync(newVersion, now);

        switch (newVersion.State)
        {
            case RestrictionState.PendingDutyOperationsManagerApproval:
                await NotifyRoleAsync(
                  RoleNames.DutyOperationsManager,
                  newVersion,
                  actorId,
                  $"Driftsrestriktionen \"{newVersion.Title}\" afventer din godkendelse og underskrift.");
                break;

            case RestrictionState.PendingControlRoomSupervisorApproval:
                await NotifyRoleAsync(
                  RoleNames.ControlroomSupervisor,
                  newVersion,
                  actorId,
                  $"Driftsrestriktionen \"{newVersion.Title}\" afventer din underskrift.");
                break;

            case RestrictionState.ReturnedForChanges:
                var reason = string.IsNullOrWhiteSpace(newVersion.ReviewDescription)
                  ? string.Empty
                  : $" Begrundelse: {newVersion.ReviewDescription}";
                AddNotification(new CreateNotificationRequest(
                  RecipientUserId: newVersion.OriginatorUserId,
                  Type: NotificationType.ChangesRequested,
                  RequiresAction: true,
                  OperationalRestrictionId: newVersion.RestrictionId,
                  Message: $"Driftsrestriktionen \"{newVersion.Title}\" er sendt retur med ønske om ændringer.{reason}"));
                break;
        }

        await _appDbContext.SaveChangesAsync();
    }

    // Every version of a restriction has its own id, so open notifications may point at an older
    // version of the same restriction. All versions share the original id.
    private async Task ResolveOpenForRestrictionAsync(OperationalRestriction newVersion, DateTime now)
    {
        var originalId = newVersion.OriginalOperationalRestrictionId ?? newVersion.RestrictionId;

        var versionIds = await _appDbContext.OperationalRestrictions
          .Where(r => r.RestrictionId == originalId || r.OriginalOperationalRestrictionId == originalId)
          .Select(r => r.RestrictionId)
          .ToListAsync();

        var openNotifications = await _appDbContext.Notifications
          .Include(x => x.Deliveries)
          .Where(x => x.OperationalRestrictionId != null
            && versionIds.Contains(x.OperationalRestrictionId.Value)
            && x.RequiresAction
            && x.ResolvedAt == null)
          .ToListAsync();

        foreach (var notification in openNotifications)
        {
            Resolve(notification, now);
        }
    }

    // Notifies every holder of the role, except the user who caused the transition.
    private async Task NotifyRoleAsync(string roleName, OperationalRestriction restriction, Guid actorId, string message)
    {
        var recipientIds = await _appDbContext.UserRoleAssignments
          .Where(a => a.Role.Name == roleName && a.UserId != actorId)
          .Select(a => a.UserId)
          .Distinct()
          .ToListAsync();

        foreach (var recipientId in recipientIds)
        {
            AddNotification(new CreateNotificationRequest(
              RecipientUserId: recipientId,
              Type: NotificationType.SignatureRequired,
              RequiresAction: true,
              OperationalRestrictionId: restriction.RestrictionId,
              Message: message));
        }
    }

    // Resolving stops further reminders: pushes that have not gone out yet are cancelled.
    private void Resolve(Notification notification, DateTime now)
    {
        notification.ResolvedAt = now;
        AddAuditEntry(notification, NotificationAuditEventType.Resolved, now);

        foreach (var delivery in notification.Deliveries
          .Where(d => d.Status == DeliveryStatus.Pending || d.Status == DeliveryStatus.Failed))
        {
            delivery.Status = DeliveryStatus.Cancelled;
            delivery.NextAttemptAt = null;
        }
    }

    /// <summary>
    /// Returns all notifications for a recipient, newest first, for the in-app inbox.
    /// </summary>
    public async Task<List<Notification>> GetNotificationsForUserAsync(Guid recipientUserId)
    {
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
            var now = DateTime.UtcNow;
            notification.ReadAt = now;
            AddAuditEntry(notification, NotificationAuditEventType.Read, now);
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
          .Include(x => x.Deliveries)
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
                    Resolve(notification, now);
                }
            }
        }
    }

    // Adds an append-only audit entry (C-021). Saved by the caller together with the change it records.
    private void AddAuditEntry(Notification notification, NotificationAuditEventType eventType, DateTime occurredAt)
    {
        _appDbContext.Add(new NotificationAuditEntry
        {
            Id = Guid.NewGuid(),
            NotificationId = notification.Id,
            RecipientUserId = notification.RecipientUserId,
            OperationalRestrictionId = notification.OperationalRestrictionId,
            EventType = eventType,
            OccurredAt = occurredAt
        });
    }
}
