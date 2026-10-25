using Backend.Data;
using Backend.Models.Entities;
using Backend.Models.Enum;
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
