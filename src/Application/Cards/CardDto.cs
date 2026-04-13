namespace Aerarium.Application.Cards;

using Aerarium.Domain.Enums;

public sealed record CardDto(
    Guid Id,
    string Name,
    decimal CreditLimit,
    decimal AvailableLimit,
    CardType Type,
    Guid? LinkedBankAccountId,
    string? LinkedBankAccountName,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
