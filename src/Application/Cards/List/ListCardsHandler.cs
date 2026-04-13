namespace Aerarium.Application.Cards.List;

using Aerarium.Application.Cards.Create;
using Aerarium.Application.Common;
using Aerarium.Domain.Common;
using Mediator;
using Microsoft.EntityFrameworkCore;

public sealed class ListCardsHandler(
    IAppDbContext dbContext,
    ICurrentUserService currentUser) : IQueryHandler<ListCardsQuery, Result<IReadOnlyList<CardDto>>>
{
    public async ValueTask<Result<IReadOnlyList<CardDto>>> Handle(
        ListCardsQuery query,
        CancellationToken cancellationToken)
    {
        var cards = await dbContext.Cards
            .AsNoTracking()
            .Where(c => c.UserId == currentUser.UserId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);

        var bankIds = cards
            .Where(c => c.LinkedBankAccountId is not null)
            .Select(c => c.LinkedBankAccountId!.Value)
            .Distinct()
            .ToList();

        var bankNames = bankIds.Count > 0
            ? await dbContext.BankAccounts
                .Where(b => bankIds.Contains(b.Id))
                .ToDictionaryAsync(b => b.Id, b => b.Name, cancellationToken)
            : new Dictionary<Guid, string>();

        return cards
            .Select(c => CreateCardHandler.ToDto(c, c.LinkedBankAccountId is not null && bankNames.TryGetValue(c.LinkedBankAccountId.Value, out var name) ? name : null))
            .ToList();
    }
}
