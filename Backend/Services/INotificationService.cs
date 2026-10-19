using Backend.Models.Requests;

namespace Backend.Services;

public interface INotificationService
{
  Guid AddNotification(CreateNotificationRequest request);

  Task ResolveActionRequiredAsync(Guid operationalRestrictionId, Guid recipientUserId);
}
