namespace Aerarium.Application.Transactions.GetById;

using Aerarium.Application.Common;
using Aerarium.Domain.Common;
using Aerarium.Domain.ValueObjects;
using Mediator;
using Microsoft.EntityFrameworkCore;

public sealed class GetTransactionHandler(
    IAppDbContext dbContext,
    ICurrentUserService currentUser,
    ICategoryLocalizer categoryLocalizer,
    IPaymentMethodLocalizer paymentMethodLocalizer) : IQueryHandler<GetTransactionQuery, Result<TransactionDto>>
{
    public async ValueTask<Result<TransactionDto>> Handle(
        GetTransactionQuery query,
        CancellationToken cancellationToken)
    {
        var transaction = await dbContext.Transactions
            .AsNoTracking()
            .FirstOrDefaultAsync(
                t => t.Id == query.Id && t.UserId == currentUser.UserId,
                cancellationToken);

        if (transaction is null)
            return Result<TransactionDto>.Failure("Transaction not found.");

        string? bankName = null;
        string? cardName = null;

        if (transaction.BankAccountId is not null)
        {
            bankName = await dbContext.BankAccounts
                .Where(b => b.Id == transaction.BankAccountId)
                .Select(b => b.Name)
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (transaction.CardId is not null)
        {
            cardName = await dbContext.Cards
                .Where(c => c.Id == transaction.CardId)
                .Select(c => c.Name)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return new TransactionDto(
            transaction.Id,
            transaction.Amount,
            transaction.Description,
            transaction.Date,
            transaction.Type,
            transaction.Category,
            categoryLocalizer.GetDisplayName(transaction.Category),
            transaction.PaymentMethod,
            transaction.PaymentMethod.HasValue ? paymentMethodLocalizer.GetDisplayName(transaction.PaymentMethod.Value) : null,
            transaction.Recurrence,
            transaction.RecurrenceGroupId,
            transaction.RecurrenceEndDate,
            transaction.RecurrenceCount,
            MapSalarySchedule(transaction.SalarySchedule),
            transaction.BankAccountId,
            bankName,
            transaction.CardId,
            cardName,
            transaction.CreatedAt,
            transaction.UpdatedAt);
    }

    private static SalaryScheduleDto? MapSalarySchedule(SalarySchedule? schedule)
    {
        if (schedule is null) return null;
        return new SalaryScheduleDto(
            schedule.Mode, schedule.BusinessDayNumber, schedule.FixedDay,
            schedule.SplitFirstAmount, schedule.SplitFirstPercentage);
    }
}
