using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Route("api/restrictions")]
public class RestrictionController : ApiControllerBase
{
    /// <summary>
    /// Signs an operational restriction as its originator.
    /// </summary>
    /// <response code="501">Restriction signing is not implemented yet</response>
    [HttpPost("{id:guid}/sign")]
    [Authorize]
    public IActionResult Sign(Guid id)
    {
        return StatusCode(
            StatusCodes.Status501NotImplemented,
            new { error = "Restriction signing is not implemented yet." });
    }
}