using Backend.Models.DTOs;

namespace Backend.Services.Users;

public interface IUserInvitationService
{
    Task<UserDto?> InviteAsync(InviteUserDto request, CancellationToken cancellationToken = default);
    Task<UserDto?> VerifyAsync(string token);
    Task<bool> AcceptAsync(AcceptInviteDto request);
}
