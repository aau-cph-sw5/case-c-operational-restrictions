using System.IdentityModel.Tokens.Jwt;

using Backend.Models.Requests;
using Backend.Signing;

using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

public abstract class ApiControllerBase : ControllerBase
{
    //User. henter claims listen, så bliver det nuværende id hentet ellers returneres et tomt id.
    //Kan bruges i alle controllers der benytter controller basen.
    protected Guid CurrentUserId
    {
        get
        {
            var subject = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            return Guid.TryParse(subject, out var id) ? id : Guid.Empty;

        }
    }

    protected ActionResult InvalidAttemptKeyResult() =>
        BadRequest(new ProblemDetails
        {
            Title = "Invalid attempt key",
            Detail = "The Idempotency-Key header must be a GUID.",
            Status = StatusCodes.Status400BadRequest
        });

    // Builds the signing context from the request headers.
    // The device class is derived from the User-Agent and Client Hints on the server.
    // Returns false only when an idempotency-Key header is present but is not a GUID.
    protected bool TryGetSigningContext(out SigningContext signingContext)
    {
        var headers = HttpContext?.Request.Headers;
        if (headers is null)
        {
            signingContext = SigningContext.Unknown;
            return true;
        }

        var userAgent = headers.UserAgent.ToString();
        var deviceClass = DeviceClassResolver.Resolve(userAgent, headers["Sec-CH-UA-Mobile"].ToString());

        Guid? attemptKey = null;
        var rawKey = headers["Idempotency-Key"].ToString();
        if (!string.IsNullOrWhiteSpace(rawKey))
        {
            if (!Guid.TryParse(rawKey, out var parsedKey))
            {
                signingContext = SigningContext.Unknown;
                return false;
            }

            attemptKey = parsedKey;
        }

        signingContext = new SigningContext(deviceClass, DeviceClassResolver.NormalizeUserAgent(userAgent), attemptKey);
        return true;
    }
}
