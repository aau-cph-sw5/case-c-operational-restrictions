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
    /// Temporary endpoint: picks a random required signature and signs it.
    /// </summary>
    /// <response code="200">Returns the created signature</response>
    [HttpPost("sign-random")]
    public ActionResult<SignatureDto> SignRandom()
    {
        var requiresSignatureId = _signatureService.GetSomeRandomRequiredSignatureId();

        // Temporary: signs as the required signee, as there is no logged-in user to test with.
        var signeeId = _signatureService.GetRequiresSignatureById(requiresSignatureId).SigneeId;

        var signature = _signatureService.Sign(requiresSignatureId, signeeId);

        return Ok(SignatureDto.FromDalEntity(signature));
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
        if (!TryGetSigningContext(out var signingContext))
        {
            return InvalidAttemptKeyResult();
        }

        try
        {
            var signature = _signatureService.Sign(requiresSignatureId, CurrentUserId, signingContext);

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
        catch (AttemptKeyReusedException exception)
        {
            return Conflict(new { error = exception.Message });
        }
    }

    /// <summary>
    /// Tells the logged-in user whether a signing attempt left a signature, so the client can
    /// recover after losing the connection mid-signature. Works for every signing action.
    /// </summary>
    /// <response code="200">Signed with the committed signature, or NotSigned</response>
    [HttpGet("attempts/{attemptKey:guid}")]
    [Authorize]
    public async Task<ActionResult<SigningAttemptStatusDto>> GetAttemptStatus(Guid attemptKey)
    {
        var signature = await _signatureService.FindReplayAsync(attemptKey, CurrentUserId);

        return Ok(new SigningAttemptStatusDto
        {
            AttemptKey = attemptKey,
            Status = signature is null ? SigningAttemptStatusDto.NotSigned : SigningAttemptStatusDto.Signed,
            Signature = signature is null ? null : SignatureDto.FromDalEntity(signature)
        });
    }
}
