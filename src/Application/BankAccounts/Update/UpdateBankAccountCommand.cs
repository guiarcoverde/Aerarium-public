namespace Aerarium.Application.BankAccounts.Update;

using Aerarium.Domain.Common;
using Mediator;

public sealed record UpdateBankAccountCommand(
    Guid Id,
    string Name) : ICommand<Result<BankAccountDto>>;
