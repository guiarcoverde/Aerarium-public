namespace Aerarium.Application.BankAccounts.Delete;

using Aerarium.Application.Common;
using Aerarium.Domain.Common;
using Mediator;
using Microsoft.EntityFrameworkCore;

public sealed class DeleteBankAccountHandler(
    IAppDbContext dbContext,
    ICurrentUserService currentUser) : ICommandHandler<DeleteBankAccountCommand, Result<bool>>
{
    public async ValueTask<Result<bool>> Handle(
        DeleteBankAccountCommand command,
        CancellationToken cancellationToken)
    {
        var account = await dbContext.BankAccounts
            .FirstOrDefaultAsync(b => b.Id == command.Id && b.UserId == currentUser.UserId, cancellationToken);

        if (account is null)
            return Result<bool>.Failure("Bank account not found.");

        var hasTransactions = await dbContext.Transactions
            .AnyAsync(t => t.BankAccountId == account.Id, cancellationToken);

        if (hasTransactions)
            return Result<bool>.Failure("Cannot delete a bank account with linked transactions.");

        var hasCards = await dbContext.Cards
            .AnyAsync(c => c.LinkedBankAccountId == account.Id, cancellationToken);

        if (hasCards)
            return Result<bool>.Failure("Cannot delete a bank account linked to cards. Remove the cards first.");

        var canDelete = account.EnsureCanBeDeleted();
        if (canDelete.IsFailure)
            return Result<bool>.Failure(canDelete.Error!);

        dbContext.BankAccounts.Remove(account);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
