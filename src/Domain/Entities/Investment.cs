namespace Aerarium.Domain.Entities;

using Aerarium.Domain.Common;
using Aerarium.Domain.Enums;

public sealed class Investment
{
    public Guid Id { get; private set; }
    public string UserId { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public decimal Amount { get; private set; }
    public decimal CurrentValue { get; private set; }
    public InvestmentType Type { get; private set; }
    public DateOnly PurchaseDate { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private Investment() { }

    public static Result<Investment> Create(
        string userId,
        string name,
        decimal amount,
        decimal currentValue,
        InvestmentType type,
        DateOnly purchaseDate)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result<Investment>.Failure("Name is required.");

        if (amount <= 0)
            return Result<Investment>.Failure("Amount must be greater than zero.");

        if (currentValue < 0)
            return Result<Investment>.Failure("Current value cannot be negative.");

        return new Investment
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            Name = name.Trim(),
            Amount = amount,
            CurrentValue = currentValue,
            Type = type,
            PurchaseDate = purchaseDate,
            CreatedAt = DateTime.UtcNow
        };
    }

    public Result<Investment> Update(
        string name,
        decimal amount,
        decimal currentValue,
        InvestmentType type,
        DateOnly purchaseDate)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result<Investment>.Failure("Name is required.");

        if (amount <= 0)
            return Result<Investment>.Failure("Amount must be greater than zero.");

        if (currentValue < 0)
            return Result<Investment>.Failure("Current value cannot be negative.");

        Name = name.Trim();
        Amount = amount;
        CurrentValue = currentValue;
        Type = type;
        PurchaseDate = purchaseDate;
        UpdatedAt = DateTime.UtcNow;

        return this;
    }
}
