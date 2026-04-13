namespace Aerarium.Application.Transactions.Delete;

using Aerarium.Domain.Common;
using Mediator;

public sealed record DeleteSeriesCommand(Guid RecurrenceGroupId) : ICommand<Result<bool>>;
