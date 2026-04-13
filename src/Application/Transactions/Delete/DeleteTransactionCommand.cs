namespace Aerarium.Application.Transactions.Delete;

using Aerarium.Domain.Common;
using Mediator;

public sealed record DeleteTransactionCommand(Guid Id) : ICommand<Result<bool>>;
