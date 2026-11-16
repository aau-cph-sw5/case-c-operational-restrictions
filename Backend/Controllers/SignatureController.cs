using Backend.Exceptions;
using Backend.Models.DTOs;
using Backend.Services;
using Backend.Authorization;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SignatureController : ApiControllerBase
{
    private readonly SignatureService _signatureService;

    private readonly IOperationalRestrictionService _operationalRestrictionService;

    public SignatureController(
        SignatureService signatureService,
        IOperationalRestrictionService operationalRestrictionService)
    {
        _signatureService = signatureService;
        _operationalRestrictionService = operationalRestrictionService;
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


    /// <summary>
    /// Gets the signature statuses for an Operational Restriction.
    /// </summary>
    /// <response code="200">Returns required signature statuses</response>
    /// <response code="404">The restriction does not exist</response>
    [HttpGet("restriction/{restrictionId:guid}/status")]
    [Authorize]
    [RequiresPermission(Permissions.OperationalRestrictionRead)]
    public async Task<ActionResult<List<SignatureStatusDto>>> GetSignatureStatuses(
        Guid restrictionId)
    {
        try
        {
            // Check that the restriction exists and the user can access it.
            await _operationalRestrictionService.GetByIdAsync(
                restrictionId, CurrentUserId);

            var statuses = _signatureService.GetSignatureStatuses(restrictionId);

            return Ok(statuses);
        }
        catch (OperationalRestrictionNotFoundException exception)
        {
            return NotFound(new { error = exception.Message });
        }
    }

}
