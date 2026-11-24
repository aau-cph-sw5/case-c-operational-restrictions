using Backend.Exceptions;
using Backend.Models.DTOs;
using Backend.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SignatureController : ApiControllerBase
{
    private readonly SignatureService _signatureService;

    public SignatureController(SignatureService signatureService)
    {
        _signatureService = signatureService;
    }

    /// <summary>
    /// Lists the logged-in user's required signatures that are still pending.
    /// </summary>
    /// <response code="200">Returns the pending required signatures (empty when there are none)</response>
    /// <response code="401">No valid JWT was supplied</response>
    [HttpGet("required/me")]
    [Authorize]
    public async Task<ActionResult<List<RequiredSignatureDto>>> GetMyPendingRequiredSignatures()
    {
        var requiredSignatures = await _signatureService.GetPendingRequiredSignaturesAsync(CurrentUserId);

        return Ok(requiredSignatures);
    }

    /// <summary>
    /// Signs the required signature with the given ID.
    /// </summary>
    /// <response code="200">Returns the created signature</response>
    /// <response code="403">The logged-in user is not the required signer</response>
    /// <response code="404">The required signature was not found</response>
    /// <response code="409">The required signature is already signed or has been invalidated</response>
    [HttpPost("sign/{requiresSignatureId:guid}")]
    [Authorize]
    public ActionResult<SignatureDto> Sign(Guid requiresSignatureId)
    {
        try
        {
            var signature = _signatureService.Sign(requiresSignatureId, CurrentUserId);

            return Ok(SignatureDto.FromDalEntity(signature));
        }
        catch (RequiresSignatureNotFoundException exception)
        {
            return NotFound(new { error = exception.Message });
        }
        catch (NotRequiredSignerException exception)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { error = exception.Message });
        }
        catch (RequiresSignatureNotSignableException exception)
        {
            return Conflict(new { error = exception.Message });
        }
    }
}
