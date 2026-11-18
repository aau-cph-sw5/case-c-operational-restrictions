using System.IdentityModel.Tokens.Jwt;

using Backend.Configuration;
using Backend.Data;
using Backend.Models.DTOs;
using Backend.Models.Entities;
using Backend.Services.Auth;
using Backend.Services.Email;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Backend.Services.Users;

public class UserInvitationService : IUserInvitationService
{
    private readonly IUserRepository _userRepository;
    private readonly JwtService _jwtService;
    private readonly PasswordHasher _passwordHasher;
    private readonly IEmailService _emailService;
    private readonly InviteOptions _inviteOptions;

    public UserInvitationService(
        IUserRepository userRepository,
        JwtService jwtService,
        PasswordHasher passwordHasher,
        IEmailService emailService,
        IOptions<InviteOptions> inviteOptions)
    {
        _userRepository = userRepository;
        _jwtService = jwtService;
        _passwordHasher = passwordHasher;
        _emailService = emailService;
        _inviteOptions = inviteOptions.Value;
    }

    public async Task<UserDto?> InviteAsync(InviteUserDto request, CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        if (await _userRepository.EmailExistsAsync(email))
        {
            return null;
        }

        var roleName = request.Role.ToString();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = $"{request.FirstName.Trim()} {request.LastName.Trim()}",
            Email = email,
            PasswordHash = string.Empty
        };

        try
        {
            await _userRepository.AddWithRoleAsync(user, roleName);
        }
        catch (DbUpdateException)
        {
            return null;
        }

        var inviteToken = _jwtService.MintInviteToken(user, TimeSpan.FromHours(_inviteOptions.TokenLifetimeHours));
        var inviteLink = $"{_inviteOptions.FrontendBaseUrl.TrimEnd('/')}/set-password?token={Uri.EscapeDataString(inviteToken.AccessToken)}";
        var body = InviteEmailTemplate.Build(user.Name, inviteLink, inviteToken.ExpiresAtUtc);

        try
        {
            await _emailService.SendAsync(user.Email, InviteEmailTemplate.Subject, body, cancellationToken);
        }
        catch
        {
            await _userRepository.DeleteAsync(user);
            throw;
        }

        return new UserDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Roles = [roleName]
        };
    }

    public async Task<UserDto?> VerifyAsync(string token)
    {
        var user = await GetPendingUserFromTokenAsync(token);
        return user is null
            ? null
            : new UserDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email
            };
    }

    public async Task<bool> AcceptAsync(AcceptInviteDto request)
    {
        var user = await GetPendingUserFromTokenAsync(request.Token);
        if (user is null)
        {
            return false;
        }

        var passwordHash = _passwordHasher.Hash(request.Password);
        return await _userRepository.SetPasswordIfUnsetAsync(user.Id, passwordHash);
    }

    private async Task<User?> GetPendingUserFromTokenAsync(string token)
    {
        var principal = _jwtService.ValidateInviteToken(token);
        if (principal is null)
        {
            return null;
        }

        var subject = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        var email = principal.FindFirst(JwtRegisteredClaimNames.Email)?.Value;
        if (!Guid.TryParse(subject, out var userId))
        {
            return null;
        }

        var user = await _userRepository.GetByIdAsync(userId);
        if (user is null || user.Email != email || !string.IsNullOrEmpty(user.PasswordHash))
        {
            return null;
        }

        return user;
    }
}
