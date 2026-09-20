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
    public ActionResult<LoginResponseDto> Login(LoginRequestDto request)
    {
        var result = _authService.Login(request);
        
        return result.Succeeded
            ? Ok(result.Response)
            : Unauthorized(new { error = "Invalid email or password." });
    }
}
