namespace Aerarium.Infrastructure.Persistence.Configurations;

using Aerarium.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class InvestmentConfiguration : IEntityTypeConfiguration<Investment>
{
    public void Configure(EntityTypeBuilder<Investment> builder)
    {
        builder.ToTable("Investments");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.UserId)
            .HasMaxLength(450)
            .IsRequired();

        builder.Property(i => i.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(i => i.Amount)
            .HasPrecision(18, 2);

        builder.Property(i => i.CurrentValue)
            .HasPrecision(18, 2);

        builder.Property(i => i.Type)
            .HasConversion<int>();

        builder.HasIndex(i => i.UserId);
    }
}
