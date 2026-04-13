namespace Aerarium.Api.Contracts;

public sealed record UserProfileResponse(
    string Id,
    string Email,
    string? FirstName,
    string? LastName,
    DateOnly? DateOfBirth,
    string? PhoneNumber);
