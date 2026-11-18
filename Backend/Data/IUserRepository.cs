using Backend.Models.Entities;

namespace Backend.Data;

public interface IUserRepository
{
    Task<bool> EmailExistsAsync(string email);
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByIdAsync(Guid id);
    Task AddAsync(User user);
    Task AddWithRoleAsync(User user, string roleName);
    Task DeleteAsync(User user);
    Task<bool> SetPasswordIfUnsetAsync(Guid userId, string passwordHash);
}
