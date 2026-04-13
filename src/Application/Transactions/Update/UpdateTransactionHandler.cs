namespace Aerarium.Application.Transactions.Update;

using Aerarium.Application.Common;
using Aerarium.Domain.Common;
using Aerarium.Domain.Enums;
using Aerarium.Domain.ValueObjects;
using Mediator;
using Microsoft.EntityFrameworkCore;

public sealed class UpdateTransactionHandler(
    IAppDbContext dbContext,
    ICurrentUserService currentUser,
    ICategoryLocalizer categoryLocalizer,
    IPaymentMethodLocalizer paymentMethodLocalizer) : ICommandHandler<UpdateTransactionCommand, Result<TransactionDto>>
{
    public async ValueTask<Result<TransactionDto>> Handle(
        UpdateTransactionCommand command,
        CancellationToken cancellationToken)
    {
        var transaction = await dbContext.Transactions
            .FirstOrDefaultAsync(
                t => t.Id == command.Id && t.UserId == currentUser.UserId,
                cancellationToken);

        if (transaction is null)
            return Result<TransactionDto>.Failure("Transaction not found.");

        var revertError = await RevertWalletEffects(transaction, cancellationToken);
        if (revertError is not null)
            return Result<TransactionDto>.Failure(revertError);

        var result = transaction.Update(
            command.Amount,
            command.Description,
            command.Date,
            command.Type,
            command.Category,
            command.Recurrence,
            command.RecurrenceEndDate,
            command.RecurrenceCount,
            command.SalarySchedule,
            command.PaymentMethod,
            command.BankAccountId,
            command.CardId);

        if (result.IsFailure)
            return Result<TransactionDto>.Failure(result.Error!);

        var applyError = await ApplyWalletEffects(transaction, cancellationToken);
        if (applyError is not null)
            return Result<TransactionDto>.Failure(applyError);

        await dbContext.SaveChangesAsync(cancellationToken);

        return await ToDto(transaction, cancellationToken);
    }

    private async Task<string?> RevertWalletEffects(Domain.Entities.Transaction transaction, CancellationToken cancellationToken)
    {
        if (transaction.BankAccountId is not null)
        {
            var bank = await dbContext.BankAccounts
                .FirstOrDefaultAsync(b => b.Id == transaction.BankAccountId, cancellationToken);

            if (bank is not null)
            {
                if (transaction.Type == TransactionType.Income)
                    bank.Debit(transaction.Amount);
                else
                    bank.Credit(transaction.Amount);
            }
        }

        if (transaction.CardId is not null && transaction.PaymentMethod == PaymentMethod.Credit)
        {
            var card = await dbContext.Cards
                .FirstOrDefaultAsync(c => c.Id == transaction.CardId, cancellationToken);

            if (card is not null)
                card.RestoreLimit(transaction.Amount);
        }

        if (transaction.CardId is not null && transaction.PaymentMethod == PaymentMethod.Debit)
        {
            var card = await dbContext.Cards
                .FirstOrDefaultAsync(c => c.Id == transaction.CardId, cancellationToken);

            if (card?.LinkedBankAccountId is not null)
            {
                var bank = await dbContext.BankAccounts
                    .FirstOrDefaultAsync(b => b.Id == card.LinkedBankAccountId, cancellationToken);

                if (bank is not null)
                    bank.Credit(transaction.Amount);
            }
        }

        return null;
    }

    private async Task<string?> ApplyWalletEffects(Domain.Entities.Transaction transaction, CancellationToken cancellationToken)
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

            if (transaction.PaymentMethod == PaymentMethod.Credit)
            {
                var consumeResult = card.ConsumeLimit(transaction.Amount);
                if (consumeResult.IsFailure)
                    return consumeResult.Error!;
            }
            else if (transaction.PaymentMethod == PaymentMethod.Debit)
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

    private async Task<TransactionDto> ToDto(Domain.Entities.Transaction transaction, CancellationToken cancellationToken)
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

        var salaryDto = transaction.SalarySchedule is not null
            ? new SalaryScheduleDto(
                transaction.SalarySchedule.Mode, transaction.SalarySchedule.BusinessDayNumber,
                transaction.SalarySchedule.FixedDay, transaction.SalarySchedule.SplitFirstAmount,
                transaction.SalarySchedule.SplitFirstPercentage)
            : null;

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
            salaryDto,
            transaction.BankAccountId,
            bankName,
            transaction.CardId,
            cardName,
            transaction.CreatedAt,
            transaction.UpdatedAt);
    }
}
