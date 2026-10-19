using Backend.Models.Enum;

namespace Backend.Models.Entities;

// One message to one recipient. This row is the source of truth for the in-app inbox (ADR 0008).
public class Notification
{
  public Guid Id { get; set; }
  public Guid RecipientUserId { get; set; }

  // The restriction version the notification is about; null for SystemInfo notifications.
  public Guid? OperationalRestrictionId { get; set; }
  public OperationalRestriction? OperationalRestriction { get; set; }

  // The user whose action caused the notification, shown on the inbox card; null for SystemInfo.
  public Guid? TriggeredByUserId { get; set; }

  public NotificationType Type { get; set; }

  // Free text for SystemInfo only. Other types build their text from the type and the restriction.
  public string? Title { get; set; }
  public string? Message { get; set; }

  public bool RequiresAction { get; set; }
  public DateTime CreatedAt { get; set; }
  public DateTime? ReadAt { get; set; }

  // Set when the required action is done, so the notification leaves the "Påkrævet" filter.
  public DateTime? ResolvedAt { get; set; }

  public List<NotificationDelivery> Deliveries { get; set; } = [];
}
