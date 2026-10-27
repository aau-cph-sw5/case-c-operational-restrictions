using Backend.Data;
using Backend.Exceptions;
using Backend.Models.DTOs;
using Backend.Models.Enums;
using Backend.Models.Requests;
using Backend.Services;

using Microsoft.EntityFrameworkCore;

namespace Backend.Tests;

public class OperationalRestrictionServiceTests
{
    private readonly AppDbContext _appDbContext;
    private readonly OperationalRestrictionService _service;

    public OperationalRestrictionServiceTests()
    {
        _appDbContext = TestDatabase.CreateSeededContext();
        _service = new OperationalRestrictionService(_appDbContext, new SignatureService(_appDbContext));
    }

    [Fact]
    public async Task CreateRestrictionPersistsDraftWithCreatorAsOriginator()
    {
        var created = await CreateDraftAsync(DbSeeder.AnnaId);

        var stored = await _appDbContext.OperationalRestrictions.AsNoTracking().SingleAsync(x => x.Id == created.Id);
        Assert.Equal(RestrictionStatus.Draft, stored.Status);
        Assert.Equal(DbSeeder.AnnaId, stored.Originator);
        Assert.Equal(DbSeeder.AnnaId, stored.CreatedBy);
    }

    [Fact]
    public async Task CreateRestrictionPersistsTextDescriptionsAndReference()
    {
        var request = new CreateRestrictionRequest(
            DbSeeder.AnnaId,
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(2),
            Reference: "REF-99",
            SituationDescription: "Situation details",
            RestrictionDescription: "Restriction details",
            CancellationDescription: "Cancellation details"
        );

        var created = await _service.CreateRestrictionAsync(request);

        var stored = await _appDbContext.OperationalRestrictions.AsNoTracking().SingleAsync(x => x.Id == created.Id);
        Assert.Equal("REF-99", stored.Reference);
        Assert.Equal("Situation details", stored.SituationDescription);
        Assert.Equal("Restriction details", stored.RestrictionDescription);
        Assert.Equal("Cancellation details", stored.CancellationDescription);

        Assert.Equal("REF-99", created.Reference);
        Assert.Equal("Situation details", created.SituationDescription);
        Assert.Equal("Restriction details", created.RestrictionDescription);
        Assert.Equal("Cancellation details", created.CancellationDescription);
    }

    [Fact]
    public async Task CreateRestrictionAllowsNullEndDate()
    {
        var request = new CreateRestrictionRequest(
            DbSeeder.AnnaId,
            DateTime.UtcNow.AddDays(1),
            EndDate: null,
            SituationDescription: "Situation details",
            RestrictionDescription: "Restriction details",
            CancellationDescription: "Cancellation details"
        );

        var created = await _service.CreateRestrictionAsync(request);

        var stored = await _appDbContext.OperationalRestrictions.AsNoTracking().SingleAsync(x => x.Id == created.Id);
        Assert.Null(stored.EndDate);
        Assert.Null(created.EndDate);
    }

    [Fact]
    public async Task CreateRestrictionWithEndDateBeforeStartDateThrows()
    {
        var request = new CreateRestrictionRequest(
            DbSeeder.AnnaId,
            DateTime.UtcNow.AddDays(2),
            DateTime.UtcNow.AddDays(1),
            SituationDescription: "Situation details",
            RestrictionDescription: "Restriction details",
            CancellationDescription: "Cancellation details"
        );

        await Assert.ThrowsAsync<InvalidRestrictionPeriodException>(() => _service.CreateRestrictionAsync(request));
    }

    [Fact]
    public async Task CreateRestrictionWithEndDateEqualToStartDateThrows()
    {
        var date = DateTime.UtcNow.AddDays(1);
        var request = new CreateRestrictionRequest(
            DbSeeder.AnnaId,
            date,
            date,
            SituationDescription: "Situation details",
            RestrictionDescription: "Restriction details",
            CancellationDescription: "Cancellation details"
        );

        await Assert.ThrowsAsync<InvalidRestrictionPeriodException>(() => _service.CreateRestrictionAsync(request));
    }

