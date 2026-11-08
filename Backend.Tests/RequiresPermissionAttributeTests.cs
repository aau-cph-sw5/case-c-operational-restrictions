using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

using Backend.Authorization;
using Backend.Data;
using Backend.Models.Entities;
using Backend.Services;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.Tests;

// Unit tests for RequiresPermissionAttribute. The filter is invoked directly
// with a hand-built AuthorizationFilterContext, both against a stubbed
// IPermissionService and against the real PermissionService backed by an
// in-memory database.
public class RequiresPermissionAttributeTests
{
    private const string ReadPermission = Permissions.OperationalRestrictionRead;
    private static readonly string[] ReadOnly = [ReadPermission];
    private static readonly string[] NoPermissions = [];
    private static readonly string[] OtherPermission = ["Other:Permission"];

    [Fact]
    public async Task AllowsRequestWhenServiceGrantsPermission()
    {
        var userId = Guid.NewGuid();
        var stub = new StubPermissionService(hasPermission: true);
        var context = CreateContext(AuthenticatedUser(userId), stub);

        await new RequiresPermissionAttribute(ReadPermission).OnAuthorizationAsync(context);

        Assert.Null(context.Result);
        Assert.Equal(userId, stub.SeenUserId);
        Assert.Equal(ReadPermission, stub.SeenPermission);
    }

    [Fact]
    public async Task ForbidsWhenServiceDeniesPermission()
    {
        var context = CreateContext(AuthenticatedUser(Guid.NewGuid()), new StubPermissionService(hasPermission: false));

        await new RequiresPermissionAttribute(ReadPermission).OnAuthorizationAsync(context);

        Assert.IsType<ForbidResult>(context.Result);
    }

    [Fact]
    public async Task ChallengesWhenUserIsNotAuthenticated()
    {
        var anonymous = new ClaimsPrincipal(new ClaimsIdentity()); // no authentication type
        var context = CreateContext(anonymous, new StubPermissionService(hasPermission: true));

        await new RequiresPermissionAttribute(ReadPermission).OnAuthorizationAsync(context);

        Assert.IsType<ChallengeResult>(context.Result);
    }

