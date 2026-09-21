using System.IdentityModel.Tokens.Jwt;
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


    // Validates the token the same way SimpleForge's AuthService.CheckValidityOfToken
    // does: run it through JwtService's real validation, then resolve the identity
    // it names. There's no database yet, so "resolving the identity" just means
    // checking it points at the one hardcoded placeholder user.
    public UserDto? CheckValidityOfToken(string token)
    {
        var principal = _jwtService.ValidateToken(token);
        if (principal is null)
        {
            return null;
        }

        var subject = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (!Guid.TryParse(subject, out var userId) || userId != _placeholderUser.Id)
        {
            return null;
        }

        return MapToDto(_placeholderUser);
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
