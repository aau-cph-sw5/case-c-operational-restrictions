using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;

using Backend.Data;

using Microsoft.IdentityModel.Tokens;

namespace Backend.Tests;

// Integration tests for the RBAC layer through the full HTTP pipeline: routing,
// JWT authentication, the RequiresPermission filter and the database.
// Seeded roles: Anna = DutyOperationsManager (all permissions), Lars = Operator
// (read/create/sign, no approve).
public class RbacEndpointTests : IAsyncLifetime, IDisposable
{
    // Seeded restriction in PendingDutyOperationsManagerApproval state, owned by the placeholder user.
    private static readonly Guid PendingRestrictionId = Guid.Parse("5eed0000-0000-0000-0000-000000000003");

    // Must match the Jwt:SigningKey set in TestWebApplicationFactory.
    private const string TestSigningKey = "integration-test-signing-key-at-least-32-characters";

    private readonly TestWebApplicationFactory _factory = new();

    public async ValueTask InitializeAsync() => await _factory.SeedAsync();

    public async ValueTask DisposeAsync()
    {
        await _factory.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    public void Dispose()
    {
        _factory.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task RequestsWithoutTokenReturn401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/restrictions", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task TokenWithWrongSignatureReturns401()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", CreateRawToken(Guid.NewGuid().ToString(), "a-completely-different-signing-key-32chars!"));

        var response = await client.GetAsync("/api/restrictions", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task TokenWithNonGuidSubjectReturns403()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", CreateRawToken("not-a-guid", TestSigningKey));

        var response = await client.GetAsync("/api/restrictions", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AuthenticatedUserWithoutAnyRoleAssignmentGets403()
    {
        // A validly signed token for a user id that exists in no UserRoleAssignments row.
        var client = CreateClientFor(Guid.NewGuid());

        var getResponse = await client.GetAsync("/api/restrictions", TestContext.Current.CancellationToken);
        var postResponse = await client.PostAsJsonAsync("/api/restrictions", new
        {
            title = "Valid Title",
            startDate = DateTime.UtcNow.AddDays(1),
            endDate = DateTime.UtcNow.AddDays(2),
            situationDescription = "Situation details",
            restrictionDescription = "Restriction details",
            cancellationDescription = "Cancellation details"
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, getResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, postResponse.StatusCode);
    }

    [Fact]
    public async Task UserWithReadPermissionCanListRestrictions()
    {
        var response = await CreateClientFor(DbSeeder.LarsId).GetAsync("/api/restrictions", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task OperatorWithoutApprovePermissionGets403OnApprove()
    {
        var response = await CreateClientFor(DbSeeder.LarsId)
            .PostAsync($"/api/restrictions/{PendingRestrictionId}/approve", null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task PermissionCheckRunsBeforeTheAction()
    {
        // Lars has no approve permission, so even a non-existent restriction yields 403.
        var forbidden = await CreateClientFor(DbSeeder.LarsId)
            .PostAsync($"/api/restrictions/{Guid.NewGuid()}/approve", null, TestContext.Current.CancellationToken);
        // Anna has the approve permission, so the filter passes and the action itself runs
        // (409 here: the approve action reports a missing restriction as a conflict).
        var notForbidden = await CreateClientFor(DbSeeder.AnnaId)
            .PostAsync($"/api/restrictions/{Guid.NewGuid()}/approve", null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, notForbidden.StatusCode);
    }

    [Fact]
    public async Task DutyOperationsManagerPassesTheApproveGate()
    {
        // Anna's role grants OperationalRestriction:Approve, so the filter lets the
        // request through and the approval succeeds (200), unlike Lars's 403 above.
        var response = await CreateClientFor(DbSeeder.AnnaId)
            .PostAsync($"/api/restrictions/{PendingRestrictionId}/approve", null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private HttpClient CreateClientFor(Guid userId)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _factory.CreateTokenFor(userId));

        return client;
    }

    // Builds a JWT by hand (instead of JwtService) so tests can mint tokens
    // with arbitrary subject claims, e.g. a non-Guid sub or a wrong signature.
    private static string CreateRawToken(string subject, string signingKey)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: "backend",
            audience: "backend",
            claims: [new Claim(JwtRegisteredClaimNames.Sub, subject)],
            notBefore: DateTime.UtcNow.AddMinutes(-5),
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
