using Backend.Models.Entities;
using Backend.Models.Results;

namespace Backend.Services;

// Sends a notification to the recipient's phone. While no sender is registered, deliveries
// become Undeliverable instead of being lost.
public interface INotificationSender
{
  Task<NotificationSendResult> SendAsync(Notification notification, CancellationToken cancellationToken);
}
