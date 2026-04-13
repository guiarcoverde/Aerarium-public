namespace Aerarium.Api.Endpoints;

using System.Security.Claims;
using Aerarium.Api.Contracts;
using Aerarium.Application.Common;
using Aerarium.Infrastructure.Auth;
using Microsoft.AspNetCore.Identity;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Auth");

        group.MapPost("/register", async (
            RegisterRequest request,
            UserManager<ApplicationUser> userManager,
            ITokenService tokenService,
            CancellationToken cancellationToken) =>
        {
            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email
            };

            var result = await userManager.CreateAsync(user, request.Password);

            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description);
                return Results.ValidationProblem(
                    new Dictionary<string, string[]>
                    {
                        { "Identity", errors.ToArray() }
                    });
            }

            var tokens = await tokenService.IssueTokensAsync(user.Id, user.Email!, cancellationToken);
            return Results.Created("/api/auth", ToResponse(tokens, user.Email!));
        });

        group.MapPost("/login", async (
            LoginRequest request,
            UserManager<ApplicationUser> userManager,
            ITokenService tokenService,
            CancellationToken cancellationToken) =>
        {
            var user = await userManager.FindByEmailAsync(request.Email);

            if (user is null || !await userManager.CheckPasswordAsync(user, request.Password))
                return Results.Problem(
                    title: "Invalid credentials",
                    statusCode: StatusCodes.Status401Unauthorized);

            var tokens = await tokenService.IssueTokensAsync(user.Id, user.Email!, cancellationToken);
            return Results.Ok(ToResponse(tokens, user.Email!));
        });

        group.MapPost("/refresh", async (
            RefreshTokenRequest request,
            ITokenService tokenService,
            CancellationToken cancellationToken) =>
        {
            var result = await tokenService.RefreshAsync(request.RefreshToken, cancellationToken);

            if (result.IsFailure)
                return Results.Problem(title: result.Error, statusCode: StatusCodes.Status401Unauthorized);

            var refreshed = result.Value!;
            return Results.Ok(ToResponse(refreshed.Tokens, refreshed.Email));
        });

        group.MapPost("/logout", async (
            RefreshTokenRequest request,
            ITokenService tokenService,
            CancellationToken cancellationToken) =>
        {
            await tokenService.RevokeAsync(request.RefreshToken, cancellationToken);
            return Results.NoContent();
        }).RequireAuthorization();

        group.MapGet("/me", (ClaimsPrincipal user) =>
        {
            var id = user.FindFirstValue(ClaimTypes.NameIdentifier);
            var email = user.FindFirstValue(ClaimTypes.Email);
            return Results.Ok(new { id, email });
        }).RequireAuthorization();
    }

    private static AuthResponse ToResponse(TokenPair tokens, string email) =>
        new(tokens.AccessToken, tokens.RefreshToken, tokens.ExpiresInSeconds, email);
}
