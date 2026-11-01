using System.Reflection;
using System.Runtime.CompilerServices;

using Backend.Exceptions;
using Backend.Models.Entities;
using Backend.Models.Enums;
using Backend.Services;

namespace Backend.Tests;

public class SignatureServiceTests
{
    // The service keeps its data in static lists, so every test uses its own restriction id.
    // xUnit creates a new test class instance per test, so each test gets a fresh service.
    private readonly SignatureService _service = new(null!);

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
        Assert.NotEmpty(signature.SigneeRoles);
        Assert.True(signature.SignedAt >= before);
    }

    [Fact]
    public void SignWithUserHavingMultipleRolesRecordsAllRoles()
    {
        var required = _service.CreateRequiredSignatures(Guid.NewGuid()).First();

        var users = GetStaticList<User>("_users");
        var roles = GetStaticList<Role>("_roles");
        var assignments = GetStaticList<UserRoleAssignment>("_userRoleAssignments");

        var signee = users.Single(u => u.Id == required.SigneeId);
        var extraRole = roles.First(r => !assignments.Any(a => a.User == signee && a.Role == r));
        assignments.Add(new UserRoleAssignment { User = signee, Role = extraRole });

        var signature = _service.Sign(required.Id, required.SigneeId);

        Assert.Equal(2, signature.SigneeRoles.Count);
        Assert.Contains(signature.SigneeRoles, r => r.Name == extraRole.Name);
    }

    private static List<T> GetStaticList<T>(string fieldName) =>
        (List<T>)typeof(SignatureService)
            .GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static)!
            .GetValue(null)!;

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

        var required = _service.CreateRequiredSignatures(Guid.NewGuid());

        Assert.Equal(4, required.Count);
        Assert.All(required, x => Assert.Equal(RequiresSignatureStatus.PendingSignature, x.RequiresSignatureStatus));
    }
}
