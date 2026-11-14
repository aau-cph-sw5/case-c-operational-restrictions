using System.Net;
using System.Net.Http.Json;

using Backend.Data;
using Backend.Models.DTOs;

namespace Backend.Tests;

public class AuthControllerTests : IAsyncLifetime
{
    private readonly TestWebApplicationFactory _factory = new();

    public async ValueTask InitializeAsync() => await _factory.SeedAsync();

    public async ValueTask DisposeAsync()
    {
        await _factory.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task LoginAndTokenCheckUseSeededDatabaseUser()
    {
        var client = _factory.CreateClient();

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto
        {
            Email = "anna.jensen@example.test",
            Password = DbSeeder.SeedPassword
        }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var login = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>(
            TestContext.Current.CancellationToken);
        Assert.NotNull(login);
        Assert.Equal("anna.jensen@example.test", login.User.Email);

        var tokenResponse = await client.PostAsync(
            $"/api/auth/check-token/{login.AccessToken}",
            content: null,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, tokenResponse.StatusCode);
        var user = await tokenResponse.Content.ReadFromJsonAsync<UserDto>(
            TestContext.Current.CancellationToken);
        Assert.NotNull(user);
        Assert.Equal(DbSeeder.AnnaId, user.Id);
    }

    [Fact]
    public async Task TokenForUserMissingFromDatabaseIsRejected()
    {
        var client = _factory.CreateClient();
        var token = _factory.CreateTokenFor(Guid.NewGuid());

        var response = await client.PostAsync(
            $"/api/auth/check-token/{token}",
            content: null,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}