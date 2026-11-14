using Backend.Models.DTOs;
using Backend.Services.Auth;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;


[ApiController]
[Route("api/[controller]")]
public class AuthController : ApiControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponseDto>> Login(
        LoginRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _authService.LoginAsync(request, cancellationToken);

        return result.Succeeded
            ? Ok(result.Response)
            : Unauthorized(new { error = "Invalid email or password." });
    }


    [HttpPost("check-token/{token}")]
    [AllowAnonymous]
    public async Task<ActionResult<UserDto>> CheckValidityOfToken(
        string token,
        CancellationToken cancellationToken)
    {
        var user = await _authService.CheckValidityOfTokenAsync(token, cancellationToken);
        return user is not null
            ? Ok(user)
            : Unauthorized(new { error = "Invalid or expired token." });
    }
}
