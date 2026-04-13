using Aerarium.Domain.Common;
using Aerarium.Domain.Enums;

namespace Aerarium.Domain.Entities;

public sealed class Card
{
    public Guid Id { get; private set; }
    public string UserId { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public decimal CreditLimit { get; private set; }
    public decimal AvailableLimit { get; private set; }
    public CardType Type { get; private set; }
    public Guid? LinkedBankAccountId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private Card() { }

    public static Result<Card> Create(string userId, string name, decimal creditLimit, CardType type, Guid? linkedBankAccountId)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result<Card>.Failure("Name is required.");

        if ((type & (CardType.Debit | CardType.Credit)) == 0)
            return Result<Card>.Failure("Card must be debit, credit, or both.");

        if (type.HasFlag(CardType.Credit) && creditLimit <= 0)
            return Result<Card>.Failure("Credit limit must be greater than zero.");

        if (type.HasFlag(CardType.Debit) && linkedBankAccountId is null)
            return Result<Card>.Failure("A bank account is required for debit cards.");

        var limit = type.HasFlag(CardType.Credit) ? creditLimit : 0m;

        return new Card
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            Name = name.Trim(),
            CreditLimit = limit,
            AvailableLimit = limit,
            Type = type,
            LinkedBankAccountId = type.HasFlag(CardType.Debit) ? linkedBankAccountId : null,
            CreatedAt = DateTime.UtcNow,
        };
    }

    public Result<Card> ConsumeLimit(decimal amount)
    {
        if (!Type.HasFlag(CardType.Credit))
            return Result<Card>.Failure("Card does not support credit operations.");

        if (amount <= 0)
            return Result<Card>.Failure("Amount must be greater than zero.");

        if (amount > AvailableLimit)
            return Result<Card>.Failure("Insufficient available limit.");

        AvailableLimit -= amount;
        UpdatedAt = DateTime.UtcNow;
        return this;
    }

    public Result<Card> RestoreLimit(decimal amount)
    {
        if (!Type.HasFlag(CardType.Credit))
            return Result<Card>.Failure("Card does not support credit operations.");

        if (amount <= 0)
            return Result<Card>.Failure("Amount must be greater than zero.");

        if (AvailableLimit + amount > CreditLimit)
            return Result<Card>.Failure("Restored amount would exceed credit limit.");

        AvailableLimit += amount;
        UpdatedAt = DateTime.UtcNow;
        return this;
    }

    public Result<Card> Update(string name, CardType type, decimal creditLimit, Guid? linkedBankAccountId)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result<Card>.Failure("Name is required.");

        if ((type & (CardType.Debit | CardType.Credit)) == 0)
            return Result<Card>.Failure("Card must be debit, credit, or both.");

        if (type.HasFlag(CardType.Credit) && creditLimit <= 0)
            return Result<Card>.Failure("Credit limit must be greater than zero.");

        if (type.HasFlag(CardType.Debit) && linkedBankAccountId is null)
            return Result<Card>.Failure("A bank account is required for debit cards.");

        if (type.HasFlag(CardType.Credit))
        {
            var consumed = CreditLimit - AvailableLimit;
            if (creditLimit < consumed)
                return Result<Card>.Failure("New credit limit cannot be lower than the amount already consumed.");

            CreditLimit = creditLimit;
            AvailableLimit = creditLimit - consumed;
        }
        else
        {
            CreditLimit = 0m;
            AvailableLimit = 0m;
        }

        Name = name.Trim();
        Type = type;
        LinkedBankAccountId = type.HasFlag(CardType.Debit) ? linkedBankAccountId : null;
        UpdatedAt = DateTime.UtcNow;

        return this;
    }

    public Result<Card> EnsureCanBeDeleted()
    {
        if (Type.HasFlag(CardType.Credit) && AvailableLimit < CreditLimit)
            return Result<Card>.Failure("Cannot delete a card with consumed credit limit. Settle outstanding charges first.");

        return this;
    }
}
