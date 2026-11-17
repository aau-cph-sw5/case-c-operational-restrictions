using Backend.Data;
using Backend.Models.Entities;
using Backend.Models.Enums;
using Backend.Services;

using Microsoft.EntityFrameworkCore;

namespace Backend.Tests;

public class SignatureCompletenessServiceTests
{
    // xUnit creates a new test class instance per test, so each test gets a
    // fresh in-memory database seeded like the local development database.
    private readonly AppDbContext _appDbContext;
    private readonly SignatureCompletenessService _service;

    public SignatureCompletenessServiceTests()
    {
        _appDbContext = TestDatabase.CreateSeededContext();
        _service = new SignatureCompletenessService(_appDbContext);
    }

    // Seeded Active restrictions: REF-2026-004, REF-2026-007 and REF-2026-014.
    private static readonly Guid[] SeededActiveRestrictionIds =
    [
        Guid.Parse("5eed0000-0000-0000-0000-000000000004"),
        Guid.Parse("5eed0000-0000-0000-0000-000000000007"),
        Guid.Parse("5eed0000-0000-0000-0000-000000000014")
    ];

    [Fact]
    public async Task ReportContainsOnlyOperatorsOnShiftAsRows()
    {
        var report = await _service.GetReportAsync();

        // Lars and Oscar are Operators with WorkingStatus AtWork; Sofie is OffWork
        // and the remaining seeded users have other roles.
        Assert.Equal(
            new[] { DbSeeder.LarsId, DbSeeder.OscarId },
            report.Operators.Select(o => o.OperatorId).OrderBy(id => id).ToArray());
        Assert.DoesNotContain(report.Operators, o => o.OperatorId == DbSeeder.SofieId);
        Assert.All(report.Operators, o => Assert.False(string.IsNullOrWhiteSpace(o.Name)));
    }

    [Fact]
    public async Task ReportContainsOnlyRestrictionsInForceAsColumns()
    {
        var report = await _service.GetReportAsync();

        Assert.Equal(
            SeededActiveRestrictionIds.OrderBy(id => id).ToArray(),
            report.Restrictions.Select(r => r.RestrictionId).OrderBy(id => id).ToArray());
    }

    [Fact]
    public async Task EveryOperatorHasOneCellPerRestrictionInForce()
    {
        var report = await _service.GetReportAsync();

        Assert.All(report.Operators, operatorRow =>
        {
            Assert.Equal(report.Restrictions.Count, operatorRow.Cells.Count);
            Assert.Equal(
                report.Restrictions.Select(r => r.RestrictionId).ToArray(),
                operatorRow.Cells.Select(c => c.RestrictionId).ToArray());
        });
    }

    [Fact]
    public async Task UnsignedCombinationsAreIdentifiableAndCounted()
    {
        var report = await _service.GetReportAsync();

        // No seeded restriction has a signature sheet, so every combination is unsigned.
        var expectedOutstanding = report.Operators.Count * report.Restrictions.Count;

        Assert.Equal(expectedOutstanding, report.OutstandingSignatureCount);
        Assert.All(report.Operators.SelectMany(o => o.Cells), cell =>
        {
            Assert.False(cell.IsSigned);
            Assert.Null(cell.SignedAt);
        });
    }

    [Fact]
    public async Task SignedCombinationIsMarkedWithSignedAtAndReducesOutstandingCount()
    {
        var restrictionId = SeededActiveRestrictionIds[0];
        var signedAt = DateTime.UtcNow;
        _appDbContext.Add(new Signature
        {
            Id = Guid.NewGuid(),
            RequiresSignatureId = Guid.NewGuid(),
            OperationalRestrictionId = restrictionId,
            SigneeId = DbSeeder.LarsId,
            SigneeRole = UserRole.Operator,
            SignedAt = signedAt
        });
        await _appDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var report = await _service.GetReportAsync();

        var larsRow = report.Operators.Single(o => o.OperatorId == DbSeeder.LarsId);
        var signedCell = larsRow.Cells.Single(c => c.RestrictionId == restrictionId);
        Assert.True(signedCell.IsSigned);
        Assert.Equal(signedAt, signedCell.SignedAt);

        var oscarRow = report.Operators.Single(o => o.OperatorId == DbSeeder.OscarId);
        Assert.False(oscarRow.Cells.Single(c => c.RestrictionId == restrictionId).IsSigned);

        Assert.Equal(report.Operators.Count * report.Restrictions.Count - 1, report.OutstandingSignatureCount);
    }

    [Fact]
    public async Task SignaturesOnNonActiveRestrictionsDoNotCountAsSigned()
    {
        var draftId = Guid.Parse("5eed0000-0000-0000-0000-000000000001");
        _appDbContext.Add(new Signature
        {
            Id = Guid.NewGuid(),
            RequiresSignatureId = Guid.NewGuid(),
            OperationalRestrictionId = draftId,
            SigneeId = DbSeeder.LarsId,
            SigneeRole = UserRole.Operator,
            SignedAt = DateTime.UtcNow
        });
        await _appDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var report = await _service.GetReportAsync();

        // The draft is not a column, so its signature changes nothing.
        Assert.DoesNotContain(report.Restrictions, r => r.RestrictionId == draftId);
        Assert.Equal(report.Operators.Count * report.Restrictions.Count, report.OutstandingSignatureCount);
    }

    [Fact]
    public async Task ReportStatesTheUtcTimeItWasComputed()
    {
        var before = DateTime.UtcNow;

        var report = await _service.GetReportAsync();

        Assert.Equal(DateTimeKind.Utc, report.ComputedAt.Kind);
        Assert.InRange(report.ComputedAt, before, DateTime.UtcNow);
    }
}
