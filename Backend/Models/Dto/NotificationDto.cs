using Backend.Models.Entities;
using Backend.Models.Enum;

namespace Backend.Models.DTOs;

// MODEL - DTO (Data Transfer Object):
// Defines the JSON structure sent over the network for a recipient's in-app inbox.
// Prevents exposing the Notification persistence entity directly to the client.
public class NotificationDto
{
  public Guid Id { get; set; }
  public Guid? OperationalRestrictionId { get; set; }
  public NotificationType Type { get; set; }
  public string? Title { get; set; }
  public string? Message { get; set; }
  public bool RequiresAction { get; set; }
  public DateTime CreatedAt { get; set; }
  public DateTime? ReadAt { get; set; }
  public DateTime? ResolvedAt { get; set; }

  public static NotificationDto FromDalEntity(Notification notification) =>
    new()
    {
      Id = notification.Id,
      OperationalRestrictionId = notification.OperationalRestrictionId,
      Type = notification.Type,
      Title = notification.Title,
      Message = notification.Message,
      RequiresAction = notification.RequiresAction,
      CreatedAt = notification.CreatedAt,
      ReadAt = notification.ReadAt,
      ResolvedAt = notification.ResolvedAt
    };
}
