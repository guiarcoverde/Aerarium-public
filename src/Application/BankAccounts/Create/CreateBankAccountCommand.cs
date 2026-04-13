namespace Aerarium.Application.BankAccounts.Create;

using Aerarium.Domain.Common;
using Mediator;

public sealed record CreateBankAccountCommand(
    string Name,
    decimal Balance) : ICommand<Result<BankAccountDto>>;
