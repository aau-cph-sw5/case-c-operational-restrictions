using System.Security.Claims;

using Backend.Authorization;
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
using Microsoft.Extensions.Logging;

namespace Backend.Tests;

// MET-C-026: server-side version validation and signing status.
[Collection("SignatureService")]
public class RestrictionVersionValidationTests
{
    private readonly AppDbContext _dbContext;
    private readonly OperationalRestrictionService _restrictionService;
    private readonly ILogger<RestrictionsController> _logger;

    public RestrictionVersionValidationTests()
    {
        _dbContext = TestDatabase.CreateSeededContext();
        _logger = new LoggerFactory().CreateLogger<RestrictionsController>();

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
            new SignatureService(_dbContext));
    }

    [Fact]
    public async Task SigningSupersededVersionIsRefusedWithStaleVersionCode()
    {
        var restriction = await AddDraftAsync();
        var newerVersion = await EditAsync(restriction);
        var controller = CreateController(restriction.OriginatorUserId);

        // The user read the old version and signs it after it was edited.
        var actionResult = await controller.Sign(restriction.RestrictionId);

        var conflict = Assert.IsType<ConflictObjectResult>(actionResult);
        Assert.Contains("STALE_VERSION", conflict.Value!.ToString());

        // Nothing was signed: the newer version is still an unsigned draft.
        var latest = await _restrictionService.GetByIdAsync(newerVersion.Id);
        Assert.Equal(newerVersion.Id, latest.Id);
        Assert.Equal(RestrictionState.Draft, latest.State);
        Assert.Null(latest.OriginatorSignature);
    }

    [Fact]
    public async Task SigningLatestVersionAfterEditSucceeds()
    {
        var restriction = await AddDraftAsync();
        var newerVersion = await EditAsync(restriction);
        var controller = CreateController(restriction.OriginatorUserId);

        var actionResult = await controller.Sign(newerVersion.Id);

        Assert.IsType<OkObjectResult>(actionResult);
    }

    [Fact]
    public async Task SigningStatusBeforeSigningShowsNoSignature()
    {
        var restriction = await AddDraftAsync();
        var controller = CreateController(restriction.OriginatorUserId);

        var result = await controller.GetSigningStatus(restriction.RestrictionId);

        var status = Assert.IsType<SigningStatusDto>(Assert.IsType<OkObjectResult>(result.Result).Value);
        Assert.True(status.IsLatestVersion);
        Assert.Equal(RestrictionState.Draft, status.State);
        Assert.Empty(status.MySignatures);
    }

    [Fact]
    public async Task SigningStatusAfterLostResponseShowsCommittedSignature()
    {
        var restriction = await AddDraftAsync();
        var controller = CreateController(restriction.OriginatorUserId);
        await controller.Sign(restriction.RestrictionId);

        // The client never saw the response and asks about the version it signed.
        var result = await controller.GetSigningStatus(restriction.RestrictionId);

        var status = Assert.IsType<SigningStatusDto>(Assert.IsType<OkObjectResult>(result.Result).Value);
        Assert.False(status.IsLatestVersion);
        Assert.Equal(RestrictionState.PendingDutyOperationsManagerApproval, status.State);
        var signature = Assert.Single(status.MySignatures);
        Assert.Equal(RoleNames.Originator, signature.SigneeRole);
    }

    [Fact]
    public async Task SigningStatusOfAnotherUsersDraftIsNotFound()
    {
        var restriction = await AddDraftAsync();
        var controller = CreateController(Guid.NewGuid());

        var result = await controller.GetSigningStatus(restriction.RestrictionId);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    private async Task<OperationalRestriction> AddDraftAsync()
    {
        var originator = Guid.NewGuid();
        var restriction = new OperationalRestriction
        {
            RestrictionId = Guid.NewGuid(),
            OriginatorUserId = originator,
            CreatedBy = originator,
            State = RestrictionState.Draft,
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddDays(1),
            CreatedAt = DateTime.UtcNow,
            Title = "Test restriction title",
            SituationDescription = "Test situation",
            RestrictionDescription = "Test restriction",
            CancellationDescription = "Test cancellation"
        };

        _dbContext.OperationalRestrictions.Add(restriction);
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        return restriction;
    }

    // Editing stores a new version, which makes the given one superseded.
    private async Task<OperationalRestrictionDto> EditAsync(OperationalRestriction restriction)
    {
        var dto = OperationalRestrictionDto.FromOperationalRestrictionToDto(restriction);
        dto.RestrictionDescription = "Changed after the signer opened it";
        return await _restrictionService.EditRestrictionAsync(restriction.RestrictionId, dto);
    }

    private RestrictionsController CreateController(Guid currentUserId)
    {
        return new RestrictionsController(_restrictionService, _logger)
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
    }
}
