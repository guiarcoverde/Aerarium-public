namespace Aerarium.Application.Investments.Create;

using Aerarium.Domain.Common;
using Aerarium.Domain.Enums;
using Mediator;

public sealed record CreateInvestmentCommand(
    string Name,
    decimal Amount,
    decimal CurrentValue,
    InvestmentType Type,
    DateOnly PurchaseDate) : ICommand<Result<InvestmentDto>>;
