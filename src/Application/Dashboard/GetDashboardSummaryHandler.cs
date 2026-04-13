namespace Aerarium.Application.Dashboard;

using Aerarium.Application.Common;
using Aerarium.Domain.Common;
using Aerarium.Domain.Enums;
using Mediator;
using Microsoft.EntityFrameworkCore;

public sealed class GetDashboardSummaryHandler(
    IAppDbContext dbContext,
    ICurrentUserService currentUser,
    ICategoryLocalizer categoryLocalizer) : IQueryHandler<GetDashboardSummaryQuery, Result<DashboardSummaryDto>>
{
    public async ValueTask<Result<DashboardSummaryDto>> Handle(
        GetDashboardSummaryQuery query,
        CancellationToken cancellationToken)
    {
        var transactions = await dbContext.Transactions
            .AsNoTracking()
            .Where(t => t.UserId == currentUser.UserId
                        && t.Date.Month == query.Month
                        && t.Date.Year == query.Year)
            .GroupBy(t => new { t.Type, t.Category })
            .Select(g => new
            {
                g.Key.Type,
                g.Key.Category,
                Total = g.Sum(t => t.Amount),
                Count = g.Count()
            })
            .ToListAsync(cancellationToken);

        var incomeByCategory = transactions
            .Where(t => t.Type == TransactionType.Income)
            .Select(t => new CategoryBreakdownDto(
                t.Category,
                categoryLocalizer.GetDisplayName(t.Category),
                t.Total,
                t.Count))
            .ToList();

        var expenseByCategory = transactions
            .Where(t => t.Type == TransactionType.Expense)
            .Select(t => new CategoryBreakdownDto(
                t.Category,
                categoryLocalizer.GetDisplayName(t.Category),
                t.Total,
                t.Count))
            .ToList();

        var totalIncome = incomeByCategory.Sum(c => c.Total);
        var totalExpenses = expenseByCategory.Sum(c => c.Total);

        var totalInvested = await dbContext.Investments
            .AsNoTracking()
            .Where(i => i.UserId == currentUser.UserId)
            .SumAsync(i => (decimal?)i.CurrentValue, cancellationToken) ?? 0m;

        return new DashboardSummaryDto(
            totalIncome,
            totalExpenses,
            totalIncome - totalExpenses,
            totalInvested,
            incomeByCategory,
            expenseByCategory);
    }
}
