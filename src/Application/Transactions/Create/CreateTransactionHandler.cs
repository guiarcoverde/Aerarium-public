namespace Aerarium.Application.Transactions.Create;

using Aerarium.Application.Common;
using Aerarium.Domain.Common;
using Aerarium.Domain.Entities;
using Aerarium.Domain.Enums;
using Aerarium.Domain.ValueObjects;
using Mediator;
using Microsoft.EntityFrameworkCore;

public sealed class CreateTransactionHandler(
    IAppDbContext dbContext,
    ICurrentUserService currentUser,
    ICategoryLocalizer categoryLocalizer,
    IPaymentMethodLocalizer paymentMethodLocalizer,
    IBusinessDayCalendar businessDayCalendar) : ICommandHandler<CreateTransactionCommand, Result<TransactionDto>>
{
    public async ValueTask<Result<TransactionDto>> Handle(
        CreateTransactionCommand command,
        CancellationToken cancellationToken)
    {
        if (command.Recurrence != Recurrence.None)
            return await HandleRecurring(command, cancellationToken);

        var result = Transaction.Create(
            currentUser.UserId,
            command.Amount,
            command.Description,
            command.Date,
            command.Type,
            command.Category,
            salarySchedule: command.SalarySchedule,
            paymentMethod: command.PaymentMethod,
            bankAccountId: command.BankAccountId,
            cardId: command.CardId);

        if (result.IsFailure)
            return Result<TransactionDto>.Failure(result.Error!);

        var transaction = result.Value!;

        var walletResult = await ApplyWalletEffects(transaction, cancellationToken);
        if (walletResult is not null)
            return Result<TransactionDto>.Failure(walletResult);

        dbContext.Transactions.Add(transaction);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await ToDto(transaction, cancellationToken);
    }

    private async ValueTask<Result<TransactionDto>> HandleRecurring(
        CreateTransactionCommand command,
        CancellationToken cancellationToken)
    {
        var result = Transaction.CreateSeries(
            currentUser.UserId,
            command.Amount,
            command.Description,
            command.Date,
            command.Type,
            command.Category,
            command.Recurrence,
            command.RecurrenceEndDate,
            command.RecurrenceCount,
            command.SalarySchedule,
            businessDayCalendar,
            command.PaymentMethod,
            command.BankAccountId,
            command.CardId);

        if (result.IsFailure)
            return Result<TransactionDto>.Failure(result.Error!);

        var transactions = result.Value!;

        foreach (var transaction in transactions)
        {
            var walletResult = await ApplyWalletEffects(transaction, cancellationToken);
            if (walletResult is not null)
                return Result<TransactionDto>.Failure(walletResult);
        }

        dbContext.Transactions.AddRange(transactions);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await ToDto(transactions[0], cancellationToken);
    }

    private async Task<string?> ApplyWalletEffects(Transaction transaction, CancellationToken cancellationToken)
    {
        if (transaction.BankAccountId is not null)
        {
            var bank = await dbContext.BankAccounts
                .FirstOrDefaultAsync(b => b.Id == transaction.BankAccountId && b.UserId == currentUser.UserId, cancellationToken);

            if (bank is null)
                return "Bank account not found.";

            if (transaction.Type == TransactionType.Income)
            {
                var creditResult = bank.Credit(transaction.Amount);
                if (creditResult.IsFailure)
                    return creditResult.Error!;
            }
            else
            {
                var debitResult = bank.Debit(transaction.Amount);
                if (debitResult.IsFailure)
                    return debitResult.Error!;
            }
        }

        if (transaction.CardId is not null)
        {
            var card = await dbContext.Cards
                .FirstOrDefaultAsync(c => c.Id == transaction.CardId && c.UserId == currentUser.UserId, cancellationToken);

            if (card is null)
                return "Card not found.";

            if (transaction.PaymentMethod == Domain.Enums.PaymentMethod.Credit)
            {
                var consumeResult = card.ConsumeLimit(transaction.Amount);
                if (consumeResult.IsFailure)
                    return consumeResult.Error!;
            }
            else if (transaction.PaymentMethod == Domain.Enums.PaymentMethod.Debit)
            {
                if (card.LinkedBankAccountId is null)
                    return "Debit card has no linked bank account.";

                var bank = await dbContext.BankAccounts
                    .FirstOrDefaultAsync(b => b.Id == card.LinkedBankAccountId, cancellationToken);

                if (bank is null)
                    return "Linked bank account not found.";

                var debitResult = bank.Debit(transaction.Amount);
                if (debitResult.IsFailure)
                    return debitResult.Error!;
            }
        }

        return null;
    }

    internal async Task<TransactionDto> ToDto(Transaction transaction, CancellationToken cancellationToken)
    {
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
