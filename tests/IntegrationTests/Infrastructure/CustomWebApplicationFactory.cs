namespace Aerarium.IntegrationTests.Infrastructure;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Aerarium.Infrastructure.Auth;
using Aerarium.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Testcontainers.PostgreSql;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17")
        .Build();

    private const string TestJwtKey = "IntegrationTestKeyThatIsLongEnoughForHmacSha256Validation!";
    private const string TestIssuer = "Aerarium";
    private const string TestAudience = "Aerarium";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));

            if (descriptor is not null)
                services.Remove(descriptor);

            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(_postgres.GetConnectionString()));
        });

        builder.UseSetting("Jwt:Key", TestJwtKey);
        builder.UseSetting("Jwt:Issuer", TestIssuer);
        builder.UseSetting("Jwt:Audience", TestAudience);
        builder.UseSetting("Jwt:AccessTokenMinutes", "15");
        builder.UseSetting("Jwt:RefreshTokenDays", "7");
    }

    public async Task<HttpClient> CreateAuthenticatedClientAsync(string? userId = null, string email = "test@test.com")
    {
        var client = CreateClient();

        using var scope = Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser { Id = userId ?? Guid.NewGuid().ToString(), UserName = email, Email = email };
            await userManager.CreateAsync(user, "Test@12345");
        }

        var token = GenerateToken(user.Id, email);
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        return client;
    }

    private static string GenerateToken(string userId, string email)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Email, email)
        };

        var token = new JwtSecurityToken(
            issuer: TestIssuer,
            audience: TestAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    public new async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await base.DisposeAsync();
    }
}
