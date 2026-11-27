using Backend.Authorization;
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
    private static readonly Action<ILogger, Exception?> LogUnexpectedError =
        LoggerMessage.Define(LogLevel.Error, new EventId(0, nameof(LogUnexpectedError)), "Unexpected error");

    private readonly IOperationalRestrictionService _operationalRestrictionService;
    private readonly ILogger<RestrictionsController> _logger;

    public RestrictionsController(
        IOperationalRestrictionService operationalRestrictionService,
        ILogger<RestrictionsController> logger)
    {
        _operationalRestrictionService = operationalRestrictionService;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new restriction in Draft state. The logged-in user is recorded as the originator.
    /// </summary>
    /// <response code="201">Returns the created restriction</response>
    /// <response code="400">The end date is not after the start date</response>
    /// <response code="403">The user has no role granting OperationalRestriction:Create</response>
    [HttpPost]
    [RequiresPermission(Permissions.OperationalRestrictionCreate)]
    public async Task<ActionResult<OperationalRestrictionDto>> Create(CreateRestrictionRequest request)
    {
        try
        {
            var restriction = await _operationalRestrictionService.CreateRestrictionAsync(
                request with { CreatedBy = CurrentUserId });

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
    /// <response code="403">The user has no role granting OperationalRestriction:Read</response>
    [HttpGet("{id:guid}")]
    [RequiresPermission(Permissions.OperationalRestrictionRead)]
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
    /// <response code="403">The user has no role granting OperationalRestriction:Read</response>
    [HttpGet]
    [RequiresPermission(Permissions.OperationalRestrictionRead)]
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
    /// <response code="403">The user has no role granting OperationalRestriction:Sign</response>
    [HttpPost("{id:guid}/sign")]
    [RequiresPermission(Permissions.OperationalRestrictionSign)]
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

    [HttpPost("{id:guid}/request-changes")]
    [RequiresPermission(Permissions.OperationalRestrictionRequestChanges)]
    public async Task<ActionResult<OperationalRestrictionDto>> RequestChanges(
        Guid id,
        RequestChangesRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Comment))
        {
            return BadRequest(new { error = "A review comment is required." });
        }

        try
        {
            var restriction = await _operationalRestrictionService.ReturnForChangesAsync(
                id,
                CurrentUserId,
                request.Comment.Trim());

            return Ok(restriction);
        }
        catch (OperationalRestrictionNotFoundException exception)
        {
            return NotFound(new { error = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { error = exception.Message });
        }
    }

    /// <summary>
    /// Signs and approves a restriction that is pending duty operations manager
    /// approval, then advances it to control room implementation approval.
    /// The originator of a restriction can never approve it themselves, even if
    /// their role grants the approval permission.
    /// </summary>
    /// <response code="200">Returns the signed restriction awaiting implementation approval</response>
    /// <response code="403">The user has no role granting OperationalRestriction:Approve, or they created the restriction themselves</response>
    /// <response code="404">The operational restriction was not found</response>
    /// <response code="409">The operational restriction is not pending approval</response>
    [HttpPost("{id:guid}/approve")]
    [RequiresPermission(Permissions.OperationalRestrictionApprove)]
    public async Task<IActionResult> Approve(Guid id)
    {
        try
        {
            var approved = await _operationalRestrictionService.ApproveRestrictionAsync(id, CurrentUserId);
            return Ok(approved);
        }
        catch (OperationalRestrictionNotFoundException exception)
        {
            return NotFound(new { error = exception.Message });
        }
        catch (SelfApprovalNotAllowedException exception)
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                new { error = exception.Message });
        }
        catch (NotDutyOperationsManagerException exception)
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                new { error = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { error = exception.Message });
        }
        catch (Exception exception)
        {
            LogUnexpectedError(_logger, exception);
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Signs an operational restriction for implementation as the control room
    /// supervisor, moving it from PendingControlRoomSupervisorApproval to Active.
    /// </summary>
    /// <response code="403">The authenticated user is not a control room supervisor</response>
    /// <response code="404">The operational restriction was not found</response>
    /// <response code="409">The operational restriction is not awaiting control room supervisor approval</response>
    /// <response code="200">Returns the created immutable signature</response>

    [HttpPost("{id:guid}/sign-for-implementation")]
    public async Task<IActionResult> SignForImplementation(Guid id)
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

        if (restriction.State != RestrictionState.PendingControlRoomSupervisorApproval)
        {
            return Conflict(new
            {
                error =
                    "Only restrictions awaiting control room supervisor approval can be signed for implementation."
            });
        }

        try
        {
            var signature =
                await _operationalRestrictionService.SignControlRoomSupervisorApprovalAsync(id, CurrentUserId);
            return Ok(SignatureDto.FromDalEntity(signature));
        }
        catch (OperationalRestrictionNotFoundException exception)
        {
            return NotFound(new { error = exception.Message });
        }
        catch (NotControlRoomSupervisorException exception)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { error = exception.Message });
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

    /// <summary>
    /// Step 1 of the cancellation chain: the originator signs an Active
    /// restriction cancelled once its cause is resolved, moving it to
    /// PendingCancellation. Only the originator check applies here, so every
    /// role that can create a restriction can also start its cancellation.
    /// </summary>
    /// <response code="200">Returns the restriction now pending cancellation</response>
    /// <response code="403">The authenticated user is not the restriction originator</response>
    /// <response code="404">The operational restriction was not found</response>
    /// <response code="409">The operational restriction is not Active</response>
    [HttpPost("{id:guid}/cancel")]
    public Task<IActionResult> StartCancellation(Guid id)
    {
        return RunCancellationStepAsync(
            () => _operationalRestrictionService.StartCancellationAsync(id, CurrentUserId));
    }

    /// <summary>
    /// Step 2 of the cancellation chain: the Duty Operations Manager confirms
    /// the cause is resolved and signs, moving the restriction to CancellationApproved.
    /// </summary>
    /// <response code="200">Returns the restriction approved for cancellation</response>
    /// <response code="403">The user is not a duty operations manager, is the originator, or lacks the cancel permission</response>
    /// <response code="404">The operational restriction was not found</response>
    /// <response code="409">The operational restriction is not pending cancellation</response>
    [HttpPost("{id:guid}/cancel/approve")]
    [RequiresPermission(Permissions.OperationalRestrictionCancel)]
    public Task<IActionResult> ApproveCancellation(Guid id)
    {
        return RunCancellationStepAsync(
            () => _operationalRestrictionService.ApproveCancellationAsync(id, CurrentUserId));
    }

    /// <summary>
    /// Step 3 of the cancellation chain: the Control Room Supervisor signs to
    /// take the restriction out of force.
    /// </summary>
    /// <response code="200">Returns the restriction that is now out of force</response>
    /// <response code="403">The user is not a control room supervisor, or lacks the cancel permission</response>
    /// <response code="404">The operational restriction was not found</response>
    /// <response code="409">The operational restriction is not approved for cancellation</response>
    [HttpPost("{id:guid}/cancel/sign")]
    [RequiresPermission(Permissions.OperationalRestrictionCancel)]
    public Task<IActionResult> SignCancellation(Guid id)
    {
        return RunCancellationStepAsync(
            () => _operationalRestrictionService.ControlRoomCancellationAsync(id, CurrentUserId));
    }

    private async Task<IActionResult> RunCancellationStepAsync(
        Func<Task<OperationalRestrictionDto>> cancellationStep)
    {
        try
        {
            return Ok(await cancellationStep());
        }
        catch (OperationalRestrictionNotFoundException exception)
        {
            return NotFound(new { error = exception.Message });
        }
        catch (Exception exception) when (exception is NotOriginatorException
                                              or SelfApprovalNotAllowedException
                                              or NotDutyOperationsManagerException
                                              or NotControlRoomSupervisorException)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { error = exception.Message });
        }
        catch (Exception exception) when (exception is OperationalRestrictionNotSignableException
                                              or InvalidOperationException)
        {
            return Conflict(new { error = exception.Message });
        }
        catch (Exception exception)
        {
            LogUnexpectedError(_logger, exception);
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
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
        catch (Exception exception)
        {
            LogUnexpectedError(_logger, exception);
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}
