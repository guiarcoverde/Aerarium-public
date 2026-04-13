namespace Aerarium.Application.BankAccounts.List;

using Aerarium.Domain.Common;
using Mediator;

public sealed record ListBankAccountsQuery : IQuery<Result<IReadOnlyList<BankAccountDto>>>;
