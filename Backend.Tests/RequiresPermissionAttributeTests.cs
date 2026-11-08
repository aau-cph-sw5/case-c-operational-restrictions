using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

using Backend.Authorization;
using Backend.Services;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.Tests;

// Unit tests for the RequiresPermission authorization filter. The filter is
// driven directly with a fake IPermissionService, so these tests isolate the
// filter's own decisions: 401 when unauthenticated, 403 when the subject
// claim is unusable or the permission is missing, and no result (request
// proceeds) when the permission is granted.
public class RequiresPermissionAttributeTests
{
    private const string TestPermission = Permissions.OperationalRestrictionApprove;

    [Fact]
    public async Task UnauthenticatedUserGetsChallenge()
    {
        var context = CreateContext(new ClaimsPrincipal(new ClaimsIdentity()), new FakePermissionService(TestPermission));

        await new RequiresPermissionAttribute(TestPermission).OnAuthorizationAsync(context);

        Assert.IsType<ChallengeResult>(context.Result);
    }

    [Fact]
    public async Task AuthenticatedUserWithoutSubjectClaimGetsForbid()
    {
        var context = CreateContext(AuthenticatedWithoutSubject(), new FakePermissionService(TestPermission));

        await new RequiresPermissionAttribute(TestPermission).OnAuthorizationAsync(context);

        Assert.IsType<ForbidResult>(context.Result);
    }

    [Fact]
    public async Task AuthenticatedUserWithNonGuidSubjectGetsForbid()
    {
        var context = CreateContext(AuthenticatedWithSubject("not-a-guid"), new FakePermissionService(TestPermission));

        await new RequiresPermissionAttribute(TestPermission).OnAuthorizationAsync(context);

        Assert.IsType<ForbidResult>(context.Result);
    }

    [Fact]
    public async Task UserWithPermissionIsAllowedAndPermissionIsPassedThroughVerbatim()
    {
        var userId = Guid.NewGuid();
        var permissionService = new FakePermissionService(TestPermission);
        var context = CreateContext(AuthenticatedWithSubject(userId.ToString()), permissionService);

        await new RequiresPermissionAttribute(TestPermission).OnAuthorizationAsync(context);

        Assert.Null(context.Result);
        Assert.Equal(userId, permissionService.LastUserId);
        Assert.Equal(TestPermission, permissionService.LastRequestedPermission);
    }

    [Fact]
    public async Task UserWithoutPermissionGetsForbid()
    {
        var context = CreateContext(
            AuthenticatedWithSubject(Guid.NewGuid().ToString()),
            new FakePermissionService(Permissions.OperationalRestrictionRead));

        await new RequiresPermissionAttribute(TestPermission).OnAuthorizationAsync(context);

        Assert.IsType<ForbidResult>(context.Result);
    }

    private static ClaimsPrincipal AuthenticatedWithoutSubject() =>
        new(new ClaimsIdentity([], "Test"));

    private static ClaimsPrincipal AuthenticatedWithSubject(string subject) =>
        new(new ClaimsIdentity([new Claim(JwtRegisteredClaimNames.Sub, subject)], "Test"));

    private static AuthorizationFilterContext CreateContext(ClaimsPrincipal user, IPermissionService permissionService)
    {
        var services = new ServiceCollection();
        services.AddSingleton(permissionService);

        var httpContext = new DefaultHttpContext
        {
            User = user,
            RequestServices = services.BuildServiceProvider()
        };
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

        return new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());
    }

    private sealed class FakePermissionService : IPermissionService
    {
        private readonly HashSet<string> _granted;

        public FakePermissionService(params string[] granted) => _granted = new HashSet<string>(granted);

        public Guid? LastUserId { get; private set; }

        public string? LastRequestedPermission { get; private set; }

        public Task<bool> UserHasPermissionAsync(Guid userId, string permission)
        {
            LastUserId = userId;
            LastRequestedPermission = permission;
            return Task.FromResult(_granted.Contains(permission));
        }

        public Task<IReadOnlyCollection<string>> GetPermissionsForUserAsync(Guid userId) =>
            Task.FromResult<IReadOnlyCollection<string>>(_granted.ToList());
    }
}
