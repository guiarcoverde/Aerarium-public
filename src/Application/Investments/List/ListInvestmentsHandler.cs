namespace Aerarium.Application.Investments.List;

using Aerarium.Application.Common;
using Aerarium.Application.Investments.Create;
using Aerarium.Domain.Common;
using Mediator;
using Microsoft.EntityFrameworkCore;

public sealed class ListInvestmentsHandler(
    IAppDbContext dbContext,
    ICurrentUserService currentUser) : IQueryHandler<ListInvestmentsQuery, Result<PagedResult<InvestmentDto>>>
{
    public async ValueTask<Result<PagedResult<InvestmentDto>>> Handle(
        ListInvestmentsQuery query,
        CancellationToken cancellationToken)
    {
        var dbQuery = dbContext.Investments
            .AsNoTracking()
            .Where(i => i.UserId == currentUser.UserId);

        var totalCount = await dbQuery.CountAsync(cancellationToken);

        var investments = await dbQuery
            .OrderByDescending(i => i.PurchaseDate)
            .ThenByDescending(i => i.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        var items = investments.Select(CreateInvestmentHandler.ToDto).ToList();
        return new PagedResult<InvestmentDto>(items, totalCount, query.Page, query.PageSize);
    }
}
