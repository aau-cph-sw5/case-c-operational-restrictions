using System.Security.Claims;

using Backend.Authorization;
using Backend.Controllers;
using Backend.Data;
using Backend.Exceptions;
using Backend.Lifecycle;
using Backend.Models.DTOs;
using Backend.Models.Entities;
using Backend.Models.Enums;
using Backend.Models.Requests;
using Backend.Services;
using Backend.Signing;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Backend.Tests;

// MET-C-010: device class on signatures, and signing that is either committed or not at all.
public class DeviceClassResolverTests
{
    private const string IPhone =
        "Mozilla/5.0 (iPhone; CPU iPhone OS 17_4 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.4 Mobile/15E148 Safari/604.1";
    private const string IPad =
        "Mozilla/5.0 (iPad; CPU OS 17_4 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.4 Mobile/15E148 Safari/604.1";
    private const string AndroidPhone =
        "Mozilla/5.0 (Linux; Android 14; Pixel 8) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0 Mobile Safari/537.36";
    private const string AndroidTablet =
        "Mozilla/5.0 (Linux; Android 14; SM-X710) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0 Safari/537.36";
    private const string WindowsChrome =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0 Safari/537.36";
    private const string MacSafari =
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.4 Safari/605.1.15";

    [Theory]
    [InlineData(IPhone, DeviceClass.Phone)]
    [InlineData(IPad, DeviceClass.Tablet)]
    [InlineData(AndroidPhone, DeviceClass.Phone)]
    [InlineData(AndroidTablet, DeviceClass.Tablet)]
    [InlineData(WindowsChrome, DeviceClass.Desktop)]
    [InlineData(MacSafari, DeviceClass.Desktop)]
    [InlineData("", DeviceClass.Unknown)]
    [InlineData("curl/8.5.0", DeviceClass.Unknown)]
    public void ResolveClassifiesUserAgent(string userAgent, DeviceClass expected)
    {
        Assert.Equal(expected, DeviceClassResolver.Resolve(userAgent, null));
    }

    [Fact]
    public void ResolveNullUserAgentIsUnknown()
    {
        Assert.Equal(DeviceClass.Unknown, DeviceClassResolver.Resolve(null, "?1"));
    }

    [Fact]
    public void ResolveUsesMobileClientHintWhenUserAgentIsGeneric()
    {
        Assert.Equal(DeviceClass.Phone, DeviceClassResolver.Resolve("SomeBrowser/1.0", "?1"));
    }

    [Fact]
    public void NormalizeUserAgentTruncatesToMaximumLength()
    {
        var normalized = DeviceClassResolver.NormalizeUserAgent(new string('a', 2000));

        Assert.Equal(DeviceClassResolver.MaxUserAgentLength, normalized.Length);
    }
}

