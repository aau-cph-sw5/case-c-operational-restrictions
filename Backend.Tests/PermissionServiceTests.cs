using Backend.Authorization;
using Backend.Data;
using Backend.Models.Entities;
using Backend.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Backend.Tests;

// Tests that PermissionService resolves permissions purely from database
// rows: user-role assignments, roles, role-permission grants and permissions.
public class PermissionServiceTests
{
    private static readonly string[] ExpectedUnion = ["A:One", "A:Two", "B:Three"];

    [Fact]
    public async Task UserHasPermissionIsTrueWhenAnyOneOfSeveralRolesGrantsIt()
    {
        await using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        var roleWithout = await AddRoleAsync(dbContext, "RoleWithout", "Other:Permission");
        var roleWith = await AddRoleAsync(dbContext, "RoleWith", Permissions.OperationalRestrictionRead);
        Assign(dbContext, userId, roleWithout, roleWith);
        await dbContext.SaveChangesAsync();

        var service = new PermissionService(dbContext);

        Assert.True(await service.UserHasPermissionAsync(userId, Permissions.OperationalRestrictionRead));
    }

    [Fact]
    public async Task UserHasPermissionIsFalseWhenNoRoleGrantsIt()
    {
        await using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        var role = await AddRoleAsync(dbContext, "Role", "Other:Permission");
        Assign(dbContext, userId, role);
        await dbContext.SaveChangesAsync();

        var service = new PermissionService(dbContext);

        Assert.False(await service.UserHasPermissionAsync(userId, Permissions.OperationalRestrictionRead));
    }

    [Fact]
    public async Task UserHasPermissionIsFalseWhenUserHasNoRoles()
    {
        await using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        dbContext.Users.Add(NewUser(userId));
        await dbContext.SaveChangesAsync();

        var service = new PermissionService(dbContext);

        Assert.False(await service.UserHasPermissionAsync(userId, Permissions.OperationalRestrictionRead));
    }

    [Fact]
    public async Task UserHasPermissionIsFalseForUnknownPermission()
    {
        await using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        var role = await AddRoleAsync(dbContext, "Role", Permissions.OperationalRestrictionRead);
        Assign(dbContext, userId, role);
        await dbContext.SaveChangesAsync();

        var service = new PermissionService(dbContext);

        Assert.False(await service.UserHasPermissionAsync(userId, "Does:NotExist"));
    }

    [Fact]
    public async Task PermissionNameIsTrimmedBeforeLookup()
    {
        await using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        var role = await AddRoleAsync(dbContext, "Role", Permissions.OperationalRestrictionRead);
        Assign(dbContext, userId, role);
        await dbContext.SaveChangesAsync();

        var service = new PermissionService(dbContext);

        Assert.True(await service.UserHasPermissionAsync(userId, $"  {Permissions.OperationalRestrictionRead}  "));
    }

    [Fact]
    public async Task RemovingARolePermissionGrantInTheDatabaseRevokesAccessWithoutCodeChanges()
    {
        await using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        var (role, permission) = await AddRoleWithPermissionAsync(dbContext, "Role", Permissions.OperationalRestrictionApprove);
        Assign(dbContext, userId, role);
        await dbContext.SaveChangesAsync();
        var service = new PermissionService(dbContext);

        Assert.True(await service.UserHasPermissionAsync(userId, Permissions.OperationalRestrictionApprove));

        dbContext.RolePermissions.Remove(
            await dbContext.RolePermissions.SingleAsync(rp => rp.RoleId == role.Id && rp.PermissionId == permission.Id));
        await dbContext.SaveChangesAsync();

        Assert.False(await service.UserHasPermissionAsync(userId, Permissions.OperationalRestrictionApprove));
    }