    [Fact]
    public async Task ForbidsWhenAuthenticatedUserHasNoSubjectClaim()
    {
        var noSubject = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Name, "no-subject")], authenticationType: "Test"));
        var context = CreateContext(noSubject, new StubPermissionService(hasPermission: true));

        await new RequiresPermissionAttribute(ReadPermission).OnAuthorizationAsync(context);

        Assert.IsType<ForbidResult>(context.Result);
    }

    [Fact]
    public async Task AllowsWhenAnyOneOfSeveralRolesGrantsThePermission()
    {
        await using var dbContext = CreateDbContext();
        var userId = await SeedUserWithRolesAsync(dbContext, ("RoleWithoutRead", NoPermissions), ("RoleWithRead", ReadOnly));

        var context = CreateContext(AuthenticatedUser(userId), new PermissionService(dbContext));

        await new RequiresPermissionAttribute(ReadPermission).OnAuthorizationAsync(context);

        Assert.Null(context.Result);
    }

    [Fact]
    public async Task ForbidsWhenNoRoleGrantsThePermission()
    {
        await using var dbContext = CreateDbContext();
        var userId = await SeedUserWithRolesAsync(dbContext, ("SomeRole", OtherPermission));

        var context = CreateContext(AuthenticatedUser(userId), new PermissionService(dbContext));

        await new RequiresPermissionAttribute(ReadPermission).OnAuthorizationAsync(context);

        Assert.IsType<ForbidResult>(context.Result);
    }

    [Fact]
    public async Task ForbidsWhenUserHasNoRolesAtAll()
    {
        await using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        dbContext.Users.Add(NewUser(userId));
        await dbContext.SaveChangesAsync();

        var context = CreateContext(AuthenticatedUser(userId), new PermissionService(dbContext));

        await new RequiresPermissionAttribute(ReadPermission).OnAuthorizationAsync(context);

        Assert.IsType<ForbidResult>(context.Result);
    }

    [Fact]
    public async Task GrantingOrRevokingARolePermissionInTheDatabaseChangesTheOutcomeWithoutCodeChanges()
    {
        await using var dbContext = CreateDbContext();
        var (userId, roleId, permissionId) = await SeedUserWithSingleRoleAndPermissionAsync(dbContext, ReadPermission);
        var attribute = new RequiresPermissionAttribute(ReadPermission);

        var allowedContext = CreateContext(AuthenticatedUser(userId), new PermissionService(dbContext));
        await attribute.OnAuthorizationAsync(allowedContext);
        Assert.Null(allowedContext.Result);

        // Revoke the permission from the role: only a database row changes.
        dbContext.RolePermissions.Remove(
            await dbContext.RolePermissions.SingleAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId));
        await dbContext.SaveChangesAsync();

        var deniedContext = CreateContext(AuthenticatedUser(userId), new PermissionService(dbContext));
        await attribute.OnAuthorizationAsync(deniedContext);
        Assert.IsType<ForbidResult>(deniedContext.Result);

        // Grant it again: allowed once more.
        dbContext.RolePermissions.Add(new RolePermission { RoleId = roleId, PermissionId = permissionId });
        await dbContext.SaveChangesAsync();

        var allowedAgainContext = CreateContext(AuthenticatedUser(userId), new PermissionService(dbContext));
        await attribute.OnAuthorizationAsync(allowedAgainContext);
        Assert.Null(allowedAgainContext.Result);
    }

    private static AuthorizationFilterContext CreateContext(ClaimsPrincipal user, IPermissionService permissionService)
    {
        var services = new ServiceCollection()
            .AddSingleton(permissionService)
            .BuildServiceProvider();

        var httpContext = new DefaultHttpContext
        {
            User = user,
            RequestServices = services
        };

        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        return new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());
    }

    private static ClaimsPrincipal AuthenticatedUser(Guid userId) =>
        new(new ClaimsIdentity(
            [new Claim(JwtRegisteredClaimNames.Sub, userId.ToString())],
            authenticationType: "Test"));

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

    private static async Task<Guid> SeedUserWithRolesAsync(
        AppDbContext dbContext,
        params (string RoleName, string[] PermissionNames)[] roles)
    {
        var userId = Guid.NewGuid();
        dbContext.Users.Add(NewUser(userId));

        foreach (var (roleName, permissionNames) in roles)
        {
            var role = new Role { Id = Guid.NewGuid(), Name = roleName };
            dbContext.Roles.Add(role);
            dbContext.UserRoleAssignments.Add(new UserRoleAssignment { UserId = userId, RoleId = role.Id });

            foreach (var permissionName in permissionNames)
            {
                var permission = new Permission { Id = Guid.NewGuid(), Name = permissionName };
                dbContext.Permissions.Add(permission);
                dbContext.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permission.Id });
            }
        }

        await dbContext.SaveChangesAsync();
        return userId;
    }

    private static async Task<(Guid UserId, Guid RoleId, Guid PermissionId)> SeedUserWithSingleRoleAndPermissionAsync(
        AppDbContext dbContext, string permissionName)
    {
        var userId = Guid.NewGuid();
        var role = new Role { Id = Guid.NewGuid(), Name = "Role" };
        var permission = new Permission { Id = Guid.NewGuid(), Name = permissionName };

        dbContext.Users.Add(NewUser(userId));
        dbContext.Roles.Add(role);
        dbContext.Permissions.Add(permission);
        dbContext.UserRoleAssignments.Add(new UserRoleAssignment { UserId = userId, RoleId = role.Id });
        dbContext.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permission.Id });
        await dbContext.SaveChangesAsync();

        return (userId, role.Id, permission.Id);
    }

    private sealed class StubPermissionService : IPermissionService
    {
        private readonly bool _hasPermission;

        public StubPermissionService(bool hasPermission)
        {
            _hasPermission = hasPermission;
        }

        public Guid? SeenUserId { get; private set; }
        public string? SeenPermission { get; private set; }

        public Task<bool> UserHasPermissionAsync(Guid userId, string permission)
        {
            SeenUserId = userId;
            SeenPermission = permission;
            return Task.FromResult(_hasPermission);
        }

        public Task<IReadOnlyCollection<string>> GetPermissionsForUserAsync(Guid userId) =>
            Task.FromResult<IReadOnlyCollection<string>>(Array.Empty<string>());
    }
}
