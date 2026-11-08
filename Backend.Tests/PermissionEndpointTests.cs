using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

using Backend.Data;
using Backend.Models.DTOs;
using Backend.Models.Enums;

namespace Backend.Tests;

// Integration tests for [RequiresPermission] on the real HTTP pipeline and for
// the rule that an originator cannot approve their own restriction.
[Collection("SignatureService")]
public class PermissionEndpointTests : IAsyncLifetime, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    // Seeded restriction in PendingDutyOperationsManagerApproval state.
    private static readonly Guid SeededPendingRestrictionId =
        Guid.Parse("5eed0000-0000-0000-0000-000000000003");

    private readonly TestWebApplicationFactory _factory = new();

    public Task InitializeAsync() => _factory.SeedAsync();

    public async Task DisposeAsync() => await _factory.DisposeAsync();

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

    [Fact]
    public async Task GetAllReturns200ForUserWhoseRoleGrantsRead()
    {
        var response = await CreateClientFor(DbSeeder.LarsId).GetAsync("/api/restrictions");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ApproveWithoutTokenReturns401()
    {
        var response = await _factory.CreateClient()
            .PostAsync($"/api/restrictions/{SeededPendingRestrictionId}/approve", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ApproveWithoutApprovePermissionReturns403()
    {
        // Lars is an Operator: his role does not grant OperationalRestriction:Approve.
        var response = await CreateClientFor(DbSeeder.LarsId)
            .PostAsync($"/api/restrictions/{SeededPendingRestrictionId}/approve", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ApproveWithApprovePermissionOnPendingRestrictionReturns200()
    {
        // Dan is a DutyOperationsManager and did not create the seeded restriction.
        var response = await CreateClientFor(DbSeeder.DanId)
            .PostAsync($"/api/restrictions/{SeededPendingRestrictionId}/approve", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var approved = await ReadAsync<OperationalRestrictionDto>(response);
        Assert.Equal(RestrictionState.Approved, approved.State);
    }

    [Fact]
    public async Task OriginatorWithApprovePermissionCannotApproveTheirOwnRestriction()
    {
        // Anna is a DutyOperationsManager, so her role allows approving...
        var anna = CreateClientFor(DbSeeder.AnnaId);
        var draft = await CreateDraftAsAsync(anna);
        await SignAsAsync(anna, draft.Id);
        var latest = await GetLatestVersionAsAsync(anna, draft.Id);

        // ...but she created the restriction, so approving it must be refused.
        var response = await anna.PostAsync($"/api/restrictions/{latest.Id}/approve", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("cannot approve", body);
    }

    [Fact]
    public async Task ApprovingAnotherUsersDraftIsNotBlockedAsSelfApproval()
    {
        // Lars (Operator) can create drafts; Anna approving Lars's draft passes
        // the self-approval rule and fails only on the lifecycle state (409),
        // proving the 403 above is specifically about self-approval.
        var lars = CreateClientFor(DbSeeder.LarsId);
        var draft = await CreateDraftAsAsync(lars);

        var response = await CreateClientFor(DbSeeder.AnnaId)
            .PostAsync($"/api/restrictions/{draft.Id}/approve", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task OtherApproverCanApproveRestrictionSubmittedBySomeoneElse()
    {
        var anna = CreateClientFor(DbSeeder.AnnaId);
        var draft = await CreateDraftAsAsync(anna);
        await SignAsAsync(anna, draft.Id);
        var latest = await GetLatestVersionAsAsync(anna, draft.Id);

        var response = await CreateClientFor(DbSeeder.DanId)
            .PostAsync($"/api/restrictions/{latest.Id}/approve", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var approved = await ReadAsync<OperationalRestrictionDto>(response);
        Assert.Equal(RestrictionState.Approved, approved.State);
        Assert.Equal(DbSeeder.AnnaId, approved.OriginatorUserId);
    }

    private HttpClient CreateClientFor(Guid userId)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _factory.CreateTokenFor(userId));

        return client;
    }

    private static async Task<OperationalRestrictionDto> CreateDraftAsAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/restrictions", new
        {
            title = "Draft Title",
            startDate = DateTime.UtcNow.AddDays(1),
            endDate = DateTime.UtcNow.AddDays(2),
            situationDescription = "Situation details",
            restrictionDescription = "Restriction details",
            cancellationDescription = "Cancellation details"
        });
        response.EnsureSuccessStatusCode();

        return await ReadAsync<OperationalRestrictionDto>(response);
    }

    private static async Task SignAsAsync(HttpClient client, Guid restrictionId)
    {
        var response = await client.PostAsync($"/api/restrictions/{restrictionId}/sign", null);
        response.EnsureSuccessStatusCode();
    }

    private static async Task<OperationalRestrictionDto> GetLatestVersionAsAsync(HttpClient client, Guid restrictionId)
    {
        var response = await client.GetAsync($"/api/restrictions/{restrictionId}");
        response.EnsureSuccessStatusCode();

        return await ReadAsync<OperationalRestrictionDto>(response);
    }

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<T>(JsonOptions))!;
}
