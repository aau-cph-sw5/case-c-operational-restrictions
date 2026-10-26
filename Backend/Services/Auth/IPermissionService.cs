namespace Backend.Services.Auth;

public interface IPermissionService
{
    Task<bool> UserHasPermissionAsync(Guid userId, string permission);
}
