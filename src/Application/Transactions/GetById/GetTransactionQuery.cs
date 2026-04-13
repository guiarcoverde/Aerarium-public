namespace Aerarium.Application.Transactions.GetById;

using Aerarium.Domain.Common;
using Mediator;

public sealed record GetTransactionQuery(Guid Id) : IQuery<Result<TransactionDto>>;
