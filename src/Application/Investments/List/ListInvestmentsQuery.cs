namespace Aerarium.Application.Investments.List;

using Aerarium.Application.Common;
using Aerarium.Domain.Common;
using Mediator;

public sealed record ListInvestmentsQuery(int Page, int PageSize)
    : IQuery<Result<PagedResult<InvestmentDto>>>;
