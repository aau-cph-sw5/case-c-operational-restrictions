using System.Security.Claims;

using Backend.Controllers;
using Backend.Data;
using Backend.Lifecycle;
using Backend.Models.DTOs;
using Backend.Models.Entities;
using Backend.Models.Enums;
using Backend.Services;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Tests;

public class RestrictionSigningTests
{
    private readonly AppDbContext _dbContext;
    private readonly SignatureService _signatureService;
    private readonly OperationalRestrictionService _restrictionService;

    public RestrictionSigningTests()
    {
        _dbContext = TestDatabase.CreateSeededContext();
        _signatureService = new SignatureService(_dbContext);

        var lifecycle = new RestrictionLifecycleService(
            new IRestrictionGuard[]
            {
                new StartDateIsFoundGuard(),
                new OperationalRestrictionIdIsFound(),
                new DescriptionTextIsFound(),
                new OriginatorSignatureIsFound(),
                new DutyOperationsManagerSignatureIsFound(),
                new ControlRoomSupervisorSignatureIsFound(),
                new OperatorsAreAssigned(),
                new ReturnedForChanges(),
                new CancelledRefused()
            });

        _restrictionService = new OperationalRestrictionService(
            lifecycle,
            new OperationalRestrictionRepository(_dbContext),
            new RestrictionTransitionEventRepository(_dbContext),
            new UnitOfWork(_dbContext),
            _signatureService);
    }

    [Fact]
    public async Task OriginatorSigningCreatesSignatureAndAdvancesRestrictionToPendingApproval()
    {
        var restriction = await AddRestrictionAsync(RestrictionState.Draft);
        var controller = CreateController(restriction.OriginatorUserId);

        var actionResult = await controller.Sign(restriction.RestrictionId);

        var result = Assert.IsType<OkObjectResult>(actionResult);
        var signature = Assert.IsType<SignatureDto>(result.Value);
        var persistedRestriction = await _dbContext.OperationalRestrictions
            .AsNoTracking()
            .SingleAsync(item => item.RestrictionId == restriction.RestrictionId);

        // The original version remains Draft, while a new version is created in PendingDutyOperationsManagerApproval
        var latestVersion = await _dbContext.OperationalRestrictions
            .AsNoTracking()
            .OrderByDescending(r => r.Version)
            .FirstAsync(item => item.OriginalOperationalRestrictionId == restriction.RestrictionId);

        Assert.Equal(latestVersion.RestrictionId, signature.OperationalRestrictionId);
        Assert.Equal(restriction.OriginatorUserId, signature.SigneeId);
        Assert.Equal(UserRole.Originator, signature.SigneeRole);
        Assert.Equal(RestrictionState.Draft, persistedRestriction.State);
        Assert.Equal(RestrictionState.PendingDutyOperationsManagerApproval, latestVersion.State);
    }

    [Fact]
    public async Task NonOriginatorCannotSignRestriction()
    {
        var restriction = await AddRestrictionAsync(RestrictionState.Draft);
        var controller = CreateController(Guid.NewGuid());

        var actionResult = await controller.Sign(restriction.RestrictionId);

        var result = Assert.IsType<ObjectResult>(actionResult);
        var persistedRestriction = await _dbContext.OperationalRestrictions
            .AsNoTracking()
            .SingleAsync(item => item.RestrictionId == restriction.RestrictionId);

        Assert.Equal(StatusCodes.Status403Forbidden, result.StatusCode);
        Assert.Equal(RestrictionState.Draft, persistedRestriction.State);
        Assert.Null(_signatureService.GetOriginatorSignature(restriction.RestrictionId));
    }

    [Fact]
    public async Task RestrictionOutsideDraftCannotBeSigned()
    {
        var restriction = await AddRestrictionAsync(RestrictionState.PendingDutyOperationsManagerApproval);
        var controller = CreateController(restriction.OriginatorUserId);

        var actionResult = await controller.Sign(restriction.RestrictionId);

        Assert.IsType<ConflictObjectResult>(actionResult);
        Assert.Null(_signatureService.GetOriginatorSignature(restriction.RestrictionId));
    }

    [Fact]
    public async Task SignatureCreationFailureLeavesRestrictionInDraft()
    {
        var restriction = await AddRestrictionAsync(RestrictionState.Draft);
        var existingSignature = _signatureService.SignOriginator(restriction.RestrictionId, restriction.OriginatorUserId);
        var controller = CreateController(restriction.OriginatorUserId);

        var actionResult = await controller.Sign(restriction.RestrictionId);

        var persistedRestriction = await _dbContext.OperationalRestrictions
            .AsNoTracking()
            .SingleAsync(item => item.RestrictionId == restriction.RestrictionId);

        Assert.IsType<ConflictObjectResult>(actionResult);
        Assert.Equal(RestrictionState.Draft, persistedRestriction.State);
        Assert.Same(existingSignature, _signatureService.GetOriginatorSignature(restriction.RestrictionId));
    }

