using Backend.Data;
using Backend.Models.Entities;
using Backend.Models.Enums;

using Microsoft.EntityFrameworkCore;

using Xunit;

namespace Backend.Tests;

public class NotificationDatabaseTests
{
    [Fact]
    public async Task CanSaveAndRetrieveNotificationWithDelivery()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;

        await using var db = new AppDbContext(options);

        var userId = Guid.NewGuid();
        var notificationId = Guid.NewGuid();

        db.Users.Add(new User
        {
            Id = userId,
            Name = "Integration Test User",
            Email = $"test-{userId}@example.test",
            PasswordHash = "test-only-hash"
        });

        db.Notifications.Add(new Notification
        {
            Id = notificationId,
            RecipientUserId = userId,
            Type = NotificationType.SystemInfo,
            Title = "Test notification",
            Message = "Testing EF Core",
            RequiresAction = false,
            CreatedAt = DateTime.UtcNow
        });

        db.NotificationDeliveries.Add(new NotificationDelivery
        {
            Id = Guid.NewGuid(),
            NotificationId = notificationId,
            Status = DeliveryStatus.Failed,
            AttemptCount = 1,
            NextAttemptAt = DateTime.UtcNow.AddMinutes(5),
            LastError = "Simulated delivery failure"
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        db.ChangeTracker.Clear();

        var saved = await db.Notifications
            .Include(n => n.Deliveries)
            .SingleAsync(n => n.Id == notificationId, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(userId, saved.RecipientUserId);
        Assert.Single(saved.Deliveries);
        Assert.Equal(DeliveryStatus.Failed, saved.Deliveries[0].Status);
        Assert.Equal(1, saved.Deliveries[0].AttemptCount);
        Assert.NotNull(saved.Deliveries[0].NextAttemptAt);
        Assert.Equal(
            "Simulated delivery failure",
            saved.Deliveries[0].LastError);
    }
}
