namespace Aerarium.Application.BankAccounts;

public sealed record BankAccountDto(
    Guid Id,
    string Name,
    decimal Balance,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
