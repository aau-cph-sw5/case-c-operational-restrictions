using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

using Backend.Controllers;
using Backend.Models.DTOs;
using Backend.Models.Entities;
using Backend.Models.Enums;
using Backend.Models.Requests;
using Backend.Services;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Tests;

// Hand-rolled stub: the test project has no mocking library.
public class StubNotificationService : INotificationService
{
    public List<Notification> NotificationsToReturn { get; set; } = [];

    public Guid? ReceivedRecipientUserId { get; private set; }

    public Guid AddNotification(CreateNotificationRequest request) => throw new NotSupportedException();

    public Task ResolveActionRequiredAsync(Guid operationalRestrictionId, Guid recipientUserId) =>
        throw new NotSupportedException();

    public Task<List<Notification>> GetNotificationsForUserAsync(Guid recipientUserId)
    {
        ReceivedRecipientUserId = recipientUserId;
        return Task.FromResult(NotificationsToReturn);
    }
}

public class NotificationsControllerTests
{
    private static NotificationsController CreateController(INotificationService service, Guid userId)
    {
        var principal = new ClaimsPrincipal(
            new ClaimsIdentity([new Claim(JwtRegisteredClaimNames.Sub, userId.ToString())]));

        return new NotificationsController(service)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            }
        };
    }

    [Fact]
    public async Task GetNotificationsReturnsOkWithDtosForTheLoggedInUser()
    {
        var userId = Guid.NewGuid();
        var notificationId = Guid.NewGuid();
        var createdAt = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);

        var service = new StubNotificationService
        {
            NotificationsToReturn =
            [
                new Notification
                {
                    Id = notificationId,
                    RecipientUserId = userId,
                    Type = NotificationType.SystemInfo,
                    Title = "Test",
                    Message = "Test",
                    RequiresAction = false,
                    CreatedAt = createdAt
                }
            ]
        };

        var controller = CreateController(service, userId);

        var result = await controller.GetNotifications();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dtos = Assert.IsType<List<NotificationDto>>(ok.Value);
        var dto = Assert.Single(dtos);
        Assert.Equal(notificationId, dto.Id);
        Assert.Equal("Test", dto.Title);
        Assert.Equal(createdAt, dto.CreatedAt);
        Assert.Equal(userId, service.ReceivedRecipientUserId);
    }

    [Fact]
    public async Task GetNotificationsUsesTheSubClaimAsRecipientId()
    {
        var userId = Guid.NewGuid();
        var service = new StubNotificationService();
        var controller = CreateController(service, userId);

        await controller.GetNotifications();

        Assert.Equal(userId, service.ReceivedRecipientUserId);
    }

    [Fact]
    public async Task GetNotificationsReturnsOkWithEmptyListWhenThereAreNoNotifications()
    {
        var service = new StubNotificationService();
        var controller = CreateController(service, Guid.NewGuid());

        var result = await controller.GetNotifications();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dtos = Assert.IsType<List<NotificationDto>>(ok.Value);
        Assert.Empty(dtos);
    }
}
