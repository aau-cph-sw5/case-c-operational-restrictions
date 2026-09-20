using System.IdentityModel.Tokens.Jwt;
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

 
}
