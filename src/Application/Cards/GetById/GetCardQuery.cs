namespace Aerarium.Application.Cards.GetById;

using Aerarium.Domain.Common;
using Mediator;

public sealed record GetCardQuery(Guid Id) : IQuery<Result<CardDto>>;
