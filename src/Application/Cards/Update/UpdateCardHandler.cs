namespace Aerarium.Application.Cards.Update;

using Aerarium.Application.Cards.Create;
using Aerarium.Application.Common;
using Aerarium.Domain.Common;
using Aerarium.Domain.Enums;
using Mediator;
using Microsoft.EntityFrameworkCore;

public sealed class UpdateCardHandler(
    IAppDbContext dbContext,
    ICurrentUserService currentUser) : ICommandHandler<UpdateCardCommand, Result<CardDto>>
{
    public async ValueTask<Result<CardDto>> Handle(
        UpdateCardCommand command,
        CancellationToken cancellationToken)
    {
        var card = await dbContext.Cards
            .FirstOrDefaultAsync(c => c.Id == command.Id && c.UserId == currentUser.UserId, cancellationToken);

        if (card is null)
            return Result<CardDto>.Failure("Card not found.");

        if (command.Type.HasFlag(CardType.Debit) && command.LinkedBankAccountId is not null)
        {
            var bankExists = await dbContext.BankAccounts
                .AnyAsync(b => b.Id == command.LinkedBankAccountId && b.UserId == currentUser.UserId, cancellationToken);

            if (!bankExists)
                return Result<CardDto>.Failure("Linked bank account not found.");
        }

        var result = card.Update(command.Name, command.Type, command.CreditLimit, command.LinkedBankAccountId);

        if (result.IsFailure)
            return Result<CardDto>.Failure(result.Error!);

        await dbContext.SaveChangesAsync(cancellationToken);

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
