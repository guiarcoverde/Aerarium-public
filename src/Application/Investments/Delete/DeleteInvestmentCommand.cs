namespace Aerarium.Application.Investments.Delete;

using Aerarium.Domain.Common;
using Mediator;

public sealed record DeleteInvestmentCommand(Guid Id) : ICommand<Result<bool>>;
