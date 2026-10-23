using System.Security.Claims;

using Backend.Controllers;
using Backend.Data;
using Backend.Models.DTOs;
using Backend.Models.Entities;
using Backend.Models.Enums;
using Backend.Services;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Backend.Tests;

public class RestrictionSigningTests : IAsyncDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _dbContext;
    private readonly SignatureService _signatureService = new();
    private readonly OperationalRestrictionService _restrictionService;

    public RestrictionSigningTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;
        _dbContext = new AppDbContext(options);
        _dbContext.Database.EnsureCreated();
        _restrictionService = new OperationalRestrictionService(_dbContext, _signatureService);
    }

    [Fact]
    public async Task OriginatorSigningCreatesSignatureAndAdvancesRestrictionToPendingApproval()
    {
        var restriction = await AddRestrictionAsync(RestrictionStatus.Draft);
        var controller = CreateController(restriction.Originator);

        var actionResult = await controller.Sign(restriction.Id);

        var result = Assert.IsType<OkObjectResult>(actionResult);
        var signature = Assert.IsType<SignatureDto>(result.Value);
        var persistedRestriction = await _dbContext.OperationalRestrictions
            .AsNoTracking()
            .SingleAsync(item => item.Id == restriction.Id);

        Assert.Equal(restriction.Id, signature.OperationalRestrictionId);
        Assert.Equal(restriction.Originator, signature.SigneeId);
        Assert.Equal(UserRole.Originator, signature.SigneeRole);
        Assert.Equal(RestrictionStatus.PendingApproval, persistedRestriction.Status);
    }

    [Fact]
    public async Task NonOriginatorCannotSignRestriction()
    {
        var restriction = await AddRestrictionAsync(RestrictionStatus.Draft);
        var controller = CreateController(Guid.NewGuid());

        var actionResult = await controller.Sign(restriction.Id);

        var result = Assert.IsType<ObjectResult>(actionResult);
        var persistedRestriction = await _dbContext.OperationalRestrictions
            .AsNoTracking()
            .SingleAsync(item => item.Id == restriction.Id);

        Assert.Equal(StatusCodes.Status403Forbidden, result.StatusCode);
        Assert.Equal(RestrictionStatus.Draft, persistedRestriction.Status);
        Assert.Null(_signatureService.GetOriginatorSignature(restriction.Id));
    }

    [Fact]
    public async Task RestrictionOutsideDraftCannotBeSigned()
    {
        var restriction = await AddRestrictionAsync(RestrictionStatus.PendingApproval);
        var controller = CreateController(restriction.Originator);

        var actionResult = await controller.Sign(restriction.Id);

        Assert.IsType<ConflictObjectResult>(actionResult);
        Assert.Null(_signatureService.GetOriginatorSignature(restriction.Id));
    }

    [Fact]
    public async Task SignatureCreationFailureLeavesRestrictionInDraft()
    {
        var restriction = await AddRestrictionAsync(RestrictionStatus.Draft);
        var existingSignature = _signatureService.SignOriginator(restriction.Id, restriction.Originator);
        var controller = CreateController(restriction.Originator);

        var actionResult = await controller.Sign(restriction.Id);

        var persistedRestriction = await _dbContext.OperationalRestrictions
            .AsNoTracking()
            .SingleAsync(item => item.Id == restriction.Id);

        Assert.IsType<ConflictObjectResult>(actionResult);
        Assert.Equal(RestrictionStatus.Draft, persistedRestriction.Status);
        Assert.Same(existingSignature, _signatureService.GetOriginatorSignature(restriction.Id));
    }

    [Theory]
    [InlineData(true, false, "Valid description")]
    [InlineData(false, true, "Valid description")]
    [InlineData(false, false, "")]
    [InlineData(false, false, "   ")]
    public async Task IncompleteRestrictionCannotBeSigned(bool defaultStartDate, bool defaultEndDate, string description)
    {
        var restriction = new OperationalRestriction
        {
            Id = Guid.NewGuid(),
            Originator = Guid.NewGuid(),
            CreatedBy = Guid.NewGuid(),
            Status = RestrictionStatus.Draft,
            StartDate = defaultStartDate ? default : DateTime.UtcNow.Date,
            EndDate = defaultEndDate ? default : DateTime.UtcNow.Date.AddDays(1),
            CreatedAt = DateTime.UtcNow,
            RestrictionDescription = description
        };
        _dbContext.OperationalRestrictions.Add(restriction);
        await _dbContext.SaveChangesAsync();

        var controller = CreateController(restriction.Originator);
        var actionResult = await controller.Sign(restriction.Id);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);

        var persisted = await _dbContext.OperationalRestrictions
            .AsNoTracking()
            .SingleAsync(item => item.Id == restriction.Id);
        Assert.Equal(RestrictionStatus.Draft, persisted.Status);
    }

    [Fact]
    public async Task GetByIdReturnsOriginatorSignatureAfterSigning()
    {
        var restriction = await AddRestrictionAsync(RestrictionStatus.Draft);
        var controller = CreateController(restriction.Originator);

        await controller.Sign(restriction.Id);

        var dto = await _restrictionService.GetByIdAsync(restriction.Id);
        Assert.NotNull(dto.OriginatorSignature);
        Assert.Equal(restriction.Id, dto.OriginatorSignature.OperationalRestrictionId);
        Assert.Equal(restriction.Originator, dto.OriginatorSignature.SigneeId);
        Assert.Equal(UserRole.Originator, dto.OriginatorSignature.SigneeRole);
        Assert.Equal(RestrictionStatus.PendingApproval, dto.Status);
    }

    private async Task<OperationalRestriction> AddRestrictionAsync(RestrictionStatus status)
    {
        var restriction = new OperationalRestriction
        {
            Id = Guid.NewGuid(),
            Originator = Guid.NewGuid(),
            CreatedBy = Guid.NewGuid(),
            Status = status,
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddDays(1),
            CreatedAt = DateTime.UtcNow,
            RestrictionDescription = "Test restriction"
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

    public async ValueTask DisposeAsync()
    {
        await _dbContext.DisposeAsync();
        await _connection.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
