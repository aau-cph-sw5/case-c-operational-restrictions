
using Backend.Lifecycle;
using Backend.Models.DTOs;

namespace Backend.Tests;

public class RestrictionLifecycleServiceTests
{
    private readonly RestrictionLifecycleService _sut;

    public RestrictionLifecycleServiceTests()
    {
        _sut = new RestrictionLifecycleService(
            new IRestrictionGuard[]
            {
                new StartDateIsFoundGuard(),
                new OperationalRestictionIdIsFound(),
                new DescriptionTextIsFound(),
                new OriginatorSignatureIsFound(),
                new DomSignatureIsFound(),
                new CRSignatureIsFound(),
                new OperatorsAreAssigned()
            });
    }

    [Fact]
    public void EnsureCanCreateWithValidDataDoesNotThrow()
    {
        // Arrange
        var data = new OperationalRestrictionDto
        {
            OriginatorUserId = Guid.NewGuid(),
            RestrictionDescription = "Maintenance work"
        };

        // Act & Assert
        _sut.EnsureCanCreate(data);
    }

    [Fact]
    public void EnsureCanCreateWithInvalidDataIdentifiesFailedGuard()
    {
        // Arrange
        var data = new OperationalRestrictionDto
        {
            OriginatorUserId = Guid.NewGuid(),
            RestrictionDescription = ""
        };

        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => _sut.EnsureCanCreate(data));

        // Assert
        Assert.Contains(
            "CreationDataIsValid",
            exception.Message);
    }
}