[Collection("SignatureService")]
public class RemoteSigningTests
{
    private const string PhoneAgent = "Mozilla/5.0 (iPhone; CPU iPhone OS 17_4 like Mac OS X) Mobile/15E148 Safari/604.1";
    private const string TabletAgent = "Mozilla/5.0 (iPad; CPU OS 17_4 like Mac OS X) Mobile/15E148 Safari/604.1";
    private const string DesktopAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) Chrome/124.0 Safari/537.36";

    private readonly AppDbContext _dbContext;
    private readonly SignatureService _signatureService;
    private readonly OperationalRestrictionService _restrictionService;
    private readonly ILogger<RestrictionsController> _logger;

    public RemoteSigningTests()
    {
        _dbContext = TestDatabase.CreateSeededContext();
        _signatureService = new SignatureService(_dbContext);
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
            _signatureService);
    }

    // AC4: the device class is recorded with the signature, derived on the server.

    [Theory]
    [InlineData(PhoneAgent, "Phone")]
    [InlineData(TabletAgent, "Tablet")]
    [InlineData(DesktopAgent, "Desktop")]
    public async Task OriginatorSignatureRecordsDeviceClassAndUserAgent(string userAgent, string expectedClass)
    {
        var restriction = await AddRestrictionAsync(RestrictionState.Draft);
        var controller = CreateController(restriction.OriginatorUserId, userAgent);

        var result = Assert.IsType<OkObjectResult>(await controller.Sign(restriction.RestrictionId));

        var dto = Assert.IsType<SignatureDto>(result.Value);
        Assert.Equal(expectedClass, dto.DeviceClass);

        var stored = await _dbContext.Set<Signature>()
            .AsNoTracking()
            .SingleAsync(signature => signature.Id == dto.Id, TestContext.Current.CancellationToken);
        Assert.Equal(Enum.Parse<DeviceClass>(expectedClass), stored.DeviceClass);
        Assert.Equal(userAgent, stored.UserAgent);
    }

    [Fact]
    public async Task DeviceClassIsKeptWhenTheSignatureIsCopiedToANewVersion()
    {
        var restriction = await AddRestrictionAsync(RestrictionState.Draft);
        var attemptKey = Guid.NewGuid();
        await CreateController(restriction.OriginatorUserId, PhoneAgent, attemptKey).Sign(restriction.RestrictionId);
        var pendingVersion = await _dbContext.OperationalRestrictions
            .AsNoTracking()
            .OrderByDescending(r => r.Version)
            .FirstAsync(r => r.OriginalOperationalRestrictionId == restriction.RestrictionId, TestContext.Current.CancellationToken);

        // A second transition copies the signatures onto the next version.
        var returned = await _restrictionService.ReturnForChangesAsync(
            pendingVersion.RestrictionId, Guid.NewGuid(), "Please add the affected track section.");

        var nextVersion = await _dbContext.OperationalRestrictions
            .AsNoTracking()
            .Include(r => r.SignatureSheet)
            .SingleAsync(r => r.RestrictionId == returned.Id, TestContext.Current.CancellationToken);
        var copied = Assert.Single(nextVersion.SignatureSheet);
        Assert.Equal(DeviceClass.Phone, copied.DeviceClass);
        Assert.Equal(PhoneAgent, copied.UserAgent);
        Assert.Equal(attemptKey, copied.AttemptKey);
    }

    [Fact]
    public void OperatorSignatureRecordsDeviceClass()
    {
        var required = _signatureService.CreateRequiredSignatures(Guid.NewGuid()).First();
        var context = new SigningContext(DeviceClass.Tablet, TabletAgent, null);

        var signature = _signatureService.Sign(required.Id, required.SigneeId, context);

        Assert.Equal(DeviceClass.Tablet, signature.DeviceClass);
        Assert.Equal(TabletAgent, signature.UserAgent);
    }

    [Fact]
    public void SignatureWithoutContextIsRecordedAsUnknownDevice()
    {
        var required = _signatureService.CreateRequiredSignatures(Guid.NewGuid()).First();

        var signature = _signatureService.Sign(required.Id, required.SigneeId);

        Assert.Equal(DeviceClass.Unknown, signature.DeviceClass);
    }

    // AC3: the assurance level (valid JWT, ADR 0008) is the same on every device.

    [Theory]
    [InlineData(PhoneAgent)]
    [InlineData(TabletAgent)]
    [InlineData(DesktopAgent)]
    public async Task SigningOutcomeDoesNotDependOnTheDevice(string userAgent)
    {
        var restriction = await AddRestrictionAsync(RestrictionState.Draft);

        var result = await CreateController(restriction.OriginatorUserId, userAgent).Sign(restriction.RestrictionId);

        Assert.IsType<OkObjectResult>(result);
    }

    [Theory]
    [InlineData(PhoneAgent)]
    [InlineData(TabletAgent)]
    [InlineData(DesktopAgent)]
    public void NotBeingTheRequiredSignerIsRefusedOnEveryDevice(string userAgent)
    {
        var required = _signatureService.CreateRequiredSignatures(Guid.NewGuid()).First();
        var context = new SigningContext(DeviceClassResolver.Resolve(userAgent, null), userAgent, null);

        Assert.Throws<NotRequiredSignerException>(
            () => _signatureService.Sign(required.Id, Guid.NewGuid(), context));
    }

    [Fact]
    public async Task ClientCannotSelectTheDeviceClassWithAHeader()
    {
        var restriction = await AddRestrictionAsync(RestrictionState.Draft);
        var controller = CreateController(restriction.OriginatorUserId, DesktopAgent);
        controller.HttpContext.Request.Headers["X-Device-Class"] = "Phone";

        var result = Assert.IsType<OkObjectResult>(await controller.Sign(restriction.RestrictionId));

        Assert.Equal("Desktop", Assert.IsType<SignatureDto>(result.Value).DeviceClass);
    }

    // AC5: a signature exists only if it was committed; a retry never creates a second one.

    [Fact]
    public async Task RetryWithTheSameKeyReturnsTheCommittedSignatureAndDoesNotSignTwice()
    {
        var restriction = await AddRestrictionAsync(RestrictionState.Draft);
        var attemptKey = Guid.NewGuid();
        var controller = CreateController(restriction.OriginatorUserId, PhoneAgent, attemptKey);

        var first = Assert.IsType<OkObjectResult>(await controller.Sign(restriction.RestrictionId));
        var retry = Assert.IsType<OkObjectResult>(await controller.Sign(restriction.RestrictionId));

        var firstSignature = Assert.IsType<SignatureDto>(first.Value);
        var retriedSignature = Assert.IsType<SignatureDto>(retry.Value);
        Assert.Equal(firstSignature.Id, retriedSignature.Id);
        Assert.Equal(firstSignature.SignedAt, retriedSignature.SignedAt);

        var versions = await _dbContext.OperationalRestrictions
            .CountAsync(r => r.OriginalOperationalRestrictionId == restriction.RestrictionId, TestContext.Current.CancellationToken);
        Assert.Equal(1, versions);
    }

    [Fact]
    public async Task LookupFallsBackToTheDatabaseForASignatureNotInMemory()
    {
        // The in-memory lists are lost on a restart, so a signature that was persisted with its
        // restriction version must still be found by its attempt key.
        var restriction = await AddRestrictionAsync(RestrictionState.PendingDutyOperationsManagerApproval);
        var attemptKey = Guid.NewGuid();
        _dbContext.Set<Signature>().Add(new Signature
        {
            Id = Guid.NewGuid(),
            RequiresSignatureId = Guid.NewGuid(),
            OperationalRestrictionId = restriction.RestrictionId,
            SigneeId = restriction.OriginatorUserId,
            SigneeRole = RoleNames.Originator,
            SignedAt = DateTime.UtcNow,
            DeviceClass = DeviceClass.Phone,
            UserAgent = PhoneAgent,
            AttemptKey = attemptKey
        });
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var found = _signatureService.FindByAttemptKey(attemptKey, restriction.OriginatorUserId);

        Assert.NotNull(found);
        Assert.Equal(DeviceClass.Phone, found.DeviceClass);
    }

    [Fact]
    public async Task AttemptThatFailedLeavesNoSignatureForItsKey()
    {
        var restriction = await AddRestrictionAsync(RestrictionState.PendingDutyOperationsManagerApproval);
        var attemptKey = Guid.NewGuid();
        var controller = CreateController(restriction.OriginatorUserId, PhoneAgent, attemptKey);

        Assert.IsType<ConflictObjectResult>(await controller.Sign(restriction.RestrictionId));

        Assert.Null(_signatureService.FindByAttemptKey(attemptKey, restriction.OriginatorUserId));
    }

    [Fact]
    public async Task KeyAlreadyUsedForAnotherSigningActionIsRefused()
    {
        var restriction = await AddRestrictionAsync(RestrictionState.Draft);
        var attemptKey = Guid.NewGuid();
        var controller = CreateController(restriction.OriginatorUserId, PhoneAgent, attemptKey);
        await controller.Sign(restriction.RestrictionId);

        var result = await controller.SignForImplementation(restriction.RestrictionId);

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task MalformedKeyIsRejected()
    {
        var restriction = await AddRestrictionAsync(RestrictionState.Draft);
        var controller = CreateController(restriction.OriginatorUserId, PhoneAgent);
        controller.HttpContext.Request.Headers["Idempotency-Key"] = "not-a-guid";

        var result = await controller.Sign(restriction.RestrictionId);

        Assert.Equal(StatusCodes.Status400BadRequest, Assert.IsType<BadRequestObjectResult>(result).StatusCode);
    }

    [Fact]
    public void OperatorRetryWithTheSameKeyReturnsTheSameSignature()
    {
        var required = _signatureService.CreateRequiredSignatures(Guid.NewGuid()).First();
        var context = new SigningContext(DeviceClass.Phone, PhoneAgent, Guid.NewGuid());

        var first = _signatureService.Sign(required.Id, required.SigneeId, context);
        var retry = _signatureService.Sign(required.Id, required.SigneeId, context);

        Assert.Same(first, retry);
        Assert.Equal(RequiresSignatureStatus.CompletedSignature, required.RequiresSignatureStatus);
    }

    [Fact]
    public void OperatorSigningWithoutKeyStillRefusesASecondSignature()
    {
        var required = _signatureService.CreateRequiredSignatures(Guid.NewGuid()).First();
        _signatureService.Sign(required.Id, required.SigneeId);

        Assert.Throws<RequiresSignatureNotSignableException>(
            () => _signatureService.Sign(required.Id, required.SigneeId));
    }

    [Fact]
    public void StatusEndpointReportsSignedWithTheCommittedSignature()
    {
        var required = _signatureService.CreateRequiredSignatures(Guid.NewGuid()).First();
        var attemptKey = Guid.NewGuid();
        _signatureService.Sign(required.Id, required.SigneeId, new SigningContext(DeviceClass.Phone, PhoneAgent, attemptKey));
        var controller = CreateSignatureController(required.SigneeId);

        var response = controller.GetAttemptStatus(attemptKey).GetAwaiter().GetResult();

        var status = Assert.IsType<SigningAttemptStatusDto>(Assert.IsType<OkObjectResult>(response.Result).Value);
        Assert.Equal(SigningAttemptStatusDto.Signed, status.Status);
        Assert.Equal("Phone", status.Signature?.DeviceClass);
    }

    [Fact]
    public void StatusEndpointReportsNotSignedForAnUnknownKey()
    {
        var controller = CreateSignatureController(Guid.NewGuid());

        var response = controller.GetAttemptStatus(Guid.NewGuid()).GetAwaiter().GetResult();

        var status = Assert.IsType<SigningAttemptStatusDto>(Assert.IsType<OkObjectResult>(response.Result).Value);
        Assert.Equal(SigningAttemptStatusDto.NotSigned, status.Status);
        Assert.Null(status.Signature);
    }

    [Fact]
    public void StatusEndpointDoesNotRevealAnotherUsersSignature()
    {
        var required = _signatureService.CreateRequiredSignatures(Guid.NewGuid()).First();
        var attemptKey = Guid.NewGuid();
        _signatureService.Sign(required.Id, required.SigneeId, new SigningContext(DeviceClass.Phone, PhoneAgent, attemptKey));
        var controller = CreateSignatureController(Guid.NewGuid());

        var response = controller.GetAttemptStatus(attemptKey).GetAwaiter().GetResult();

        var status = Assert.IsType<SigningAttemptStatusDto>(Assert.IsType<OkObjectResult>(response.Result).Value);
        Assert.Equal(SigningAttemptStatusDto.NotSigned, status.Status);
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

    private RestrictionsController CreateController(Guid currentUserId, string userAgent, Guid? attemptKey = null)
    {
        var httpContext = CreateHttpContext(currentUserId, userAgent, attemptKey);

        return new RestrictionsController(_restrictionService, _logger)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };
    }

    private SignatureController CreateSignatureController(Guid currentUserId)
    {
        return new SignatureController(_signatureService)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = CreateHttpContext(currentUserId, DesktopAgent, null)
            }
        };
    }

    private static DefaultHttpContext CreateHttpContext(Guid currentUserId, string userAgent, Guid? attemptKey)
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim("sub", currentUserId.ToString())
            ], "Test"))
        };
        httpContext.Request.Headers.UserAgent = userAgent;
        if (attemptKey is not null)
        {
            httpContext.Request.Headers["Idempotency-Key"] = attemptKey.ToString();
        }

        return httpContext;
    }
}