    [Theory]
    [InlineData(true, "Valid description")]
    [InlineData(false, "")]
    [InlineData(false, "   ")]
    public async Task IncompleteRestrictionCannotBeSigned(bool defaultStartDate, string description)
    {
        var originator = Guid.NewGuid();
        var restriction = new OperationalRestriction
        {
            RestrictionId = Guid.NewGuid(),
            OriginatorUserId = originator,
            CreatedBy = originator,
            State = RestrictionState.Draft,
            StartDate = defaultStartDate ? default : DateTime.UtcNow.Date,
            CreatedAt = DateTime.UtcNow,
            Title = "Title",
            SituationDescription = "Situation",
            RestrictionDescription = description,
            CancellationDescription = "Cancellation"
        };
        _dbContext.OperationalRestrictions.Add(restriction);
        await _dbContext.SaveChangesAsync();

        var controller = CreateController(restriction.OriginatorUserId);
        var actionResult = await controller.Sign(restriction.RestrictionId);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);

        var persisted = await _dbContext.OperationalRestrictions
            .AsNoTracking()
            .SingleAsync(item => item.RestrictionId == restriction.RestrictionId);
        Assert.Equal(RestrictionState.Draft, persisted.State);
    }

    [Fact]
    public async Task RestrictionWithoutEndDateCanBeSigned()
    {
        var originator = Guid.NewGuid();
        var restriction = new OperationalRestriction
        {
            RestrictionId = Guid.NewGuid(),
            OriginatorUserId = originator,
            CreatedBy = originator,
            State = RestrictionState.Draft,
            StartDate = DateTime.UtcNow.Date,
            EndDate = null,
            CreatedAt = DateTime.UtcNow,
            Title = "Title",
            SituationDescription = "Situation",
            RestrictionDescription = "Test restriction",
            CancellationDescription = "Cancellation"
        };
        _dbContext.OperationalRestrictions.Add(restriction);
        await _dbContext.SaveChangesAsync();

        var controller = CreateController(restriction.OriginatorUserId);

        var actionResult = await controller.Sign(restriction.RestrictionId);

        Assert.IsType<OkObjectResult>(actionResult);
        var persisted = await _dbContext.OperationalRestrictions
            .AsNoTracking()
            .SingleAsync(item => item.RestrictionId == restriction.RestrictionId);
        Assert.Equal(RestrictionState.Draft, persisted.State);
    }

    [Fact]
    public async Task GetByIdReturnsOriginatorSignatureAfterSigning()
    {
        var restriction = await AddRestrictionAsync(RestrictionState.Draft);
        var controller = CreateController(restriction.OriginatorUserId);

        var actionResult = await controller.Sign(restriction.RestrictionId);
        var result = Assert.IsType<OkObjectResult>(actionResult);
        var signature = Assert.IsType<SignatureDto>(result.Value);

        var dto = await _restrictionService.GetByIdAsync(restriction.RestrictionId, restriction.OriginatorUserId);
        Assert.NotNull(dto.OriginatorSignature);
        Assert.Equal(signature.OperationalRestrictionId, dto.OriginatorSignature.OperationalRestrictionId);
        Assert.Equal(restriction.OriginatorUserId, dto.OriginatorSignature.SigneeId);
        Assert.Equal(UserRole.Originator, dto.OriginatorSignature.SigneeRole);
        Assert.Equal(RestrictionState.PendingDutyOperationsManagerApproval, dto.State);
    }

    [Fact]
    public async Task SigningAlreadySignedRestrictionReturnsConflict()
    {
        var restriction = await AddRestrictionAsync(RestrictionState.Draft);
        var controller = CreateController(restriction.OriginatorUserId);

        var firstSign = await controller.Sign(restriction.RestrictionId);
        Assert.IsType<OkObjectResult>(firstSign);

        var secondSign = await controller.Sign(restriction.RestrictionId);
        var conflict = Assert.IsType<ConflictObjectResult>(secondSign);
        Assert.Equal(StatusCodes.Status409Conflict, conflict.StatusCode);
    }

    private async Task<OperationalRestriction> AddRestrictionAsync(RestrictionState state)
    {
        var originator = Guid.NewGuid();
        var restriction = new OperationalRestriction
        {
            RestrictionId = Guid.NewGuid(),
            OriginatorUserId = originator,
            CreatedBy = originator,
            State = state,
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddDays(1),
            CreatedAt = DateTime.UtcNow,
            Title = "Test restriction title",
            SituationDescription = "Test situation",
            RestrictionDescription = "Test restriction",
            CancellationDescription = "Test cancellation"
        };

        _dbContext.OperationalRestrictions.Add(restriction);
        await _dbContext.SaveChangesAsync();
        return restriction;
    }

    private RestrictionsController CreateController(Guid currentUserId)
    {
        var controller = new RestrictionsController(_restrictionService)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                    [
                        new Claim("sub", currentUserId.ToString())
                    ], "Test"))
                }
            }
        };

        return controller;
    }
}
