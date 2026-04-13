namespace Aerarium.Api.Contracts;

public sealed record AuthResponse(string AccessToken, string RefreshToken, int ExpiresIn, string Email);