    [Fact]
    public async Task AssigningANewRoleInTheDatabaseGrantsAccessWithoutCodeChanges()
    {
        await using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        var plainRole = await AddRoleAsync(dbContext, "PlainRole", "Other:Permission");
        var approvingRole = await AddRoleAsync(dbContext, "ApprovingRole", Permissions.OperationalRestrictionApprove);
        Assign(dbContext, userId, plainRole);
        await dbContext.SaveChangesAsync();
        var service = new PermissionService(dbContext);

        Assert.False(await service.UserHasPermissionAsync(userId, Permissions.OperationalRestrictionApprove));

        dbContext.UserRoleAssignments.Add(new UserRoleAssignment { UserId = userId, RoleId = approvingRole.Id });
        await dbContext.SaveChangesAsync();

        Assert.True(await service.UserHasPermissionAsync(userId, Permissions.OperationalRestrictionApprove));
    }

    [Fact]
    public async Task GetPermissionsForUserReturnsDistinctUnionAcrossAllRoles()
    {
        await using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        var roleA = await AddRoleAsync(dbContext, "RoleA", "A:One", "A:Two");
        var roleB = await AddRoleAsync(dbContext, "RoleB", "A:Two", "B:Three");
        Assign(dbContext, userId, roleA, roleB);
        await dbContext.SaveChangesAsync();

        var service = new PermissionService(dbContext);
        var permissions = await service.GetPermissionsForUserAsync(userId);

        Assert.Equal(
            ExpectedUnion.OrderBy(x => x),
            permissions.OrderBy(x => x));
    }

    [Fact]
    public async Task SeededDatabaseGrantsExpectedPermissionsPerRole()
    {
        await using var dbContext = TestDatabase.CreateSeededContext();
        var service = new PermissionService(dbContext);

        // Anna is a DutyOperationsManager: can read, create, sign and approve.
        Assert.True(await service.UserHasPermissionAsync(DbSeeder.AnnaId, Permissions.OperationalRestrictionRead));
        Assert.True(await service.UserHasPermissionAsync(DbSeeder.AnnaId, Permissions.OperationalRestrictionCreate));
        Assert.True(await service.UserHasPermissionAsync(DbSeeder.AnnaId, Permissions.OperationalRestrictionSign));
        Assert.True(await service.UserHasPermissionAsync(DbSeeder.AnnaId, Permissions.OperationalRestrictionApprove));

        // Lars is an Operator: can read, create and sign, but not approve.
        Assert.True(await service.UserHasPermissionAsync(DbSeeder.LarsId, Permissions.OperationalRestrictionRead));
        Assert.False(await service.UserHasPermissionAsync(DbSeeder.LarsId, Permissions.OperationalRestrictionApprove));

        // Every seeded user can read.
        foreach (var user in await dbContext.Users.ToListAsync())
            Assert.True(await service.UserHasPermissionAsync(user.Id, Permissions.OperationalRestrictionRead));
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new AppDbContext(options);
    }

    private static User NewUser(Guid userId) => new()
    {
        Id = userId,
        Name = "Test User",
        Email = $"{userId}@example.com",
        PasswordHash = "hash"
    };

    private static async Task<Role> AddRoleAsync(AppDbContext dbContext, string roleName, params string[] permissionNames)
    {
        var role = new Role { Id = Guid.NewGuid(), Name = roleName };
        dbContext.Roles.Add(role);
        foreach (var permissionName in permissionNames)
        {
            var permission = new Permission { Id = Guid.NewGuid(), Name = permissionName };
            dbContext.Permissions.Add(permission);
            dbContext.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permission.Id });
        }
        await dbContext.SaveChangesAsync();
        return role;
    }

    private static async Task<(Role Role, Permission Permission)> AddRoleWithPermissionAsync(
        AppDbContext dbContext, string roleName, string permissionName)
    {
        var role = new Role { Id = Guid.NewGuid(), Name = roleName };
        var permission = new Permission { Id = Guid.NewGuid(), Name = permissionName };
        dbContext.Roles.Add(role);
        dbContext.Permissions.Add(permission);
        dbContext.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permission.Id });
        await dbContext.SaveChangesAsync();
        return (role, permission);
    }

    private static void Assign(AppDbContext dbContext, Guid userId, params Role[] roles)
    {
        dbContext.Users.Add(NewUser(userId));
        foreach (var role in roles)
            dbContext.UserRoleAssignments.Add(new UserRoleAssignment { UserId = userId, RoleId = role.Id });
    }
}
