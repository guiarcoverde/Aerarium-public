namespace Aerarium.Application.Transactions.Delete;

using Aerarium.Application.Common;
using Aerarium.Domain.Common;
using Aerarium.Domain.Enums;
using Mediator;
using Microsoft.EntityFrameworkCore;

public sealed class DeleteTransactionHandler(
    IAppDbContext dbContext,
    ICurrentUserService currentUser) : ICommandHandler<DeleteTransactionCommand, Result<bool>>
{
    public async ValueTask<Result<bool>> Handle(
        DeleteTransactionCommand command,
        CancellationToken cancellationToken)
    {
        var transaction = await dbContext.Transactions
            .FirstOrDefaultAsync(
                t => t.Id == command.Id && t.UserId == currentUser.UserId,
                cancellationToken);

        if (transaction is null)
            return Result<bool>.Failure("Transaction not found.");

        await RevertWalletEffects(transaction, cancellationToken);

        dbContext.Transactions.Remove(transaction);
        await dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    private async Task RevertWalletEffects(Domain.Entities.Transaction transaction, CancellationToken cancellationToken)
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
    }
}
