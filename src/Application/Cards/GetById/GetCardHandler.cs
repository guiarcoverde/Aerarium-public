namespace Aerarium.Application.Cards.GetById;

using Aerarium.Application.Cards.Create;
using Aerarium.Application.Common;
using Aerarium.Domain.Common;
using Mediator;
using Microsoft.EntityFrameworkCore;

public sealed class GetCardHandler(
    IAppDbContext dbContext,
    ICurrentUserService currentUser) : IQueryHandler<GetCardQuery, Result<CardDto>>
{
    public async ValueTask<Result<CardDto>> Handle(
        GetCardQuery query,
        CancellationToken cancellationToken)
    {
        var card = await dbContext.Cards
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == query.Id && c.UserId == currentUser.UserId, cancellationToken);

        if (card is null)
            return Result<CardDto>.Failure("Card not found.");

        string? bankName = null;
        if (card.LinkedBankAccountId is not null)
        {
            bankName = await dbContext.BankAccounts
                .Where(b => b.Id == card.LinkedBankAccountId)
                .Select(b => b.Name)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return CreateCardHandler.ToDto(card, bankName);
    }
}
