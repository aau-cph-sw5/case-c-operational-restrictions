using Backend.Models.DTOs;

namespace Backend.Services.Auth;

public class LoginResult
{
    public bool Succeeded { get; }

    public LoginResponseDto? Response { get; }

    private LoginResult(bool succeeded, LoginResponseDto? response)
    {
        Succeeded = succeeded;
        Response = response;
    }

    public static LoginResult Success(LoginResponseDto response) => new(true, response);

    public static LoginResult Failure() => new(false, null);
}
