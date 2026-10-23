using Backend.Data;
using Backend.Lifecycle;
using Backend.Models.DTOs;
using Backend.Models.Entities;
using Backend.Models.Requests;
using Backend.Services;

namespace Backend.Tests;

public class OperationalRestrictionServiceTests
{
    private static OperationalRestrictionService CreateService(
        FakeOperationalRestrictionRepository repository)
    {
        var lifecycle = new RestrictionLifecycleService(
            new IRestrictionGuard[] { new StartDateIsFoundGuard() });

        return new OperationalRestrictionService(lifecycle, repository);
    }

    [Fact]
    public async Task EditRestrictionCreatesNewVersionAndLeavesOriginalUntouched()
    {
        // Arrange
        var repo = new FakeOperationalRestrictionRepository();
        var service = CreateService(repo);

        var creator = Guid.NewGuid();
        var created = await service.CreateRestrictionAsync(
            new CreateRestrictionRequest(
                creator,
                "Maintenance work",
                DateTime.UtcNow,
                DateTime.UtcNow.AddHours(8)));

        // Act
        var editor = Guid.NewGuid();
        await service.EditRestrictionAsync(
            created.RestrictionId,
            new OperationalRestrictionDto
            {
                CreatedBy = editor,
                RestrictionDescription = "Maintenance work (updated)"
            });

        // Assert
        Assert.Equal(2, repo.Restrictions.Count);

        var original = repo.Restrictions.Single(
            r => r.RestrictionId == created.RestrictionId);
        Assert.Null(original.OriginalOperationalRestrictionId);
        Assert.Equal(0, original.Version);
        Assert.Equal("Maintenance work", original.RestrictionDescription);
        Assert.Equal(creator, original.CreatedBy);

        var clone = repo.Restrictions.Single(
            r => r.RestrictionId != created.RestrictionId);
        Assert.Equal(created.RestrictionId, clone.OriginalOperationalRestrictionId);
        Assert.Equal(1, clone.Version);
        Assert.Equal("Maintenance work (updated)", clone.RestrictionDescription);
        Assert.Equal(editor, clone.CreatedBy);

        Assert.False(repo.UpdateWasCalled);
    }
}