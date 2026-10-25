using Backend.Models.DTOs;
using Backend.Models.Entities;
using Backend.Models.Enum;

namespace Backend.Tests;

public class NotificationDtoTests
{
    [Fact]
    public void FromDalEntityMapsAllFields()
    {
        var id = Guid.NewGuid();
        var restrictionId = Guid.NewGuid();
        var createdAt = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);
        var readAt = new DateTime(2026, 10, 2, 0, 0, 0, DateTimeKind.Utc);
        var resolvedAt = new DateTime(2026, 10, 3, 0, 0, 0, DateTimeKind.Utc);

        var notification = new Notification
        {
            Id = id,
            RecipientUserId = Guid.NewGuid(),
            OperationalRestrictionId = restrictionId,
            Type = NotificationType.SignatureRequired,
            Title = "Signature required",
            Message = "Sign the restriction",
            RequiresAction = true,
            CreatedAt = createdAt,
            ReadAt = readAt,
            ResolvedAt = resolvedAt
        };

        var dto = NotificationDto.FromDalEntity(notification);

        Assert.Equal(id, dto.Id);
        Assert.Equal(restrictionId, dto.OperationalRestrictionId);
        Assert.Equal(NotificationType.SignatureRequired, dto.Type);
        Assert.Equal("Signature required", dto.Title);
        Assert.Equal("Sign the restriction", dto.Message);
        Assert.True(dto.RequiresAction);
        Assert.Equal(createdAt, dto.CreatedAt);
        Assert.Equal(readAt, dto.ReadAt);
        Assert.Equal(resolvedAt, dto.ResolvedAt);
    }

    [Fact]
    public void FromDalEntityMapsNullableFieldsAsNull()
    {
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            RecipientUserId = Guid.NewGuid(),
            Type = NotificationType.SystemInfo,
            Title = "Info",
            RequiresAction = false,
            CreatedAt = DateTime.UtcNow
        };

        var dto = NotificationDto.FromDalEntity(notification);

        Assert.Null(dto.OperationalRestrictionId);
        Assert.Null(dto.Message);
        Assert.Null(dto.ReadAt);
        Assert.Null(dto.ResolvedAt);
    }
}
