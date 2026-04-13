namespace Aerarium.Infrastructure.Persistence.Configurations;

using Aerarium.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.UserId)
            .HasMaxLength(450)
            .IsRequired();

        builder.Property(t => t.TokenHash)
            .HasMaxLength(128)
            .IsRequired();

        builder.HasIndex(t => t.TokenHash).IsUnique();

        builder.HasIndex(t => t.UserId);
    }
}
