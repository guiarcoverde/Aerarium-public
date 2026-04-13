namespace Aerarium.Infrastructure.Auth;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Aerarium.Application.Common;
using Aerarium.Domain.Common;
using Aerarium.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

public sealed class TokenService(
    IConfiguration configuration,
    IAppDbContext dbContext,
    UserManager<ApplicationUser> userManager) : ITokenService
{
    private const int RefreshTokenByteLength = 64;

    public async Task<TokenPair> IssueTokensAsync(
        string userId,
        string email,
        CancellationToken cancellationToken = default)
    {
        var (accessToken, expiresInSeconds) = GenerateAccessToken(userId, email);
        var refreshToken = await IssueRefreshTokenAsync(userId, cancellationToken);
        return new TokenPair(accessToken, refreshToken, expiresInSeconds);
    }

    public async Task<Result<RefreshResult>> RefreshAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return Result<RefreshResult>.Failure("Refresh token is required.");

        var hash = HashToken(refreshToken);
        var stored = await dbContext.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);

        if (stored is null)
            return Result<RefreshResult>.Failure("Invalid refresh token.");

        if (!stored.IsActive)
            return Result<RefreshResult>.Failure("Refresh token is no longer valid.");

        var user = await userManager.FindByIdAsync(stored.UserId);
        if (user is null)
            return Result<RefreshResult>.Failure("User no longer exists.");

        var (accessToken, expiresInSeconds) = GenerateAccessToken(user.Id, user.Email!);

        var newPlainText = GenerateOpaqueToken();
        var newEntity = RefreshToken.Issue(user.Id, HashToken(newPlainText), GetRefreshLifetime());
        dbContext.RefreshTokens.Add(newEntity);

        stored.Revoke(newEntity.Id);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new RefreshResult(new TokenPair(accessToken, newPlainText, expiresInSeconds), user.Email!);
    }

    public async Task<bool> RevokeAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return false;

        var hash = HashToken(refreshToken);
        var stored = await dbContext.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);

        if (stored is null || stored.RevokedAt is not null)
            return false;

        stored.Revoke();
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private (string token, int expiresInSeconds) GenerateAccessToken(string userId, string email)
    {
        var jwtSettings = configuration.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var minutes = int.Parse(jwtSettings["AccessTokenMinutes"] ?? "15");

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Email, email)
        };

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(minutes),
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), minutes * 60);
    }

    private async Task<string> IssueRefreshTokenAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        var plainText = GenerateOpaqueToken();
        var entity = RefreshToken.Issue(userId, HashToken(plainText), GetRefreshLifetime());

        dbContext.RefreshTokens.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return plainText;
    }

    private TimeSpan GetRefreshLifetime()
    {
        var days = int.Parse(configuration.GetSection("Jwt")["RefreshTokenDays"] ?? "7");
        return TimeSpan.FromDays(days);
    }

    private static string GenerateOpaqueToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(RefreshTokenByteLength);
        return Base64UrlEncoder.Encode(bytes);
    }

    private static string HashToken(string token)
    {
        var bytes = Encoding.UTF8.GetBytes(token);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }
}
