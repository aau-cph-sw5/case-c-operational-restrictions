using Backend.Data;

using DatabaseLayer.Tests;

namespace Backend.Tests.Data;

public class OperationalRestrictionRepositoryTest(
  TestDatabaseFixture fixture) : IClassFixture<TestDatabaseFixture>
{
    [Fact]
    public async Task GetAllAsyncShouldReturnList()
    {
        // Arrange
        var context = fixture.CreateContext();
        var repo = new OperationalRestrictionRepository(context);

        // Act
        var actual = await repo.GetAllAsync();

        // Assert
        Assert.NotNull(actual);
        Assert.True(actual.Count > 0);
    }
}
