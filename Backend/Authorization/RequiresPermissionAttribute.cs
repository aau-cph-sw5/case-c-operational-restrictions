using System.IdentityModel.Tokens.Jwt;

using Backend.Services;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Backend.Authorization;

// Authorization filter for controller actions. The user must be
// authenticated and have at least one role (resolved from the database)
// that grants the named permission; otherwise the request is rejected.
// Multiple attributes combine with AND semantics.
//   401 when there is no authenticated user, 403 when the permission is missing.
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class RequiresPermissionAttribute : Attribute, IAsyncAuthorizationFilter
{
    public RequiresPermissionAttribute(string permission)
    {
        Permission = permission;
    }

    public string Permission { get; }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (context.HttpContext.User.Identity?.IsAuthenticated != true)
        {
            context.Result = new ChallengeResult();
            return;
        }

        var subject = context.HttpContext.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (!Guid.TryParse(subject, out var userId))
        {
            context.Result = new ForbidResult();
            return;
        }

        var permissionService = context.HttpContext.RequestServices.GetRequiredService<IPermissionService>();
        if (!await permissionService.UserHasPermissionAsync(userId, Permission))
        {
            context.Result = new ForbidResult();
        }
    }
}