    [Fact]
    public async Task CreateRestrictionWithoutStartDateThrows()
    {
        var request = new CreateRestrictionRequest(
            DbSeeder.AnnaId,
            StartDate: null,
            SituationDescription: "Situation details",
            RestrictionDescription: "Restriction details",
            CancellationDescription: "Cancellation details"
        );

        await Assert.ThrowsAsync<MissingRequiredRestrictionFieldException>(() => _service.CreateRestrictionAsync(request));
    }

    [Theory]
    [InlineData("", "Restriction details", "Cancellation details")]
    [InlineData("   ", "Restriction details", "Cancellation details")]
    [InlineData("Situation details", "", "Cancellation details")]
    [InlineData("Situation details", "   ", "Cancellation details")]
    [InlineData("Situation details", "Restriction details", "")]
    [InlineData("Situation details", "Restriction details", "   ")]
    public async Task CreateRestrictionWithEmptyOrWhitespaceDescriptionThrows(
        string situation,
        string restriction,
        string cancellation)
    {
        var request = new CreateRestrictionRequest(
            DbSeeder.AnnaId,
            DateTime.UtcNow.AddDays(1),
            SituationDescription: situation,
            RestrictionDescription: restriction,
            CancellationDescription: cancellation
        );

        await Assert.ThrowsAsync<MissingRequiredRestrictionFieldException>(() => _service.CreateRestrictionAsync(request));
    }

    [Fact]
    public async Task GetByIdReturnsOwnDraft()
    {
        var draft = await CreateDraftAsync(DbSeeder.AnnaId);

        var restriction = await _service.GetByIdAsync(draft.Id, DbSeeder.AnnaId);

        Assert.Equal(draft.Id, restriction.Id);
    }

    [Fact]
    public async Task GetByIdHidesDraftFromOtherUsers()
    {
        var draft = await CreateDraftAsync(DbSeeder.AnnaId);

        await Assert.ThrowsAsync<OperationalRestrictionNotFoundException>(() =>
            _service.GetByIdAsync(draft.Id, DbSeeder.LarsId));
    }

    [Fact]
    public async Task GetByIdReturnsNonDraftToAnyUser()
    {
        var nonDraft = await _appDbContext.OperationalRestrictions
            .FirstAsync(x => x.Status != RestrictionStatus.Draft && x.Originator != DbSeeder.LarsId);

        var restriction = await _service.GetByIdAsync(nonDraft.Id, DbSeeder.LarsId);

        Assert.Equal(nonDraft.Id, restriction.Id);
    }

    [Fact]
    public async Task GetByIdWithUnknownIdThrows()
    {
        await Assert.ThrowsAsync<OperationalRestrictionNotFoundException>(() =>
            _service.GetByIdAsync(Guid.NewGuid(), DbSeeder.AnnaId));
    }

    [Fact]
    public async Task GetAllReturnsAllNonDraftsAndOnlyOwnDrafts()
    {
        var nonDraftCount = await _appDbContext.OperationalRestrictions.CountAsync(x => x.Status != RestrictionStatus.Draft);
        var ownDraftCount = await _appDbContext.OperationalRestrictions
            .CountAsync(x => x.Status == RestrictionStatus.Draft && x.Originator == DbSeeder.AnnaId);

        var restrictions = await _service.GetAllAsync(DbSeeder.AnnaId);

        Assert.Equal(nonDraftCount + ownDraftCount, restrictions.Count);
        Assert.All(
            restrictions.Where(x => x.Status == RestrictionStatus.Draft),
            x => Assert.Equal(DbSeeder.AnnaId, x.OriginatorUserId));
    }

    private Task<OperationalRestrictionDto> CreateDraftAsync(Guid createdBy)
    {
        var request = new CreateRestrictionRequest(
            createdBy,
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(2),
            SituationDescription: "Situation details",
            RestrictionDescription: "Restriction details",
            CancellationDescription: "Cancellation details"
        );

        return _service.CreateRestrictionAsync(request);
    }
}
