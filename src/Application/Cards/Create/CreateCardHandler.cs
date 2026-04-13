namespace Aerarium.Application.Cards.Create;

using Aerarium.Application.Common;
using Aerarium.Domain.Common;
using Aerarium.Domain.Entities;
using Aerarium.Domain.Enums;
using Mediator;
using Microsoft.EntityFrameworkCore;

public sealed class CreateCardHandler(
    IAppDbContext dbContext,
    ICurrentUserService currentUser) : ICommandHandler<CreateCardCommand, Result<CardDto>>
{
    public async ValueTask<Result<CardDto>> Handle(
        CreateCardCommand command,
        CancellationToken cancellationToken)
    {
        if (command.Type.HasFlag(CardType.Debit) && command.LinkedBankAccountId is not null)
        {
            var bankExists = await dbContext.BankAccounts
                .AnyAsync(b => b.Id == command.LinkedBankAccountId && b.UserId == currentUser.UserId, cancellationToken);

            if (!bankExists)
                return Result<CardDto>.Failure("Linked bank account not found.");
        }

        var result = Card.Create(
            currentUser.UserId,
            command.Name,
            command.CreditLimit,
            command.Type,
            command.LinkedBankAccountId);

        if (result.IsFailure)
            return Result<CardDto>.Failure(result.Error!);

        var card = result.Value!;
        dbContext.Cards.Add(card);
        await dbContext.SaveChangesAsync(cancellationToken);

        var bankName = await GetLinkedBankName(card.LinkedBankAccountId, cancellationToken);
        return ToDto(card, bankName);
    }

    internal static CardDto ToDto(Card c, string? bankName) => new(
        c.Id, c.Name, c.CreditLimit, c.AvailableLimit, c.Type,
        c.LinkedBankAccountId, bankName, c.CreatedAt, c.UpdatedAt);

    internal async Task<string?> GetLinkedBankName(Guid? bankAccountId, CancellationToken cancellationToken)
    {
        if (bankAccountId is null)
            return null;

        return await dbContext.BankAccounts
            .Where(b => b.Id == bankAccountId)
            .Select(b => b.Name)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
