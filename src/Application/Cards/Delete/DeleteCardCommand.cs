namespace Aerarium.Application.Cards.Delete;

using Aerarium.Domain.Common;
using Mediator;

public sealed record DeleteCardCommand(Guid Id) : ICommand<Result<bool>>;
