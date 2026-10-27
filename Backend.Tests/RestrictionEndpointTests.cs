using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

using Backend.Data;
using Backend.Models.DTOs;
using Backend.Models.Enums;

namespace Backend.Tests;

// Integration tests that go through the full HTTP pipeline: routing, JWT authentication, model binding and the database.
public class RestrictionEndpointTests : IAsyncLifetime, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

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
    public async Task RequestsWithoutTokenReturn401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/restrictions");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostCreatesDraftWithLoggedInUserAsOriginatorAndPersistsIt()
    {
        var client = CreateClientFor(DbSeeder.AnnaId);

        var response = await client.PostAsJsonAsync("/api/restrictions", new
        {
            createdBy = DbSeeder.LarsId,
            startDate = DateTime.UtcNow.AddDays(1),
            endDate = DateTime.UtcNow.AddDays(2),
            situationDescription = "Situation details",
            restrictionDescription = "Restriction details",
            cancellationDescription = "Cancellation details"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await ReadAsync<OperationalRestrictionDto>(response);
        Assert.Equal(DbSeeder.AnnaId, created.OriginatorUserId);
        Assert.Equal(RestrictionStatus.Draft, created.Status);
        Assert.Equal("Situation details", created.SituationDescription);
        Assert.Equal("Restriction details", created.RestrictionDescription);
        Assert.Equal("Cancellation details", created.CancellationDescription);

        var fetched = await client.GetAsync($"/api/restrictions/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, fetched.StatusCode);
    }

    [Fact]
    public async Task PostWithEndDateBeforeStartDateReturns400()
    {
        var client = CreateClientFor(DbSeeder.AnnaId);

        var response = await client.PostAsJsonAsync("/api/restrictions", new
        {
            startDate = DateTime.UtcNow.AddDays(2),
            endDate = DateTime.UtcNow.AddDays(1),
            situationDescription = "Situation details",
            restrictionDescription = "Restriction details",
            cancellationDescription = "Cancellation details"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostWithoutStartDateReturns400()
    {
        var client = CreateClientFor(DbSeeder.AnnaId);

        var response = await client.PostAsJsonAsync("/api/restrictions", new
        {
            endDate = DateTime.UtcNow.AddDays(2),
            situationDescription = "Situation details",
            restrictionDescription = "Restriction details",
            cancellationDescription = "Cancellation details"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("", "Restriction details", "Cancellation details")]
    [InlineData("   ", "Restriction details", "Cancellation details")]
    [InlineData("Situation details", "", "Cancellation details")]
    [InlineData("Situation details", "   ", "Cancellation details")]
    [InlineData("Situation details", "Restriction details", "")]
    [InlineData("Situation details", "Restriction details", "   ")]
    public async Task PostWithMissingOrEmptyDescriptionsReturns400(
        string situation,
        string restriction,
        string cancellation)
    {
        var client = CreateClientFor(DbSeeder.AnnaId);

        var response = await client.PostAsJsonAsync("/api/restrictions", new
        {
            startDate = DateTime.UtcNow.AddDays(1),
            situationDescription = situation,
            restrictionDescription = restriction,
            cancellationDescription = cancellation
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetByIdOfOtherUsersDraftReturns404()
    {
        var draft = await CreateDraftAsAsync(DbSeeder.AnnaId);

        var response = await CreateClientFor(DbSeeder.LarsId).GetAsync($"/api/restrictions/{draft.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetByIdOfNonDraftReturns200ForAnyUser()
    {
        // Seeded Approved restriction owned by Anna.
        var response = await CreateClientFor(DbSeeder.LarsId).GetAsync("/api/restrictions/5eed0000-0000-0000-0000-000000000006");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetAllReturnsNonDraftsAndOnlyOwnDrafts()
    {
        await CreateDraftAsAsync(DbSeeder.AnnaId);

        var response = await CreateClientFor(DbSeeder.LarsId).GetAsync("/api/restrictions");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var restrictions = await ReadAsync<List<OperationalRestrictionDto>>(response);
        Assert.Contains(restrictions, x => x.Status != RestrictionStatus.Draft);
        Assert.Contains(restrictions, x => x.Status == RestrictionStatus.Draft && x.OriginatorUserId == DbSeeder.LarsId);
        Assert.DoesNotContain(restrictions, x => x.Status == RestrictionStatus.Draft && x.OriginatorUserId != DbSeeder.LarsId);
    }

    private HttpClient CreateClientFor(Guid userId)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _factory.CreateTokenFor(userId));

        return client;
    }

    private async Task<OperationalRestrictionDto> CreateDraftAsAsync(Guid userId)
    {
        var response = await CreateClientFor(userId).PostAsJsonAsync("/api/restrictions", new
        {
            startDate = DateTime.UtcNow.AddDays(1),
            endDate = DateTime.UtcNow.AddDays(2),
            situationDescription = "Situation details",
            restrictionDescription = "Restriction details",
            cancellationDescription = "Cancellation details"
        });
        response.EnsureSuccessStatusCode();

        return await ReadAsync<OperationalRestrictionDto>(response);
    }

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<T>(JsonOptions))!;
}
