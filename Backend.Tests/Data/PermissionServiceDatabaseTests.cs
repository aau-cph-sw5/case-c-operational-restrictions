using Backend.Authorization;
using Backend.Data;
using Backend.Models.Entities;
using Backend.Services;

using DatabaseLayer.Tests;

using Microsoft.EntityFrameworkCore;

namespace Backend.Tests.Data;

// Runs the RBAC resolution against a real PostgreSQL database in a
// Testcontainers container: the seeder creates the roles, permissions and
// assignments, and PermissionService resolves them with real SQL joins.
public class PermissionServiceDatabaseTests(
    TestDatabaseFixture fixture) : IClassFixture<TestDatabaseFixture>
{
    [Fact]
    public async Task SeededUsersResolvePermissionsThroughTheirRoles()
    {
        await using var context = fixture.CreateContext();
        await DbSeeder.SeedAsync(context);
        var service = new PermissionService(context);

        // Anna is seeded with DutyOperationsManager, which grants every permission.
        Assert.True(await service.UserHasPermissionAsync(DbSeeder.AnnaId, Permissions.OperationalRestrictionApprove));
        Assert.True(await service.UserHasPermissionAsync(DbSeeder.AnnaId, Permissions.OperationalRestrictionRead));

        // Lars is seeded with Operator, which does not grant approve.
        Assert.True(await service.UserHasPermissionAsync(DbSeeder.LarsId, Permissions.OperationalRestrictionRead));
        Assert.False(await service.UserHasPermissionAsync(DbSeeder.LarsId, Permissions.OperationalRestrictionApprove));

        // A user id with no assignments has nothing.
        Assert.False(await service.UserHasPermissionAsync(Guid.NewGuid(), Permissions.OperationalRestrictionRead));
    }

    [Fact]
    public async Task GetPermissionsForUserAsyncReturnsDistinctGrantedNames()
    {
        await using var context = fixture.CreateContext();
        await DbSeeder.SeedAsync(context);
        var service = new PermissionService(context);

        var permissions = await service.GetPermissionsForUserAsync(DbSeeder.AnnaId);

        Assert.Equal(4, permissions.Count);
        Assert.Contains(Permissions.OperationalRestrictionApprove, permissions);
    }

    [Fact]
    public async Task GrantingAPermissionToARoleInTheDatabaseChangesAccessWithoutCodeChanges()
    {
        await using var context = fixture.CreateContext();
        await DbSeeder.SeedAsync(context);
        var service = new PermissionService(context);

        // A fresh user with a fresh role (no grants yet) cannot approve. The test uses its
        // own role so the shared seeded roles stay untouched for the other tests in this class.
        var user = new User { Id = Guid.NewGuid(), Name = "Temp User", Email = $"{Guid.NewGuid()}@example.test" };
        var tempRole = new Role { Id = Guid.NewGuid(), Name = $"TempRole-{Guid.NewGuid():N}" };
        context.Users.Add(user);
        context.Roles.Add(tempRole);
        context.UserRoleAssignments.Add(new UserRoleAssignment { UserId = user.Id, RoleId = tempRole.Id });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        Assert.False(await service.UserHasPermissionAsync(user.Id, Permissions.OperationalRestrictionApprove));

        // Granting the permission to the role as a data change is enough.
        var approvePermission = await context.Permissions.SingleAsync(p => p.Name == Permissions.OperationalRestrictionApprove, TestContext.Current.CancellationToken);
        context.RolePermissions.Add(new RolePermission { RoleId = tempRole.Id, PermissionId = approvePermission.Id });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        Assert.True(await service.UserHasPermissionAsync(user.Id, Permissions.OperationalRestrictionApprove));
    }

    [Fact]
    public async Task AssigningARoleToAUserInTheDatabaseGrantsItsPermissions()
    {
        await using var context = fixture.CreateContext();
        await DbSeeder.SeedAsync(context);
        var service = new PermissionService(context);

        var user = new User { Id = Guid.NewGuid(), Name = "Temp User", Email = $"{Guid.NewGuid()}@example.test" };
        context.Users.Add(user);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        Assert.False(await service.UserHasPermissionAsync(user.Id, Permissions.OperationalRestrictionRead));

        var dutyOperationsManagerRole = await context.Roles.SingleAsync(r => r.Name == RoleNames.DutyOperationsManager, TestContext.Current.CancellationToken);
        context.UserRoleAssignments.Add(new UserRoleAssignment { UserId = user.Id, RoleId = dutyOperationsManagerRole.Id });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        Assert.True(await service.UserHasPermissionAsync(user.Id, Permissions.OperationalRestrictionRead));
        Assert.True(await service.UserHasPermissionAsync(user.Id, Permissions.OperationalRestrictionApprove));
    }
}
