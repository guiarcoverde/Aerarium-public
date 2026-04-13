namespace Aerarium.Application.Investments.Update;

using Aerarium.Domain.Common;
using Aerarium.Domain.Enums;
using Mediator;

public sealed record UpdateInvestmentCommand(
    Guid Id,
    string Name,
    decimal Amount,
    decimal CurrentValue,
    InvestmentType Type,
    DateOnly PurchaseDate) : ICommand<Result<InvestmentDto>>;
