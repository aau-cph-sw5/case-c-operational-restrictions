using Backend.Models.DTOs;
using Backend.Services.Users;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InvitationsController : ApiControllerBase
{
    private readonly IUserInvitationService _invitationService;

    public InvitationsController(IUserInvitationService invitationService)
    {
        _invitationService = invitationService;
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<UserDto>> Invite(InviteUserDto request, CancellationToken cancellationToken)
    {
        var user = await _invitationService.InviteAsync(request, cancellationToken);

        return user is not null
            ? StatusCode(StatusCodes.Status201Created, user)
            : Conflict(new { error = "A user with this email already exists." });
    }

    [HttpPost("verify")]
    [AllowAnonymous]
    public async Task<ActionResult<UserDto>> Verify(VerifyInviteDto request)
    {
        var user = await _invitationService.VerifyAsync(request.Token);

        return user is not null
            ? Ok(user)
            : BadRequest(new { error = "The invitation link is invalid, expired or has already been used." });
    }

    [HttpPost("accept")]
    [AllowAnonymous]
    public async Task<IActionResult> Accept(AcceptInviteDto request)
    {
        var accepted = await _invitationService.AcceptAsync(request);

        return accepted
            ? NoContent()
            : BadRequest(new { error = "The invitation link is invalid, expired or has already been used." });
    }
}
