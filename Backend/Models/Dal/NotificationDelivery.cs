using Backend.Models.Enum;

namespace Backend.Models.Entities;

// The push delivery of a notification and its attempts. Sent means the push service accepted the
// message, not that the recipient saw it.
public class NotificationDelivery
{
  public Guid Id { get; set; }
  public Guid NotificationId { get; set; }
  public Notification Notification { get; set; } = null!;

  public DeliveryStatus Status { get; set; }
  public int AttemptCount { get; set; }
  public DateTime? NextAttemptAt { get; set; }
  public DateTime? SentAt { get; set; }
  public string? LastError { get; set; }
}
