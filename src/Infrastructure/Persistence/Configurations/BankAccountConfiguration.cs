using Aerarium.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aerarium.Infrastructure.Persistence.Configurations;

public sealed class BankAccountConfiguration : IEntityTypeConfiguration<BankAccount>
{
    public void Configure(EntityTypeBuilder<BankAccount> builder)
    {
        builder.ToTable("BankAccounts");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.UserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(b => b.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(b => b.Balance)
            .HasPrecision(18, 2);

        builder.HasIndex(b => b.UserId);
    }
}
