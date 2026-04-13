namespace Aerarium.Application.Transactions.List;

using Aerarium.Application.Common;
using Aerarium.Domain.Common;
using Aerarium.Domain.Enums;
using Mediator;
using Microsoft.EntityFrameworkCore;

public sealed class ListTransactionsHandler(
    IAppDbContext dbContext,
    ICurrentUserService currentUser,
    ICategoryLocalizer categoryLocalizer,
    IPaymentMethodLocalizer paymentMethodLocalizer) : IQueryHandler<ListTransactionsQuery, Result<PagedResult<TransactionDto>>>
{
    public async ValueTask<Result<PagedResult<TransactionDto>>> Handle(
        ListTransactionsQuery query,
        CancellationToken cancellationToken)
    {
        var dbQuery = dbContext.Transactions
            .AsNoTracking()
            .Where(t => t.UserId == currentUser.UserId);

        if (query.Month.HasValue)
            dbQuery = dbQuery.Where(t => t.Date.Month == query.Month.Value);

        if (query.Year.HasValue)
            dbQuery = dbQuery.Where(t => t.Date.Year == query.Year.Value);

        if (query.Type.HasValue)
            dbQuery = dbQuery.Where(t => t.Type == query.Type.Value);

        if (query.Category.HasValue)
            dbQuery = dbQuery.Where(t => t.Category == query.Category.Value);

        var totalCount = await dbQuery.CountAsync(cancellationToken);

        var transactions = await dbQuery
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        var bankIds = transactions
            .Where(t => t.BankAccountId is not null)
            .Select(t => t.BankAccountId!.Value)
            .Distinct()
            .ToList();

        var cardIds = transactions
            .Where(t => t.CardId is not null)
            .Select(t => t.CardId!.Value)
            .Distinct()
            .ToList();

        var bankNames = bankIds.Count > 0
            ? await dbContext.BankAccounts
                .Where(b => bankIds.Contains(b.Id))
                .ToDictionaryAsync(b => b.Id, b => b.Name, cancellationToken)
            : new Dictionary<Guid, string>();

        var cardNames = cardIds.Count > 0
            ? await dbContext.Cards
                .Where(c => cardIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, c => c.Name, cancellationToken)
            : new Dictionary<Guid, string>();

        var items = transactions
            .Select(t => new TransactionDto(
                t.Id,
                t.Amount,
                t.Description,
                t.Date,
                t.Type,
                t.Category,
                categoryLocalizer.GetDisplayName(t.Category),
                t.PaymentMethod,
                t.PaymentMethod.HasValue ? paymentMethodLocalizer.GetDisplayName(t.PaymentMethod.Value) : null,
                t.Recurrence,
                t.RecurrenceGroupId,
                t.RecurrenceEndDate,
                t.RecurrenceCount,
                t.SalarySchedule != null ? new SalaryScheduleDto(
                    t.SalarySchedule.Mode, t.SalarySchedule.BusinessDayNumber,
                    t.SalarySchedule.FixedDay, t.SalarySchedule.SplitFirstAmount,
                    t.SalarySchedule.SplitFirstPercentage) : null,
                t.BankAccountId,
                t.BankAccountId is not null && bankNames.TryGetValue(t.BankAccountId.Value, out var bn) ? bn : null,
                t.CardId,
                t.CardId is not null && cardNames.TryGetValue(t.CardId.Value, out var cn) ? cn : null,
                t.CreatedAt,
                t.UpdatedAt))
            .ToList();

        return new PagedResult<TransactionDto>(items, totalCount, query.Page, query.PageSize);
    }
}
