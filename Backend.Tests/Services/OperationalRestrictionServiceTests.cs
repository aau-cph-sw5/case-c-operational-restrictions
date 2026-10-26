using Backend.Services;

using DatabaseLayer.Tests;

namespace Backend.Tests.Services;

public class OperationalRestrictionServiceTests(
  TestDatabaseFixture fixture
  ) : IClassFixture<TestDatabaseFixture>
{
  [Fact]
  public async Task GetAllAsyncShouldReturnList()
  {
    // Arrange
    var context = fixture.CreateContext();
    var sut = new OperationalRestrictionService(context, new SignatureService(context));

    // Act
    var actual = await sut.GetAllAsync();

    // Assert
    Assert.Equal(2, actual.Count);
  }
}