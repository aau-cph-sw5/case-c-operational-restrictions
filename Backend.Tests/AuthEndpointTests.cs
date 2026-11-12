using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

using Backend.Data;
using Backend.Models.DTOs;
using Backend.Models.Enums;

namespace Backend.Tests;

public class AuthEndpointTests : IAsyncLifetime, IDisposable
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

    [Theory]
    [InlineData("originator@example.test", UserRole.Originator)]
    [InlineData("anna.jensen@example.test", UserRole.DutyOperationsManager)]
    [InlineData("operator@example.test", UserRole.Operator)]
    [InlineData("mikkel.pedersen@example.test", UserRole.ControlroomSupervisor)]
    [InlineData("camilla.andersen@example.test", UserRole.Betriebsleiter)]
    public async Task SeededUsersCanLoginWithSeedPassword(string email, UserRole expectedRole)
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto
        {
            Email = email,
            Password = DbSeeder.SeedPassword
        }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadFromJsonAsync<LoginResponseDto>(JsonOptions, TestContext.Current.CancellationToken);
        Assert.NotNull(content);
        Assert.False(string.IsNullOrWhiteSpace(content.AccessToken));
        Assert.Equal(email, content.User.Email);
        Assert.Contains(expectedRole.ToString(), content.User.Roles);
    }

    [Fact]
    public async Task LoginWithInvalidPasswordReturns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto
        {
            Email = "operator@example.test",
            Password = "WrongPassword123!"
        }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task LoginWithUnknownEmailReturns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto
        {
            Email = "nonexistent.user@example.test",
            Password = DbSeeder.SeedPassword
        }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CheckValidityOfTokenReturnsSeededUserWithRole()
    {
        var client = _factory.CreateClient();

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto
        {
            Email = "anna.jensen@example.test",
            Password = DbSeeder.SeedPassword
        }, TestContext.Current.CancellationToken);

        var loginContent = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>(JsonOptions, TestContext.Current.CancellationToken);
        Assert.NotNull(loginContent);

        var checkResponse = await client.PostAsync($"/api/auth/check-token/{Uri.EscapeDataString(loginContent.AccessToken)}", null, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, checkResponse.StatusCode);

        var checkedUser = await checkResponse.Content.ReadFromJsonAsync<UserDto>(JsonOptions, TestContext.Current.CancellationToken);
        Assert.NotNull(checkedUser);
        Assert.Equal("anna.jensen@example.test", checkedUser.Email);
        Assert.Contains("DutyOperationsManager", checkedUser.Roles);
    }

    [Fact]
    public async Task CheckValidityOfTokenWithInvalidTokenReturns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync("/api/auth/check-token/invalid.token.structure", null, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
