using Aerarium.Domain.Common;

namespace Aerarium.Domain.Entities;

public sealed class BankAccount
{
    public Guid Id { get; private set; }
    public string UserId { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public decimal Balance { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private BankAccount() { }

    public static Result<BankAccount> Create(string userId, string name, decimal balance)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result<BankAccount>.Failure("Name is required.");

        return new BankAccount
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            Name = name.Trim(),
            Balance = balance,
            CreatedAt = DateTime.UtcNow,
        };
    }

    public Result<BankAccount> Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result<BankAccount>.Failure("Name is required.");

        Name = name.Trim();
        UpdatedAt = DateTime.UtcNow;
        return this;
    }

    public Result<BankAccount> Debit(decimal amount)
    {
        if (amount <= 0)
            return Result<BankAccount>.Failure("Amount must be greater than zero.");

        Balance -= amount;
        UpdatedAt = DateTime.UtcNow;
        return this;
    }

    public Result<BankAccount> Credit(decimal amount)
    {
        if (amount <= 0)
            return Result<BankAccount>.Failure("Amount must be greater than zero.");

        Balance += amount;
        UpdatedAt = DateTime.UtcNow;
        return this;
    }

    public Result<BankAccount> EnsureCanBeDeleted()
    {
        return this;
    }
}
