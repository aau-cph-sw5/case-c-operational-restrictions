using System.Reflection;
using System.Runtime.CompilerServices;

using Backend.Exceptions;
using Backend.Models.Entities;
using Backend.Models.Enum;
using Backend.Services;

namespace Backend.Tests;

public class SignatureServiceTests
{
    // The service keeps its data in static lists, so every test uses its own restriction id.
    private static SignatureService CreateService() => new(null!);

    [Fact]
    public void Signature_HasNoPublicSetters()
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
    public void Sign_RecordsSignerRoleVersionAndServerTimestamp()
    {
        var service = CreateService();
        var restrictionVersionId = Guid.NewGuid();
        var required = service.CreateRequiredSignatures(restrictionVersionId).First();
        var before = DateTime.UtcNow;

        var signature = service.Sign(required.Id, required.SigneeId);

        Assert.Equal(required.SigneeId, signature.SigneeId);
        Assert.Equal(restrictionVersionId, signature.OperationalRestrictionId);
        Assert.NotEqual(default, signature.SigneeRole);
        Assert.True(signature.SignedAt >= before);
    }

    [Fact]
    public void Sign_ByAnotherUser_Throws()
    {
        var service = CreateService();
        var required = service.CreateRequiredSignatures(Guid.NewGuid()).First();

        Assert.Throws<NotRequiredSignerException>(() => service.Sign(required.Id, Guid.NewGuid()));
    }

    [Fact]
    public void Sign_Twice_Throws()
    {
        var service = CreateService();
        var required = service.CreateRequiredSignatures(Guid.NewGuid()).First();
        service.Sign(required.Id, required.SigneeId);

        Assert.Throws<RequiresSignatureNotSignableException>(() => service.Sign(required.Id, required.SigneeId));
    }

    [Fact]
    public void InvalidateRequiredSignatures_InvalidatesSignedAndBlocksFurtherSigning()
    {
        var service = CreateService();
        var restrictionVersionId = Guid.NewGuid();
        var requiredSignatures = service.CreateRequiredSignatures(restrictionVersionId);
        var signature = service.Sign(requiredSignatures[0].Id, requiredSignatures[0].SigneeId);
        Assert.True(service.IsSignatureValid(signature.Id));

        service.InvalidateRequiredSignatures(restrictionVersionId);

        Assert.False(service.IsSignatureValid(signature.Id));
        Assert.Throws<RequiresSignatureNotSignableException>(() =>
            service.Sign(requiredSignatures[1].Id, requiredSignatures[1].SigneeId));
    }

    [Fact]
    public void CreateRequiredSignatures_SkipsUsersOnHolidayAndStartsUnsigned()
    {
        var service = CreateService();

        var required = service.CreateRequiredSignatures(Guid.NewGuid());

        Assert.Equal(4, required.Count);
        Assert.All(required, x => Assert.Equal(RequiresSignatureStatus.Unsigned, x.RequiresSignatureStatus));
    }
}
