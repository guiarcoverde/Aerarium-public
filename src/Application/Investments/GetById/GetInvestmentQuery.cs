namespace Aerarium.Application.Investments.GetById;

using Aerarium.Domain.Common;
using Mediator;

public sealed record GetInvestmentQuery(Guid Id) : IQuery<Result<InvestmentDto>>;
