using System.IdentityModel.Tokens.Jwt;

using Backend.Data;
using Backend.Models.DTOs;
using Backend.Models.Entities;

using Microsoft.EntityFrameworkCore;

namespace Backend.Services.Auth;

public class AuthService
{
    private readonly AppDbContext _db;
    private readonly PasswordHasher _passwordHasher;
    private readonly JwtService _jwtService;

    public AuthService(
        AppDbContext db,
        PasswordHasher passwordHasher,
        JwtService jwtService)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _jwtService = jwtService;
    }

    public async Task<LoginResult> LoginAsync(
        LoginRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.SingleOrDefaultAsync(
            user => user.Email == email,
            cancellationToken);

        if (user is null || !_passwordHasher.Verify(user.PasswordHash, request.Password))
        {
            return LoginResult.Failure();
        }

        var token = _jwtService.Mint(user);

        return LoginResult.Success(new LoginResponseDto
        {
            AccessToken = token.AccessToken,
            ExpiresAtUtc = token.ExpiresAtUtc,
            User = MapToDto(user)
        });
    }

    public async Task<UserDto?> CheckValidityOfTokenAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        var principal = _jwtService.ValidateToken(token);
        if (principal is null)
        {
            return null;
        }

        var subject = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (!Guid.TryParse(subject, out var userId))
        {
            return null;
        }

        var user = await _db.Users.SingleOrDefaultAsync(
            user => user.Id == userId,
            cancellationToken);

        return user is null ? null : MapToDto(user);
    }

    private static UserDto MapToDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email
        };
    }

}
