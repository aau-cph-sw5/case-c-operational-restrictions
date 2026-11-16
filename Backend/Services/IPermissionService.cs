namespace Backend.Services;

public interface IPermissionService
{
    // True when the user has at least one role that grants the permission.
    Task<bool> UserHasPermissionAsync(Guid userId, string permission);

    // All distinct permission names granted to the user through their roles.
    Task<IReadOnlyCollection<string>> GetPermissionsForUserAsync(Guid userId);
}
