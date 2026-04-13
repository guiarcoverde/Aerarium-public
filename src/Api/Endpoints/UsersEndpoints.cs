namespace Aerarium.Api.Endpoints;

using System.Security.Claims;
using Aerarium.Api.Contracts;
using Aerarium.Infrastructure.Auth;
using Microsoft.AspNetCore.Identity;

public static class UsersEndpoints
{
    public static void MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/users")
            .WithTags("Users")
            .RequireAuthorization();

        group.MapGet("/me", async (
            ClaimsPrincipal principal,
            UserManager<ApplicationUser> userManager) =>
        {
            var user = await GetCurrentUserAsync(principal, userManager);
            if (user is null)
                return Results.Problem(title: "User not found.", statusCode: StatusCodes.Status404NotFound);

            return Results.Ok(ToResponse(user));
        });

        group.MapPut("/me", async (
            UpdateProfileRequest request,
            ClaimsPrincipal principal,
            UserManager<ApplicationUser> userManager) =>
        {
            var validation = ValidateProfile(request);
            if (validation is not null)
                return Results.ValidationProblem(validation);

            var user = await GetCurrentUserAsync(principal, userManager);
            if (user is null)
                return Results.Problem(title: "User not found.", statusCode: StatusCodes.Status404NotFound);

            user.FirstName = Trim(request.FirstName);
            user.LastName = Trim(request.LastName);
            user.DateOfBirth = request.DateOfBirth;
            user.PhoneNumber = Trim(request.PhoneNumber);

            var result = await userManager.UpdateAsync(user);
            if (!result.Succeeded)
                return Results.ValidationProblem(IdentityErrors(result));

            return Results.Ok(ToResponse(user));
        });

        group.MapPost("/me/change-password", async (
            ChangePasswordRequest request,
            ClaimsPrincipal principal,
            UserManager<ApplicationUser> userManager) =>
        {
            if (string.IsNullOrWhiteSpace(request.CurrentPassword) || string.IsNullOrWhiteSpace(request.NewPassword))
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    { "Password", new[] { "Current and new password are required." } }
                });

            var user = await GetCurrentUserAsync(principal, userManager);
            if (user is null)
                return Results.Problem(title: "User not found.", statusCode: StatusCodes.Status404NotFound);

            var result = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
            if (!result.Succeeded)
                return Results.ValidationProblem(IdentityErrors(result));

            return Results.NoContent();
        });
    }

    private static Task<ApplicationUser?> GetCurrentUserAsync(
        ClaimsPrincipal principal,
        UserManager<ApplicationUser> userManager)
    {
        var id = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return id is null ? Task.FromResult<ApplicationUser?>(null) : userManager.FindByIdAsync(id);
    }

    private static UserProfileResponse ToResponse(ApplicationUser user) =>
        new(user.Id, user.Email!, user.FirstName, user.LastName, user.DateOfBirth, user.PhoneNumber);

    private static Dictionary<string, string[]>? ValidateProfile(UpdateProfileRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (request.FirstName is { Length: > 100 })
            errors["FirstName"] = new[] { "First name must be at most 100 characters." };

        if (request.LastName is { Length: > 100 })
            errors["LastName"] = new[] { "Last name must be at most 100 characters." };

        if (request.DateOfBirth is not null && request.DateOfBirth > DateOnly.FromDateTime(DateTime.UtcNow))
            errors["DateOfBirth"] = new[] { "Date of birth cannot be in the future." };

        if (request.PhoneNumber is { Length: > 30 })
            errors["PhoneNumber"] = new[] { "Phone number must be at most 30 characters." };

        return errors.Count == 0 ? null : errors;
    }

    private static Dictionary<string, string[]> IdentityErrors(IdentityResult result) =>
        new() { { "Identity", result.Errors.Select(e => e.Description).ToArray() } };

    private static string? Trim(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
