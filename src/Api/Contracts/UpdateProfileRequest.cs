namespace Aerarium.Api.Contracts;

public sealed record UpdateProfileRequest(
    string? FirstName,
    string? LastName,
    DateOnly? DateOfBirth,
    string? PhoneNumber);
