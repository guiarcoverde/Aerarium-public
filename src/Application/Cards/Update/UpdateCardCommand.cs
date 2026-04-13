namespace Aerarium.Application.Cards.Update;

using Aerarium.Domain.Common;
using Aerarium.Domain.Enums;
using Mediator;

public sealed record UpdateCardCommand(
    Guid Id,
    string Name,
    CardType Type,
    decimal CreditLimit,
    Guid? LinkedBankAccountId) : ICommand<Result<CardDto>>;
