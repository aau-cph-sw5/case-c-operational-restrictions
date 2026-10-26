using Backend.Data;

using Microsoft.EntityFrameworkCore;

namespace Backend.Services.Auth;

// Checks permissions through the user -> role -> permission connection:
// UserRoleAssignment links a user to a role, RolePermissionAssignment links that
// role to a permission. Used by RequirePermissionAttribute on controller endpoints.
public class PermissionService : IPermissionService
{
    private readonly AppDbContext _context;

    public PermissionService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<bool> UserHasPermissionAsync(Guid userId, string permission)
    {
        return await _context.Users
            .AsNoTracking()
            .AnyAsync(user =>
                user.Id == userId &&
                user.RoleAssignments.Any(roleAssignment =>
                    roleAssignment.Role.PermissionAssignments.Any(
                        permissionAssignment => permissionAssignment.PermissionName == permission)));
    }
}
