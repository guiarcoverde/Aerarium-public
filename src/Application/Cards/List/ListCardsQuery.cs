namespace Aerarium.Application.Cards.List;

using Aerarium.Domain.Common;
using Mediator;

public sealed record ListCardsQuery : IQuery<Result<IReadOnlyList<CardDto>>>;
