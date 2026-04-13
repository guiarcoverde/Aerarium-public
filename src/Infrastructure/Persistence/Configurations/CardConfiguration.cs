using Aerarium.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aerarium.Infrastructure.Persistence.Configurations;

public sealed class CardConfiguration : IEntityTypeConfiguration<Card>
{
    public void Configure(EntityTypeBuilder<Card> builder)
    {
        builder.ToTable("Cards");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.UserId)
            .HasMaxLength(450)
            .IsRequired();

        builder.Property(c => c.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(c => c.Type)
            .HasConversion<int>();

        builder.Property(c => c.CreditLimit)
            .HasPrecision(18, 2);

        builder.Property(c => c.AvailableLimit)
            .HasPrecision(18, 2);

        builder.HasIndex(c => c.LinkedBankAccountId);
        builder.HasOne<BankAccount>()
            .WithMany()
            .HasForeignKey(c => c.LinkedBankAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => c.UserId);

        builder.HasIndex(c => new { c.UserId, c.CreatedAt });
    }
}
