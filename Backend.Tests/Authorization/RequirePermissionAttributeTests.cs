using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

using Backend.Authorization;
using Backend.Services.Auth;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.Tests.Authorization;

// RequirePermissionAttribute is exercised directly through the authorization
// filter pipeline: the user comes from the sub claim on ClaimsPrincipal and the
// permission check goes through IPermissionService, which is faked here so no
// database is involved.
public class RequirePermissionAttributeTests
{
    private sealed class FakePermissionService : IPermissionService
    {
        private readonly Func<Guid, string, bool> _rule;

        public FakePermissionService(Func<Guid, string, bool> rule)
        {
            _rule = rule;
        }

        public Guid? LastUserId { get; private set; }

        public string? LastPermission { get; private set; }

        public Task<bool> UserHasPermissionAsync(Guid userId, string permission)
        {
            LastUserId = userId;
            LastPermission = permission;
            return Task.FromResult(_rule(userId, permission));
        }
    }

    private static AuthorizationFilterContext CreateContext(
        ClaimsPrincipal? user,
        IPermissionService? permissionService)
    {
        var services = new ServiceCollection();
        if (permissionService is not null)
        {
            services.AddSingleton(permissionService);
        }

        var httpContext = new DefaultHttpContext
        {
            User = user ?? new ClaimsPrincipal(new ClaimsIdentity()),
            RequestServices = services.BuildServiceProvider(),
        };

        return new AuthorizationFilterContext(
            new ActionContext(httpContext, new RouteData(), new ActionDescriptor()),
            Array.Empty<IFilterMetadata>());
    }

    private static ClaimsPrincipal UserWithSubject(string? subject)
    {
        var claims = subject is null
            ? Array.Empty<Claim>()
            : new[] { new Claim(JwtRegisteredClaimNames.Sub, subject) };

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
    }

    [Fact]
    public async Task UserWithPermissionContinues()
    {
        var userId = Guid.NewGuid();
        var permissionService = new FakePermissionService((_, _) => true);
        var context = CreateContext(UserWithSubject(userId.ToString()), permissionService);
        var attribute = new RequirePermissionAttribute("OperationalRestriction:read");

        await attribute.OnAuthorizationAsync(context);

        Assert.Null(context.Result);
        Assert.Equal(userId, permissionService.LastUserId);
        Assert.Equal("OperationalRestriction:read", permissionService.LastPermission);
    }

    [Fact]
    public async Task UserWithoutPermissionGets403()
    {
        var permissionService = new FakePermissionService((_, _) => false);
        var context = CreateContext(UserWithSubject(Guid.NewGuid().ToString()), permissionService);
        var attribute = new RequirePermissionAttribute("OperationalRestriction:read");

        await attribute.OnAuthorizationAsync(context);

        var result = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, result.StatusCode);
    }

    [Fact]
    public async Task UserWithoutSubClaimGets401()
    {
        var permissionService = new FakePermissionService((_, _) => true);
        var context = CreateContext(UserWithSubject(null), permissionService);
        var attribute = new RequirePermissionAttribute("OperationalRestriction:read");

        await attribute.OnAuthorizationAsync(context);

        Assert.IsType<UnauthorizedObjectResult>(context.Result);
        Assert.Null(permissionService.LastUserId);
    }

    [Theory]
    [InlineData("not-a-guid")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public async Task UserWithInvalidSubClaimGets401(string subject)
    {
        var permissionService = new FakePermissionService((_, _) => true);
        var context = CreateContext(UserWithSubject(subject), permissionService);
        var attribute = new RequirePermissionAttribute("OperationalRestriction:read");

        await attribute.OnAuthorizationAsync(context);

        Assert.IsType<UnauthorizedObjectResult>(context.Result);
        Assert.Null(permissionService.LastUserId);
    }

    [Fact]
    public async Task MissingPermissionServiceThrows()
    {
        var context = CreateContext(UserWithSubject(Guid.NewGuid().ToString()), permissionService: null);
        var attribute = new RequirePermissionAttribute("OperationalRestriction:read");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => attribute.OnAuthorizationAsync(context));
    }
}
