using Backend.Models.Entities;
using Backend.Models.Enum;
using Backend.Models.Results;

namespace Backend.Services;

// One implementation per channel (Web Push, email). A channel with no registered sender is
// treated as unavailable, which is how email stays off until Metro provides a mail relay.
public interface INotificationSender
{
  NotificationChannel Channel { get; }

  Task<NotificationSendResult> SendAsync(Notification notification, CancellationToken cancellationToken);
}
