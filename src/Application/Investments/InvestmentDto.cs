namespace Aerarium.Application.Investments;

using Aerarium.Domain.Enums;

public sealed record InvestmentDto(
    Guid Id,
    string Name,
    decimal Amount,
    decimal CurrentValue,
    InvestmentType Type,
    DateOnly PurchaseDate,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
