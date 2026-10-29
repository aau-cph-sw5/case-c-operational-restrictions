using System.Reflection;
using System.Runtime.CompilerServices;

using Backend.Data;
using Backend.Exceptions;
using Backend.Models.Entities;
using Backend.Models.Enums;
using Backend.Services;

namespace Backend.Tests;

public class SignatureServiceTests
{
    // Users come from a seeded in-memory database, but required signatures and signatures are still kept
    // in static lists, so every test uses its own restriction id.
    // xUnit creates a new test class instance per test, so each test gets a fresh service and database.
    private readonly AppDbContext _appDbContext;
    private readonly SignatureService _service;

    public SignatureServiceTests()
    {
        _appDbContext = TestDatabase.CreateSeededContext();
        _service = new SignatureService(_appDbContext);
    }

    [Fact]
    public void SignatureHasNoPublicSetters()
    {
        var mutableProperties = typeof(Signature)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.SetMethod is { IsPublic: true }
                && !property.SetMethod.ReturnParameter
                    .GetRequiredCustomModifiers()
                    .Contains(typeof(IsExternalInit)))
            .Select(property => property.Name)
            .ToList();

        Assert.Empty(mutableProperties);
    }

    [Fact]
    public void SignRecordsSignerRoleVersionAndServerTimestamp()
    {
        var restrictionVersionId = Guid.NewGuid();
        var required = _service.CreateRequiredSignatures(restrictionVersionId).First();
        var before = DateTime.UtcNow;

        var signature = _service.Sign(required.Id, required.SigneeId);

        Assert.Equal(required.SigneeId, signature.SigneeId);
        Assert.Equal(restrictionVersionId, signature.OperationalRestrictionId);
        Assert.Equal(_appDbContext.Users.Single(x => x.Id == required.SigneeId).Role, signature.SigneeRole);
        Assert.True(signature.SignedAt >= before);
    }

    [Fact]
    public void SignByAnotherUserThrows()
    {
        var required = _service.CreateRequiredSignatures(Guid.NewGuid()).First();

        Assert.Throws<NotRequiredSignerException>(() => _service.Sign(required.Id, Guid.NewGuid()));
    }

    [Fact]
    public void SignTwiceThrows()
    {
        var required = _service.CreateRequiredSignatures(Guid.NewGuid()).First();
        _service.Sign(required.Id, required.SigneeId);

        Assert.Throws<RequiresSignatureNotSignableException>(() => _service.Sign(required.Id, required.SigneeId));
    }

    [Fact]
    public void InvalidateRequiredSignaturesInvalidatesSignedAndBlocksFurtherSigning()
    {
        var restrictionVersionId = Guid.NewGuid();
        var requiredSignatures = _service.CreateRequiredSignatures(restrictionVersionId);
        var signature = _service.Sign(requiredSignatures[0].Id, requiredSignatures[0].SigneeId);
        Assert.True(_service.IsSignatureValid(signature.Id));

        _service.InvalidateRequiredSignatures(restrictionVersionId);

        Assert.False(_service.IsSignatureValid(signature.Id));
        Assert.Throws<RequiresSignatureNotSignableException>(() =>
            _service.Sign(requiredSignatures[1].Id, requiredSignatures[1].SigneeId));
    }

    [Fact]
    public void CreateRequiredSignaturesSkipsUsersOnHolidayAndStartsUnsigned()
    {
        var usersNotOnHoliday = _appDbContext.Users.Count(x => x.WorkingStatus != UserWorkingStatus.OnHoliday);

        var required = _service.CreateRequiredSignatures(Guid.NewGuid());

        Assert.Equal(usersNotOnHoliday, required.Count);
        Assert.DoesNotContain(required, x => x.SigneeId == DbSeeder.MikkelId);
        Assert.All(required, x => Assert.Equal(RequiresSignatureStatus.PendingSignature, x.RequiresSignatureStatus));
    }
}
