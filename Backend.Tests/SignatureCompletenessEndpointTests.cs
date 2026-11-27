using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

using Backend.Data;
using Backend.Models.DTOs;

namespace Backend.Tests;

// Integration tests for GET /api/signaturecompleteness through the full HTTP
// pipeline: routing, JWT authentication, model binding and the database.
public class SignatureCompletenessEndpointTests : IAsyncLifetime, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly TestWebApplicationFactory _factory = new();

    public async ValueTask InitializeAsync() => await _factory.SeedAsync();

    public async ValueTask DisposeAsync()
    {
        await _factory.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _factory.Dispose();
        }
    }

    private HttpClient CreateClientFor(Guid userId)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _factory.CreateTokenFor(userId));

        return client;
    }

    [Fact]
    public async Task RequestsWithoutTokenReturn401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/signaturecompleteness", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetReturnsOperatorsOnShiftAgainstRestrictionsInForceWithOutstandingCountAndComputedAt()
    {
        var client = CreateClientFor(DbSeeder.MikkelId);

        var response = await client.GetAsync("/api/signaturecompleteness", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var report = await response.Content.ReadFromJsonAsync<SignatureCompletenessReportDto>(JsonOptions, TestContext.Current.CancellationToken);

        Assert.NotNull(report);
        // Seeded operators on shift: Lars and Oscar.
        Assert.Equal(2, report.Operators.Count);
        Assert.Contains(report.Operators, o => o.OperatorId == DbSeeder.LarsId && o.Name == "Lars Nielsen");
        Assert.Contains(report.Operators, o => o.OperatorId == DbSeeder.OscarId);
        // Seeded restrictions in force: REF-2026-004, REF-2026-007, REF-2026-014.
        Assert.Equal(3, report.Restrictions.Count);
        Assert.All(report.Restrictions, r => Assert.False(string.IsNullOrWhiteSpace(r.Title)));
        // Every operator has one cell per restriction in force, all unsigned in the seed.
        Assert.All(report.Operators, o => Assert.Equal(report.Restrictions.Count, o.Cells.Count));
        Assert.Equal(6, report.OutstandingSignatureCount);
        Assert.All(report.Operators.SelectMany(o => o.Cells), cell => Assert.False(cell.IsSigned));
        // The report states when it was computed, in UTC.
        Assert.Equal(DateTimeKind.Utc, report.ComputedAt.Kind);
        Assert.True(report.ComputedAt <= DateTime.UtcNow);
        Assert.True(report.ComputedAt > DateTime.UtcNow.AddMinutes(-5));
    }
}
