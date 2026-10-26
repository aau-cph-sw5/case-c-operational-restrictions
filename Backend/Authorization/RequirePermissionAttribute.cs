using System.IdentityModel.Tokens.Jwt;

using Backend.Services.Auth;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Backend.Authorization;

// Usage: [RequirePermission("OperationalRestriction:read")] on a controller endpoint.
// Reads the current user the same way ApiControllerBase.CurrentUserId does (the sub
// claim), then checks the permission through the user -> role -> permission
// connection in the database. Returns 401 when there is no logged-in user and 403
// when the user does not have the permission.
public class RequirePermissionAttribute : Attribute, IAsyncAuthorizationFilter
{
    private readonly string _permission;

    public RequirePermissionAttribute(string permission)
    {
        if (string.IsNullOrWhiteSpace(permission))
        {
            throw new ArgumentException("Permission name must not be empty.", nameof(permission));
        }

        _permission = permission;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var subject = context.HttpContext.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (!Guid.TryParse(subject, out var userId) || userId == Guid.Empty)
        {
            context.Result = new UnauthorizedObjectResult(new { error = "A valid logged-in user is required." });
            return;
        }

        var permissionService = context.HttpContext.RequestServices.GetRequiredService<IPermissionService>();
        var hasPermission = await permissionService.UserHasPermissionAsync(userId, _permission);

        if (!hasPermission)
        {
            context.Result = new ObjectResult(new { error = $"The required permission '{_permission}' is missing." })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }
    }
}
