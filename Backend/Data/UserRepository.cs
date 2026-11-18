using Backend.Models.Entities;

using Microsoft.EntityFrameworkCore;

namespace Backend.Data;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<bool> EmailExistsAsync(string email)
    {
        return _context.Users.AnyAsync(u => u.Email == email);
    }

    public Task<User?> GetByEmailAsync(string email)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        return _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == normalizedEmail);
    }

    public Task<User?> GetByIdAsync(Guid id)
    {
        return _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task AddAsync(User user)
    {
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(User user)
    {
        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> SetPasswordIfUnsetAsync(Guid userId, string passwordHash)
    {
        var affectedRows = await _context.Users
            .Where(u => u.Id == userId && u.PasswordHash == string.Empty)
            .ExecuteUpdateAsync(setters => setters.SetProperty(u => u.PasswordHash, passwordHash));

        return affectedRows == 1;
    }
}
