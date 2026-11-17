using Backend.Data;

using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

// Resolves permissions purely from the database: user -> user-role
// assignments -> roles -> role-permissions -> permissions. Changing which
// permissions a role has, or which roles a user has, is a data change only.
public class PermissionService : IPermissionService
{
    private readonly AppDbContext _dbContext;

    public PermissionService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> UserHasPermissionAsync(Guid userId, string permission)
    {
        var normalized = permission.Trim();

        return await (
            from assignment in _dbContext.UserRoleAssignments
            join rolePermission in _dbContext.RolePermissions
                on assignment.RoleId equals rolePermission.RoleId
            join permissionRow in _dbContext.Permissions
                on rolePermission.PermissionId equals permissionRow.Id
            where assignment.UserId == userId
                && permissionRow.Name == normalized
            select permissionRow.Id
        ).AnyAsync();
    }

    public async Task<IReadOnlyCollection<string>> GetPermissionsForUserAsync(Guid userId)
    {
        var names = await (
            from assignment in _dbContext.UserRoleAssignments
            join rolePermission in _dbContext.RolePermissions
                on assignment.RoleId equals rolePermission.RoleId
            join permissionRow in _dbContext.Permissions
                on rolePermission.PermissionId equals permissionRow.Id
            where assignment.UserId == userId
            select permissionRow.Name
        ).Distinct().ToListAsync();

        return names;
    }
}
