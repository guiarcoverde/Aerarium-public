namespace Aerarium.Application.BankAccounts.Delete;

using Aerarium.Domain.Common;
using Mediator;

public sealed record DeleteBankAccountCommand(Guid Id) : ICommand<Result<bool>>;
