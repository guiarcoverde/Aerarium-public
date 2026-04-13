namespace Aerarium.Infrastructure.Persistence.Configurations;

using Aerarium.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("Transactions");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Amount)
            .HasPrecision(18, 2);

        builder.Property(t => t.Description)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(t => t.UserId)
            .HasMaxLength(450)
            .IsRequired();

        builder.Property(t => t.Type)
            .HasConversion<int>();

        builder.Property(t => t.Category)
            .HasConversion<int>();

        builder.Property(t => t.Recurrence)
            .HasConversion<int>();

        builder.Property(t => t.PaymentMethod)
            .HasConversion<int?>();

        builder.HasIndex(t => t.UserId);

        builder.HasIndex(t => new { t.UserId, t.Date });

        builder.HasIndex(t => t.RecurrenceGroupId)
            .HasFilter("\"RecurrenceGroupId\" IS NOT NULL");

        builder.OwnsOne(t => t.SalarySchedule, sa =>
        {
            sa.Property(s => s.Mode).HasConversion<int>();
            sa.Property(s => s.BusinessDayNumber);
            sa.Property(s => s.FixedDay);
            sa.Property(s => s.SplitFirstAmount).HasPrecision(18, 2);
            sa.Property(s => s.SplitFirstPercentage).HasPrecision(5, 2);
        });

        builder.HasIndex(t => t.BankAccountId);
        builder.HasIndex(t => t.CardId);

        builder.HasOne<BankAccount>()
            .WithMany()
            .HasForeignKey(t => t.BankAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Card>()
            .WithMany()
            .HasForeignKey(t => t.CardId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
