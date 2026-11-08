using Backend.Authorization;
using Backend.Data;
using Backend.Models.Entities;
using Backend.Services;

using Microsoft.EntityFrameworkCore;

namespace Backend.Tests.Services;

// Tests for RBAC resolution in PermissionService against the seeded in-memory
// database: user -> UserRoleAssignments -> roles -> RolePermissions -> permissions.
public class PermissionServiceTests
{
    [Fact]
    public async Task UserWithGrantingRoleHasPermission()
    {
        var service = CreateService();

        // Anna is seeded with the DutyOperationsManager role, which grants every permission.
        Assert.True(await service.UserHasPermissionAsync(DbSeeder.AnnaId, Permissions.OperationalRestrictionApprove));
        Assert.True(await service.UserHasPermissionAsync(DbSeeder.LarsId, Permissions.OperationalRestrictionRead));
    }

    [Fact]
    public async Task UserWithoutGrantingRoleLacksPermission()
    {
        var service = CreateService();

        // Lars is an Operator: read/create/sign, but not approve.
        Assert.False(await service.UserHasPermissionAsync(DbSeeder.LarsId, Permissions.OperationalRestrictionApprove));
        // Mikkel is a ControlroomSupervisor: read/sign/approve, but not create.
        Assert.False(await service.UserHasPermissionAsync(DbSeeder.MikkelId, Permissions.OperationalRestrictionCreate));
    }

    [Fact]
    public async Task UserWithoutAnyRoleAssignmentHasNoPermissions()
    {
        var service = CreateService();
        var unknownUserId = Guid.NewGuid();

        Assert.False(await service.UserHasPermissionAsync(unknownUserId, Permissions.OperationalRestrictionRead));
        Assert.Empty(await service.GetPermissionsForUserAsync(unknownUserId));
    }

    [Fact]
    public async Task PermissionNameIsTrimmedBeforeLookup()
    {
        var service = CreateService();

        Assert.True(await service.UserHasPermissionAsync(DbSeeder.LarsId, $"  {Permissions.OperationalRestrictionRead}  "));
    }

    [Fact]
    public async Task GetPermissionsForUserAsyncReturnsDistinctNamesGrantedThroughRoles()
    {
        var service = CreateService();

        var annaPermissions = await service.GetPermissionsForUserAsync(DbSeeder.AnnaId);
        Assert.Equal(4, annaPermissions.Count);
        Assert.Contains(Permissions.OperationalRestrictionRead, annaPermissions);
        Assert.Contains(Permissions.OperationalRestrictionCreate, annaPermissions);
        Assert.Contains(Permissions.OperationalRestrictionSign, annaPermissions);
        Assert.Contains(Permissions.OperationalRestrictionApprove, annaPermissions);

        var larsPermissions = await service.GetPermissionsForUserAsync(DbSeeder.LarsId);
        Assert.Equal(3, larsPermissions.Count);
        Assert.DoesNotContain(Permissions.OperationalRestrictionApprove, larsPermissions);
    }

    [Fact]
    public async Task AssigningAnAdditionalRoleUnionsThePermissions()
    {
        var context = TestDatabase.CreateSeededContext();
        var service = new PermissionService(context);

        Assert.False(await service.UserHasPermissionAsync(DbSeeder.LarsId, Permissions.OperationalRestrictionApprove));

        var dutyOperationsManagerRole = await context.Roles.SingleAsync(r => r.Name == RoleNames.DutyOperationsManager, TestContext.Current.CancellationToken);
        context.UserRoleAssignments.Add(new UserRoleAssignment { UserId = DbSeeder.LarsId, RoleId = dutyOperationsManagerRole.Id });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        Assert.True(await service.UserHasPermissionAsync(DbSeeder.LarsId, Permissions.OperationalRestrictionApprove));
    }

    [Fact]
    public async Task RemovingARolePermissionGrantRevokesThePermission()
    {
        var context = TestDatabase.CreateSeededContext();
        var service = new PermissionService(context);

        var dutyOperationsManagerRole = await context.Roles.SingleAsync(r => r.Name == RoleNames.DutyOperationsManager, TestContext.Current.CancellationToken);
        var approvePermission = await context.Permissions.SingleAsync(p => p.Name == Permissions.OperationalRestrictionApprove, TestContext.Current.CancellationToken);
        var grant = await context.RolePermissions.SingleAsync(rp => rp.RoleId == dutyOperationsManagerRole.Id && rp.PermissionId == approvePermission.Id, TestContext.Current.CancellationToken);
        context.RolePermissions.Remove(grant);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        Assert.False(await service.UserHasPermissionAsync(DbSeeder.AnnaId, Permissions.OperationalRestrictionApprove));
        // The other permissions of the role are unaffected.
        Assert.True(await service.UserHasPermissionAsync(DbSeeder.AnnaId, Permissions.OperationalRestrictionRead));
    }

    private static PermissionService CreateService() =>
        new(TestDatabase.CreateSeededContext());
}
