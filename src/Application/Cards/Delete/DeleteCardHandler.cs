namespace Aerarium.Application.Cards.Delete;

using Aerarium.Application.Common;
using Aerarium.Domain.Common;
using Mediator;
using Microsoft.EntityFrameworkCore;

public sealed class DeleteCardHandler(
    IAppDbContext dbContext,
    ICurrentUserService currentUser) : ICommandHandler<DeleteCardCommand, Result<bool>>
{
    public async ValueTask<Result<bool>> Handle(
        DeleteCardCommand command,
        CancellationToken cancellationToken)
    {
        var card = await dbContext.Cards
            .FirstOrDefaultAsync(c => c.Id == command.Id && c.UserId == currentUser.UserId, cancellationToken);

        if (card is null)
            return Result<bool>.Failure("Card not found.");

        var hasTransactions = await dbContext.Transactions
            .AnyAsync(t => t.CardId == card.Id, cancellationToken);

        if (hasTransactions)
            return Result<bool>.Failure("Cannot delete a card with linked transactions.");

        var canDelete = card.EnsureCanBeDeleted();
        if (canDelete.IsFailure)
            return Result<bool>.Failure(canDelete.Error!);

        dbContext.Cards.Remove(card);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
