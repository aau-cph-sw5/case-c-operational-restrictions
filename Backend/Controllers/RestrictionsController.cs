using Backend.Exceptions;
using Backend.Models.DTOs;
using Backend.Models.Enums;
using Backend.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RestrictionsController : ApiControllerBase
{
    private readonly IOperationalRestrictionService _operationalRestrictionService;

    public RestrictionsController(IOperationalRestrictionService operationalRestrictionService)
    {
        _operationalRestrictionService = operationalRestrictionService;
    }

    /// <summary>
    /// Signs an operational restriction as its originator.
    /// </summary>
    /// <response code="403">The authenticated user is not the restriction originator</response>
    /// <response code="404">The operational restriction was not found</response>
    /// <response code="400">The restriction is missing required information</response>
    /// <response code="409">The operational restriction is not in Draft state</response>
    /// <response code="200">Returns the created immutable signature</response>
    [HttpPost("{id:guid}/sign")]
    [Authorize]
    public async Task<IActionResult> Sign(Guid id)
    {
        OperationalRestrictionDto restriction;
        try
        {
            restriction = await _operationalRestrictionService.GetByIdAsync(id);
        }
        catch (OperationalRestrictionNotFoundException exception)
        {
            return NotFound(new { error = exception.Message });
        }

        var validationResult = ValidateSigningRequirements(restriction);
        if (validationResult is not null)
        {
            return validationResult;
        }

        return await SignRestrictionAsync(id);
    }

    private IActionResult? ValidateSigningRequirements(OperationalRestrictionDto restriction)
    {
        if (CurrentUserId == Guid.Empty || restriction.OriginatorUserId != CurrentUserId)
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                new { error = "Only the restriction originator can provide the initial signature." });
        }

        if (restriction.Status != RestrictionStatus.Draft)
        {
            return Conflict(new { error = "Only draft restrictions can be signed." });
        }

        if (IsMissingRequiredSignatureData(restriction))
        {
            return BadRequest(new { error = "StartDate and RestrictionDescription must be populated before signing." });
        }

        return null;
    }

    private static bool IsMissingRequiredSignatureData(OperationalRestrictionDto restriction)
    {
        return restriction.StartDate == default
            || string.IsNullOrWhiteSpace(restriction.RestrictionDescription);
    }

    private async Task<IActionResult> SignRestrictionAsync(Guid id)
    {
        try
        {
            var signature = await _operationalRestrictionService
                .SignOriginatorAndAdvanceToPendingApprovalAsync(id, CurrentUserId);
            return Ok(SignatureDto.FromDalEntity(signature));
        }
        catch (OperationalRestrictionNotFoundException exception)
        {
            return NotFound(new { error = exception.Message });
        }
        catch (OperationalRestrictionNotSignableException exception)
        {
            return Conflict(new { error = exception.Message });
        }
        catch (RequiresSignatureNotSignableException exception)
        {
            return Conflict(new { error = exception.Message });
        }
    }
}
