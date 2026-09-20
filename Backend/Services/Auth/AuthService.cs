using Backend.Models.DTOs;
using Backend.Models.Entities;

namespace Backend.Services.Auth;

public class AuthService
{
    private readonly PasswordHasher _passwordHasher;
    private readonly JwtService _jwtService;
    private readonly User _placeholderUser;

    public AuthService(PasswordHasher passwordHasher, JwtService jwtService)
    {
        _passwordHasher = passwordHasher;
        _jwtService = jwtService;
        //Laver en placeholder user mens der ikke er en databse
        _placeholderUser = new User
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
            Name = "Placeholder User",
            Email = "placeholder@example.com",
            PasswordHash = _passwordHasher.Hash("Password123!")
        };
    }

    public LoginResult Login(LoginRequestDto request)
    {
        if (!string.Equals(request.Email.ToLower(), _placeholderUser.Email.ToLower()))
        {
            return LoginResult.Failure();
        }

        
        if (!_passwordHasher.Verify(_placeholderUser.PasswordHash, request.Password))
        {
            return LoginResult.Failure();
        }

        var token = _jwtService.Mint(_placeholderUser);

        return LoginResult.Success(new LoginResponseDto
        {
            AccessToken = token.AccessToken,
            ExpiresAtUtc = token.ExpiresAtUtc,
            User = MapToDto(_placeholderUser)
        });
    }

    
    // Midlertidigt mens vi ikke endnu har fået implementeret automapper
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
