using Backend.Exceptions;
using Backend.Models.DTOs;
using Backend.Models.Enums;
using Backend.Models.Requests;
using Backend.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RestrictionsController : ApiControllerBase
{
    private readonly IOperationalRestrictionService _operationalRestrictionService;

    public RestrictionsController(IOperationalRestrictionService operationalRestrictionService)
    {
        _operationalRestrictionService = operationalRestrictionService;
    }

    /// <summary>
    /// Creates a new restriction in Draft state. The logged-in user is recorded as the originator.
    /// </summary>
    /// <response code="201">Returns the created restriction</response>
    /// <response code="400">The end date is not after the start date</response>
    [HttpPost]
    public async Task<ActionResult<OperationalRestrictionDto>> Create(CreateRestrictionRequest request)
    {
        try
        {
            var restriction = await _operationalRestrictionService.CreateRestrictionAsync(request with { CreatedBy = CurrentUserId });

            return CreatedAtAction(nameof(GetById), new { id = restriction.Id }, restriction);
        }
        catch (InvalidRestrictionPeriodException exception)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid restriction period",
                Detail = exception.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (MissingRequiredRestrictionFieldException exception)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Missing required field",
                Detail = exception.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    /// <summary>
    /// Retrieves a restriction by its ID. Drafts can only be retrieved by their originator.
    /// </summary>
    /// <response code="200">Returns the restriction</response>
    /// <response code="404">The restriction was not found, or it is a draft owned by another user</response>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OperationalRestrictionDto>> GetById(Guid id)
    {
        try
        {
            var restriction = await _operationalRestrictionService.GetByIdAsync(id, CurrentUserId);

            return Ok(restriction);
        }
        catch (OperationalRestrictionNotFoundException exception)
        {
            return NotFound(new { error = exception.Message });
        }
    }

    /// <summary>
    /// Retrieves all non-draft restrictions plus the logged-in user's own drafts.
    /// </summary>
    /// <response code="200">Returns the restrictions</response>
    [HttpGet]
    public async Task<ActionResult<List<OperationalRestrictionDto>>> GetAll()
    {
        var restrictions = await _operationalRestrictionService.GetAllAsync(CurrentUserId);

        return Ok(restrictions);
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

        return await SignRestrictionAsync(restriction.Id);
    }

    private IActionResult? ValidateSigningRequirements(OperationalRestrictionDto restriction)
    {
        if (CurrentUserId == Guid.Empty || restriction.OriginatorUserId != CurrentUserId)
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                new { error = "Only the restriction originator can provide the initial signature." });
        }

        if (restriction.State != RestrictionState.Draft)
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
        catch (InvalidOperationException exception)
        {
            return Conflict(new { error = exception.Message });
        }
    }
}
