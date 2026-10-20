using Backend.Models.Enum;

namespace Backend.Models.Requests;

public record CreateNotificationRequest(
  Guid RecipientUserId,
  NotificationType Type,
  bool RequiresAction,
  Guid? OperationalRestrictionId = null,
  string? Title = null,
  string? Message = null
);
