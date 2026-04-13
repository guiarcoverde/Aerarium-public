namespace Aerarium.IntegrationTests.Endpoints;

using System.Net;
using System.Net.Http.Json;
using Aerarium.Api.Contracts;
using Aerarium.IntegrationTests.Infrastructure;
using FluentAssertions;

public sealed class AuthEndpointTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task Register_NewUser_ReturnsAccessAndRefreshTokens()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            Email = $"reg-{Guid.NewGuid():N}@test.com",
            Password = "Test@12345"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        body.Should().NotBeNull();
        body!.AccessToken.Should().NotBeNullOrWhiteSpace();
        body.RefreshToken.Should().NotBeNullOrWhiteSpace();
        body.ExpiresIn.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsTokens()
    {
        var client = factory.CreateClient();
        var email = $"login-{Guid.NewGuid():N}@test.com";

        await client.PostAsJsonAsync("/api/auth/register", new { Email = email, Password = "Test@12345" });

        var response = await client.PostAsJsonAsync("/api/auth/login", new { Email = email, Password = "Test@12345" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        body!.AccessToken.Should().NotBeNullOrWhiteSpace();
        body.RefreshToken.Should().NotBeNullOrWhiteSpace();
        body.Email.Should().Be(email);
    }

    [Fact]
    public async Task Login_InvalidCredentials_ReturnsUnauthorized()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            Email = "nobody@test.com",
            Password = "WrongPass1!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_ValidRefreshToken_RotatesAndReturnsNewPair()
    {
        var client = factory.CreateClient();
        var email = $"refresh-{Guid.NewGuid():N}@test.com";

        var register = await client.PostAsJsonAsync("/api/auth/register", new { Email = email, Password = "Test@12345" });
        var initial = await register.Content.ReadFromJsonAsync<AuthResponse>();

        var response = await client.PostAsJsonAsync("/api/auth/refresh", new { RefreshToken = initial!.RefreshToken });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var refreshed = await response.Content.ReadFromJsonAsync<AuthResponse>();
        refreshed!.AccessToken.Should().NotBeNullOrWhiteSpace();
        refreshed.RefreshToken.Should().NotBe(initial.RefreshToken);
        refreshed.Email.Should().Be(email);
    }

    [Fact]
    public async Task Refresh_RevokedToken_ReturnsUnauthorized()
    {
        var client = factory.CreateClient();
        var email = $"revoked-{Guid.NewGuid():N}@test.com";

        var register = await client.PostAsJsonAsync("/api/auth/register", new { Email = email, Password = "Test@12345" });
        var initial = await register.Content.ReadFromJsonAsync<AuthResponse>();

        // First refresh rotates and revokes the original.
        await client.PostAsJsonAsync("/api/auth/refresh", new { RefreshToken = initial!.RefreshToken });

        // Reusing the original token must fail.
        var response = await client.PostAsJsonAsync("/api/auth/refresh", new { RefreshToken = initial.RefreshToken });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_InvalidToken_ReturnsUnauthorized()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/refresh", new { RefreshToken = "not-a-real-token" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_AuthenticatedUser_RevokesRefreshToken()
    {
        var client = factory.CreateClient();
        var email = $"logout-{Guid.NewGuid():N}@test.com";

        var register = await client.PostAsJsonAsync("/api/auth/register", new { Email = email, Password = "Test@12345" });
        var initial = await register.Content.ReadFromJsonAsync<AuthResponse>();

        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", initial!.AccessToken);

        var logout = await client.PostAsJsonAsync("/api/auth/logout", new { RefreshToken = initial.RefreshToken });
        logout.StatusCode.Should().Be(HttpStatusCode.NoContent);

        client.DefaultRequestHeaders.Authorization = null;
        var refresh = await client.PostAsJsonAsync("/api/auth/refresh", new { RefreshToken = initial.RefreshToken });
        refresh.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_Unauthenticated_ReturnsUnauthorized()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/logout", new { RefreshToken = "anything" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_AuthenticatedUser_ReturnsClaims()
    {
        var client = await factory.CreateAuthenticatedClientAsync(userId: "me-user", email: "me@test.com");

        var response = await client.GetAsync("/api/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<MeResponse>();
        body!.Id.Should().Be("me-user");
        body.Email.Should().Be("me@test.com");
    }

    [Fact]
    public async Task Me_Unauthenticated_ReturnsUnauthorized()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private sealed record MeResponse(string Id, string Email);
}
