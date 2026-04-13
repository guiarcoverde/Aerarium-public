namespace Aerarium.Application.Cards.Create;

using Aerarium.Domain.Common;
using Aerarium.Domain.Enums;
using Mediator;

public sealed record CreateCardCommand(
    string Name,
    decimal CreditLimit,
    CardType Type,
    Guid? LinkedBankAccountId) : ICommand<Result<CardDto>>;
