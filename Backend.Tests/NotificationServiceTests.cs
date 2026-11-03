using Backend.Data;
using Backend.Models.Entities;
using Backend.Models.Enums;
using Backend.Services;

using Microsoft.EntityFrameworkCore;

namespace Backend.Tests;

public class NotificationServiceTests : IDisposable
{
    private readonly AppDbContext _db;

    public NotificationServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"notifications-{Guid.NewGuid()}")
            .Options;

        _db = new AppDbContext(options);
    }

    public void Dispose()
    {
        _db.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task GetNotificationsForUserAsyncReturnsOnlyTheRecipientsNotifications()
    {
        var user = Guid.NewGuid();
        var other = Guid.NewGuid();
        SeedNotification(Guid.NewGuid(), other, DateTime.UtcNow);
        var expected = SeedNotification(Guid.NewGuid(), user, DateTime.UtcNow);

        var service = new NotificationService(_db);

        var result = await service.GetNotificationsForUserAsync(user);

        Assert.Single(result);
        Assert.Equal(expected, result[0].Id);
        Assert.Equal(user, result[0].RecipientUserId);
    }

    [Fact]
    public async Task GetNotificationsForUserAsyncReturnsNewestFirst()
    {
        var user = Guid.NewGuid();
        var oldest = SeedNotification(Guid.NewGuid(), user, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var newest = SeedNotification(Guid.NewGuid(), user, new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc));

        var service = new NotificationService(_db);

        var result = await service.GetNotificationsForUserAsync(user);

        Assert.Equal(2, result.Count);
        Assert.Equal(newest, result[0].Id);
        Assert.Equal(oldest, result[1].Id);
    }

    [Fact]
    public async Task GetNotificationsForUserAsyncReturnsEmptyListForUnknownUser()
    {
        var service = new NotificationService(_db);

        var result = await service.GetNotificationsForUserAsync(Guid.NewGuid());

        Assert.Empty(result);
    }

    [Fact]
    public async Task MarkAsReadAsyncUpdatesReadAt()
    {
        // Arrange: Create a test notification
        var userId = Guid.NewGuid();
        var notificationId = SeedNotification(
            Guid.NewGuid(),
            userId,
            DateTime.UtcNow
        );

        var service = new NotificationService(_db);

        // Act: Mark the notification as read
        var result = await service.MarkAsReadAsync(notificationId, userId);

        // Assert: Verify the notification was marked as read
        var notification = await _db.Notifications
         .FirstAsync(x => x.Id == notificationId);

        Assert.True(result);
        Assert.NotNull(notification.ReadAt);
    }
    [Fact]
    public async Task MarkAsReadAsyncCannotMarkAnotherUsersNotification()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        var notificationId = SeedNotification(
            Guid.NewGuid(),
            ownerId,
            DateTime.UtcNow
        );

        var service = new NotificationService(_db);

        // Act
        var result = await service.MarkAsReadAsync(notificationId, otherUserId);

        // Assert
        var notification = await _db.Notifications
            .FirstAsync(x => x.Id == notificationId);

        Assert.False(result);
        Assert.Null(notification.ReadAt);
    }
    [Fact]
    public async Task MarkAsReadAsyncDoesNotChangeExistingReadAt()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var notificationId = SeedNotification(
            Guid.NewGuid(),
            userId,
            DateTime.UtcNow
        );

        var originalReadAt = new DateTime(
            2026, 11, 1, 12, 0, 0, DateTimeKind.Utc
        );

        var notification = await _db.Notifications
            .FirstAsync(x => x.Id == notificationId);

        notification.ReadAt = originalReadAt;
        await _db.SaveChangesAsync();

        var service = new NotificationService(_db);

        // Act
        var result = await service.MarkAsReadAsync(notificationId, userId);

        // Assert
        Assert.True(result);
        Assert.Equal(originalReadAt, notification.ReadAt);
    }















    private Guid SeedNotification(Guid id, Guid recipientUserId, DateTime createdAt)
    {
        _db.Notifications.Add(new Notification
        {
            Id = id,
            RecipientUserId = recipientUserId,
            Type = NotificationType.SystemInfo,
            Title = "Test",
            Message = "Test",
            RequiresAction = false,
            CreatedAt = createdAt
        });

        _db.SaveChanges();

        return id;
    }
}
