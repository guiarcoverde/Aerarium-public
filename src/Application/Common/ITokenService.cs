namespace Aerarium.Application.Common;

using Aerarium.Domain.Common;

public interface ITokenService
{
    Task<TokenPair> IssueTokensAsync(string userId, string email, CancellationToken cancellationToken = default);

    Task<Result<RefreshResult>> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default);

    Task<bool> RevokeAsync(string refreshToken, CancellationToken cancellationToken = default);
}

public sealed record TokenPair(string AccessToken, string RefreshToken, int ExpiresInSeconds);

public sealed record RefreshResult(TokenPair Tokens, string Email);
