namespace Aerarium.Domain.Entities;

public sealed class RefreshToken
{
    public Guid Id { get; private set; }
    public string UserId { get; private set; } = null!;
    public string TokenHash { get; private set; } = null!;
    public DateTime ExpiresAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public Guid? ReplacedByTokenId { get; private set; }

    private RefreshToken() { }

    public static RefreshToken Issue(string userId, string tokenHash, TimeSpan lifetime)
    {
        return new RefreshToken
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.Add(lifetime),
            CreatedAt = DateTime.UtcNow
        };
    }

    public bool IsActive => RevokedAt is null && DateTime.UtcNow < ExpiresAt;

    public void Revoke(Guid? replacedByTokenId = null)
    {
        if (RevokedAt is not null) return;
        RevokedAt = DateTime.UtcNow;
        ReplacedByTokenId = replacedByTokenId;
    }
}
