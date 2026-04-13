namespace Aerarium.IntegrationTests.Endpoints;

using System.Net;
using System.Net.Http.Json;
using Aerarium.Api.Contracts;
using Aerarium.IntegrationTests.Infrastructure;
using FluentAssertions;

public sealed class UsersEndpointTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task GetMe_AuthenticatedUser_ReturnsProfile()
    {
        var client = await factory.CreateAuthenticatedClientAsync(userId: "profile-user", email: "profile@test.com");

        var response = await client.GetAsync("/api/users/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var profile = await response.Content.ReadFromJsonAsync<UserProfileResponse>();
        profile.Should().NotBeNull();
        profile!.Id.Should().Be("profile-user");
        profile.Email.Should().Be("profile@test.com");
        profile.FirstName.Should().BeNull();
    }

    [Fact]
    public async Task GetMe_Unauthenticated_ReturnsUnauthorized()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/users/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateMe_ValidPayload_PersistsChanges()
    {
        var client = await factory.CreateAuthenticatedClientAsync(userId: "update-user", email: "update@test.com");

        var update = await client.PutAsJsonAsync("/api/users/me", new
        {
            FirstName = "Guilherme",
            LastName = "Cavalcanti",
            DateOfBirth = "1995-06-15",
            PhoneNumber = "+55 11 91234 5678"
        });

        update.StatusCode.Should().Be(HttpStatusCode.OK);

        var fetched = await client.GetFromJsonAsync<UserProfileResponse>("/api/users/me");
        fetched!.FirstName.Should().Be("Guilherme");
        fetched.LastName.Should().Be("Cavalcanti");
        fetched.DateOfBirth.Should().Be(new DateOnly(1995, 6, 15));
        fetched.PhoneNumber.Should().Be("+55 11 91234 5678");
    }

    [Fact]
    public async Task UpdateMe_FutureDateOfBirth_ReturnsValidationProblem()
    {
        var client = await factory.CreateAuthenticatedClientAsync(userId: "future-dob", email: "future@test.com");

        var update = await client.PutAsJsonAsync("/api/users/me", new
        {
            FirstName = "Test",
            DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1))
        });

        update.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ChangePassword_CorrectCurrent_ReturnsNoContent()
    {
        var client = await factory.CreateAuthenticatedClientAsync(userId: "pwd-user", email: "pwd@test.com");

        var response = await client.PostAsJsonAsync("/api/users/me/change-password", new
        {
            CurrentPassword = "Test@12345",
            NewPassword = "NewPass@2026"
        });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ChangePassword_WrongCurrent_ReturnsValidationProblem()
    {
        var client = await factory.CreateAuthenticatedClientAsync(userId: "pwd-wrong", email: "pwdwrong@test.com");

        var response = await client.PostAsJsonAsync("/api/users/me/change-password", new
        {
            CurrentPassword = "WrongPass1!",
            NewPassword = "NewPass@2026"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ChangePassword_Unauthenticated_ReturnsUnauthorized()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/users/me/change-password", new
        {
            CurrentPassword = "x",
            NewPassword = "y"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
